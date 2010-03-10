using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Xml;
using ZDFMediathek2009.Code;
using ZDFMediathek2009.Code.DTO;

namespace OnlineVideos.Sites
{
    public class ZDFMediathekUtil : SiteUtilBase
    {
        [Category("OnlineVideosUserConfiguration"), Description("Defines the maximum quality for the video to be played.")]
        videoFormitaetQuality videoQuality = videoFormitaetQuality.veryhigh;

        RestAgent agent;
        RestAgent Agent 
        { 
            get 
            {
                if (agent == null)
                {
                    agent = new RestAgent("http://www.zdf.de/ZDFmediathek/xmlservice/tv/konfiguration");
                }
                return agent;
            } 
        }
        Dictionary<string, string> categoriesForSearching = new Dictionary<string, string>();

        public override int DiscoverDynamicCategories()
        {
            string[] groups = new string[] { "0-9|D", "E|K", "L|R", "S|V", "W|Z" };
            Settings.Categories.Clear();

            Teaser[][] teasers = new Teaser[5][];
            System.Threading.ManualResetEvent[] threadWaitHandles = new System.Threading.ManualResetEvent[5];
            for (int i = 0; i < groups.Length; i++)
            {
                threadWaitHandles[i] = new System.Threading.ManualResetEvent(false);
                new System.Threading.Thread(delegate(object o)
                    {
                        int o_i = (int)o;
                        string[] startEnd = groups[o_i].Split(new char[] { '|' });
                        var teaserlist = Agent.SendungenAbisZTeasers(ConfigurationHelper.GetSendungenAbisZServiceUrl(RestAgent.Configuration), startEnd[0], startEnd[1]);
                        teasers[o_i] = Agent.GetMCETeasers(teaserlist, TeaserListChoiceType.SendungenAZ);
                        threadWaitHandles[o_i].Set();
                    }) { IsBackground = true }.Start(i);
            }
            System.Threading.WaitHandle.WaitAll(threadWaitHandles);
            
            Settings.Categories.Add(new Category() { Name = "Sendung Verpasst", HasSubCategories = true, Description = "Sendungen der letzten 7 Tage." });
            
            for (int i = 0; i < teasers.Length; i++)
            foreach (var teaser in teasers[i])
            {
                RssLink item = new RssLink();
                item.Name = teaser.Title;
                item.EstimatedVideoCount = (uint)teaser.NumberOfTeasers;
                item.Url = teaser.ID;
                item.HasSubCategories = false;
                item.SubCategoriesDiscovered = true;
                item.Thumb = teaser.Image173x120;
                Settings.Categories.Add(item);
            }            

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (parentCategory.SubCategories != null &&
                parentCategory.SubCategories.Count > 0 &&
                parentCategory.SubCategories[0].Name == DateTime.Today.ToString("dddd, d.M.yyy"))
            { /* no need to rediscover if day hasn't changed */ }
            else
            {
                parentCategory.SubCategories = new List<Category>();
                for (int i = 0; i <= 7; i++)
                {
                    parentCategory.SubCategories.Add(new RssLink()
                    {
                        Name = DateTime.Today.AddDays(-i).ToString("dddd, d.M.yyy"),
                        Url = string.Format("enddate={0}&startdate={0}", DateTime.Today.AddDays(-i).ToString("ddMMyy")),
                        ParentCategory = parentCategory
                    });
                }
            }
            return parentCategory.SubCategories.Count;
        }
       
        public override String getUrl(VideoInfo video)        
        {
            video videoInfo = Agent.BeitragsDetail(ConfigurationHelper.GetBeitragsDetailsServiceUrl(RestAgent.Configuration), video.VideoUrl, video.Other.ToString());

            videoFormitaetQuality? foundQuality = null;
            string foundUrl = null;

            foreach (var vid in videoInfo.formitaeten)
            {
                if (vid.url.StartsWith("http://") && vid.url.EndsWith(".asx"))
                {
                    if (vid.quality == videoQuality)
                    {
                        foundQuality = vid.quality;
                        foundUrl = vid.url;
                        break;
                    }

                    if (vid.quality >= videoQuality && (foundQuality == null || vid.quality < foundQuality))
                    {
                        foundQuality = vid.quality;
                        foundUrl = vid.url;
                    }
                }
            }

            if (foundUrl != null) foundUrl = SiteUtilBase.ParseASX(foundUrl)[0];

            return foundUrl;
        }        

        public override List<VideoInfo> getVideoList(Category category)
        {
            teaserlist teaserlist;
            if ((category as RssLink).ParentCategory != null)
            {
                teaserlist = Agent.SendungVerpasst(ConfigurationHelper.GetSendungVerpasstServiceUrl(RestAgent.Configuration), 50, 0, (category as RssLink).Url);                
            }
            else
            {
                teaserlist = Agent.Aktuellste(ConfigurationHelper.GetAktuellsteServiceUrl(RestAgent.Configuration), (category as RssLink).Url, 50, 0, false);
            }
            return GetVideos(Agent.GetMCETeasers(teaserlist, TeaserListChoiceType.CurrentBroadcasts));
        }

        List<VideoInfo> GetVideos(Teaser[] teaserlist)
        {
            List<VideoInfo> list = new List<VideoInfo>();
            foreach (Teaser teaser in teaserlist)
            {
                if (teaser.IsVideo)
                {
                    VideoInfo item = new VideoInfo();
                    item.Title = teaser.Title;
                    item.ImageUrl = teaser.Image173x120;
                    item.Description = teaser.Details;
                    item.Length = teaser.VideoLength.ToString() + " | " + teaser.AirtimeDateTime.ToString("g");
                    item.VideoUrl = teaser.ID;
                    item.Other = teaser.ChannelID;
                    list.Add(item);
                }
            }
            return list;
        }

        #region Search

        public override bool CanSearch { get { return true; } }

        public override List<VideoInfo> Search(string query)
        {
            var teaserlist = Agent.DetailsSuche(ConfigurationHelper.GetSucheServiceUrl(), query, 50, 0);
            return GetVideos(Agent.GetMCETeasers(teaserlist, TeaserListChoiceType.Search));
        }

        #endregion
    }
}

