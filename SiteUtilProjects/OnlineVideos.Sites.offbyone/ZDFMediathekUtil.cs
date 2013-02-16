using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using ZDFMediathek2009.Code;
using ZDFMediathek2009.Code.DTO;

namespace OnlineVideos.Sites
{
    public class ZDFMediathekUtil : SiteUtilBase
    {
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Video Quality", TranslationFieldName="VideoQuality"), Description("Defines the maximum quality for the video to be played.")]
        videoFormitaetQuality videoQuality = videoFormitaetQuality.veryhigh;        

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            Settings.Categories.Add(new Category() { Name = "Startseite" });
            Settings.Categories.Add(new Category() { Name = "Nachrichten" });
            Settings.Categories.Add(new Category() { Name = "Sendung Verpasst", HasSubCategories = true, Description = "Sendungen der letzten 7 Tage." });
            Settings.Categories.Add(new Category() { Name = "Live" });
            Settings.Categories.Add(new Category() { Name = "Sendungen A-Z", HasSubCategories = true });
            Settings.Categories.Add(new Category() { Name = "Rubriken", HasSubCategories = true });
            Settings.Categories.Add(new Category() { Name = "Themen", HasSubCategories = true });
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (parentCategory.ParentCategory == null)
            {
                switch (parentCategory.Name)
                {
                    case "Sendung Verpasst":
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
                                    Url = string.Format("startdate={0}&enddate={0}", DateTime.Today.AddDays(-i).ToString("ddMMyy")),
                                    ParentCategory = parentCategory
                                });
                            }
                        }
                        break;
                    case "Sendungen A-Z":
                        parentCategory.SubCategories = new List<Category>();
                        parentCategory.SubCategories.Add(new RssLink() { Name = "0-9..C", ParentCategory = parentCategory, HasSubCategories = true });
                        parentCategory.SubCategories.Add(new RssLink() { Name = "D..E", ParentCategory = parentCategory, HasSubCategories = true });
                        parentCategory.SubCategories.Add(new RssLink() { Name = "F..J", ParentCategory = parentCategory, HasSubCategories = true });
                        parentCategory.SubCategories.Add(new RssLink() { Name = "K..L", ParentCategory = parentCategory, HasSubCategories = true });
                        parentCategory.SubCategories.Add(new RssLink() { Name = "M..R", ParentCategory = parentCategory, HasSubCategories = true });
                        parentCategory.SubCategories.Add(new RssLink() { Name = "S..U", ParentCategory = parentCategory, HasSubCategories = true });
                        parentCategory.SubCategories.Add(new RssLink() { Name = "V..Z", ParentCategory = parentCategory, HasSubCategories = true });
                        parentCategory.SubCategoriesDiscovered = true;
                        break;
                    case "Rubriken":
                        parentCategory.SubCategories = new List<Category>();
                        var teaserlistRubriken = Agent.Themen(ConfigurationHelper.GetRubrikenServiceUrl(RestAgent.Configuration), 50, 0);
                        foreach (var teaser in Agent.GetMCETeasers(teaserlistRubriken, TeaserListChoiceType.ThemenRubriken))
                        {
                            RssLink item = new RssLink();
                            item.Name = teaser.Title;
                            item.Description = teaser.Details;
                            item.EstimatedVideoCount = (uint)teaser.NumberOfTeasers;
                            item.Url = teaser.ID;
                            item.Thumb = teaser.Image173x120;
                            item.ParentCategory = parentCategory;
                            item.HasSubCategories = true;
                            parentCategory.SubCategories.Add(item);
                        }
                        parentCategory.SubCategoriesDiscovered = true;
                        break;
                    case "Themen":
                        parentCategory.SubCategories = new List<Category>();
                        var teaserlistThemen = Agent.Themen(ConfigurationHelper.GetThemenServiceUrl(RestAgent.Configuration), 50, 0);
                        foreach (var teaser in Agent.GetMCETeasers(teaserlistThemen, TeaserListChoiceType.ThemenRubriken))
                        {
                            RssLink item = new RssLink();
                            item.Name = teaser.ShortTitle.Length < teaser.Title.Length && teaser.ShortTitle.Length > 0 ? teaser.ShortTitle.Trim() : teaser.Title.Trim();
                            item.Description = teaser.Details;
                            item.EstimatedVideoCount = (uint)teaser.NumberOfTeasers;
                            item.Url = teaser.ID;
                            item.Thumb = teaser.Image173x120;
                            item.ParentCategory = parentCategory;
                            parentCategory.SubCategories.Add(item);
                        }
                        parentCategory.SubCategoriesDiscovered = true;
                        break;
                }
            }
            else
            {
                parentCategory.SubCategories = new List<Category>();
                teaserlist teaserlist = null;
                if (parentCategory.ParentCategory.Name == "Rubriken")
                {
                    teaserlist = Agent.Aktuellste(ConfigurationHelper.GetAktuellsteServiceUrl(RestAgent.Configuration), (parentCategory as RssLink).Url, 50, 0, false);
                }
                else
                {
                    
                    string[] startEnd = parentCategory.Name.Split(new string[] { ".." }, StringSplitOptions.RemoveEmptyEntries);
                    teaserlist = Agent.SendungenAbisZTeasers(ConfigurationHelper.GetSendungenAbisZServiceUrl(RestAgent.Configuration), startEnd[0], startEnd[1]);
                }
                foreach (var teaser in Agent.GetMCETeasers(teaserlist, TeaserListChoiceType.SendungenAZ))
                {
                    RssLink item = new RssLink();
                    item.Name = teaser.Title;
                    item.EstimatedVideoCount = (uint)teaser.NumberOfTeasers;
                    item.Url = teaser.ID;
                    item.Thumb = teaser.Image173x120;
                    item.ParentCategory = parentCategory;
                    parentCategory.SubCategories.Add(item);
                }
                parentCategory.SubCategoriesDiscovered = true;
            }
            return parentCategory.SubCategories.Count;
        }               

        public override List<VideoInfo> getVideoList(Category category)
        {
            // reset paging fields
            currentStart = 0;
            currentCategory = null;
            nextPageAvailable = false;
            previousPageAvailable = false;

            teaserlist teaserlist = null;
            switch (category.Name)
            {
                case "Startseite":
                    teaserlist = Agent.Tipps(ConfigurationHelper.GetTippsServiceUrl(RestAgent.Configuration), "_STARTSEITE", 50, 0);
                    break;
                case "Nachrichten":
                    teaserlist = Agent.Aktuellste(ConfigurationHelper.GetAktuellsteServiceUrl(RestAgent.Configuration), "_NACHRICHTEN", 50, 0, false);
                    break;
                case "Live":
                    teaserlist = Agent.Live(ConfigurationHelper.GetLiveServiceUrl(RestAgent.Configuration), 50, 0);
                    break;
                default:
                    if (category.ParentCategory.Name == "Sendung Verpasst")
                    {
                        teaserlist = Agent.SendungVerpasst(ConfigurationHelper.GetSendungVerpasstServiceUrl(RestAgent.Configuration), pageSize, 0, (category as RssLink).Url);
                    }
                    else
                    {
                        currentCategory = category as RssLink;
                        teaserlist = Agent.Aktuellste(ConfigurationHelper.GetAktuellsteServiceUrl(RestAgent.Configuration), currentCategory.Url, pageSize, 0, false);
                        nextPageAvailable = currentCategory.EstimatedVideoCount > pageSize;
                    }
                    break;
            }
            return GetVideos(Agent.GetMCETeasers(teaserlist, TeaserListChoiceType.CurrentBroadcasts));
        }

        public override String getUrl(VideoInfo video)
        {
            if (video.PlaybackOptions == null)
            {
                video videoInfo = Agent.BeitragsDetail(ConfigurationHelper.GetBeitragsDetailsServiceUrl(RestAgent.Configuration), video.VideoUrl, video.Other.ToString());
				var sortedPlaybackOptions = new SortedDictionary<uint, KeyValuePair<string, string>>();
                foreach (var vid in videoInfo.formitaeten)
                {
					if (vid.url.StartsWith("http://") && (vid.url.EndsWith(".asx") || (vid.url.EndsWith(".mp4") && !vid.url.Contains("hbbtv"))))
                    {
						if (vid.facets != null && vid.facets.Any(s => s == "restriction_useragent"))
							continue;
						string myUrl = vid.url.EndsWith(".asx") ? SiteUtilBase.ParseASX(vid.url)[0] : vid.url;
						uint bitrate = vid.bruttoBitrateSpecified ? vid.bruttoBitrate : vid.audioBitrate + vid.videoBitrate;
						sortedPlaybackOptions.Add(bitrate, 
							new KeyValuePair<string,string>(
								string.Format("{0} | {1,3:d}x{2,3:d} | {3,4:d} kbps | {4}:// | {5}", vid.quality.ToString().Replace("OBSOLETE_", "").PadLeft(8,' '), vid.width, vid.height, bitrate / 1024, myUrl.Substring(0, myUrl.IndexOf("://")), myUrl.Substring(myUrl.LastIndexOf('.'))), myUrl));
                    }
                }
				video.PlaybackOptions = sortedPlaybackOptions.ToDictionary(e => e.Value.Key, e => e.Value.Value);
            }

			if (video.PlaybackOptions == null || video.PlaybackOptions.Count == 0)
				return string.Empty;
			else if (video.PlaybackOptions.Count == 1)
				return video.PlaybackOptions.First().Value;
			else
			{
				string qualitytoMatch = videoQuality.ToString().Replace("OBSOLETE_", "");
				string firstUrl = video.PlaybackOptions.FirstOrDefault(p => p.Key.Contains(qualitytoMatch)).Value;
				if (!string.IsNullOrEmpty(firstUrl)) return firstUrl;
				else return video.PlaybackOptions.First().Value;
			}
        }

        List<VideoInfo> GetVideos(Teaser[] teaserlist)
        {
            List<VideoInfo> list = new List<VideoInfo>();
            foreach (Teaser teaser in teaserlist)
            {
                if (teaser.IsVideo || teaser.IsEinzelsendung)
                {
                    VideoInfo item = new VideoInfo();
                    item.Title = teaser.Title;
                    item.ImageUrl = teaser.Image173x120;
                    item.Description = teaser.Details;
                    item.Length = teaser.VideoLength != TimeSpan.Zero ? teaser.VideoLength.ToString() : teaser.Length;
                    item.Airdate = teaser.AirtimeDateTime.ToString("g", OnlineVideoSettings.Instance.Locale);
                    item.VideoUrl = teaser.ID;
                    item.Other = teaser.ChannelID;
                    list.Add(item);
                }
            }
            return list;
        }

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

        #region Next/Previous Page

        int pageSize = 50;
        int currentStart = 0;
        RssLink currentCategory;
        
        protected bool nextPageAvailable = false;
        public override bool HasNextPage
        {
            get { return nextPageAvailable; }
        }
        
        protected bool previousPageAvailable = false;
        public override bool HasPreviousPage
        {
            get { return previousPageAvailable; }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            var teaserlist = Agent.Aktuellste(ConfigurationHelper.GetAktuellsteServiceUrl(RestAgent.Configuration), currentCategory.Url, pageSize, currentStart + pageSize, false);
            if (teaserlist != null && teaserlist.teasers.Length > 0)
            {
                currentStart += pageSize;
                nextPageAvailable = currentCategory.EstimatedVideoCount > currentStart + pageSize;
                previousPageAvailable = true;
                return GetVideos(Agent.GetMCETeasers(teaserlist, TeaserListChoiceType.CurrentBroadcasts));
            }
            else
            {
                nextPageAvailable = false;
                return new List<VideoInfo>();
            }
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            currentStart -= pageSize;
            if (currentStart < 0) currentStart = 0;
            previousPageAvailable = currentStart > 0;
            var teaserlist = Agent.Aktuellste(ConfigurationHelper.GetAktuellsteServiceUrl(RestAgent.Configuration), currentCategory.Url, pageSize, currentStart, false);
            return GetVideos(Agent.GetMCETeasers(teaserlist, TeaserListChoiceType.CurrentBroadcasts));
        }

        #endregion

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

