using MediaPortal.Configuration;
using MediaPortal.Profile;
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
    public class ZDFMediathekUtil : SiteUtilBase, ISearch
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
            var teaserlist = Agent.Aktuellste(ConfigurationHelper.GetAktuellsteServiceUrl(RestAgent.Configuration), (category as RssLink).Url, 50, 0, false);
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

        #region ISearch Member

        public Dictionary<string, string> GetSearchableCategories()
        {
            return new Dictionary<string,string>();
        }

        public List<VideoInfo> Search(string query)
        {
            var teaserlist = Agent.DetailsSuche(ConfigurationHelper.GetSucheServiceUrl(), query, 50, 0);
            return GetVideos(Agent.GetMCETeasers(teaserlist, TeaserListChoiceType.Search));
        }

        public List<VideoInfo> Search(string query, string category)
        {
            return Search(query);
        }

        #endregion
    }
}

