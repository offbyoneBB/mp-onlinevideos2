using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace OnlineVideos.Sites
{
    public class MovHunterUtil : GenericSiteUtil
    {
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Enable download subtitles from context menu"), Description("Enable the possibility to download subtitles from context menu(F9 or info button on remote) of videos. Slows down opening of context menu. Far from all movies have subtitles")]
        protected bool enableSubtitles = true;

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Load movie timeout"), Description("In seconds. Onlinvideos default 20 seconds, MovHunter default 60 seconds.")]
        uint httpReceiveDataTimeoutInSec = 60;

        public override int DiscoverDynamicCategories()
        {
            int count = base.DiscoverDynamicCategories();
            foreach (Category cat in Settings.Categories) { cat.HasSubCategories = cat.Name == "TV Series"; }
            return count;
        }

        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            ITrackingInfo ti = new TrackingInfo();
            Regex rgx = new Regex(@"(.+)S(\d+)E(\d+)");
            uint s = 0;
            uint e = 0;
            Match m = rgx.Match(video.Title);
            if (m.Success)
            {
                ti.VideoKind = VideoKind.TvSeries;
                ti.Title = m.Groups[1].Value.Trim();
                uint.TryParse(m.Groups[2].Value, out s);
                ti.Season = s;
                uint.TryParse(m.Groups[3].Value, out e);
                ti.Episode = e;
            }
            else
            {
                ti.VideoKind = VideoKind.Movie;
                rgx = new Regex(@"(.+)\((\d{4})\)");
                m = rgx.Match(video.Title);
                uint y = 0;
                if (m.Success) //movies with year
                {
                    ti.Title = m.Groups[1].Value.Trim();
                    uint.TryParse(m.Groups[2].Value, out y);
                    ti.Year = y;
                }
                else // movie no year
                {
                    ti.Title = video.Title;
                }
            }
            return ti;
        }

        public override string getUrl(VideoInfo video)
        {
            string url = base.getUrl(video);
            url = new MPUrlSourceFilter.HttpUrl(url) { ReceiveDataTimeout = (int)httpReceiveDataTimeoutInSec * 1000 }.ToString();
            return url;
        }

        public override string GetFileNameForDownload(VideoInfo video, Category category, string url)
        {
            return OnlineVideos.Utils.GetSaveFilename(video.Title) + ".mp4";
        }

        public override List<ContextMenuEntry> GetContextMenuEntries(Category selectedCategory, VideoInfo selectedItem)
        {
            List<ContextMenuEntry> entries = new List<ContextMenuEntry>();
            if (selectedItem != null && enableSubtitles)
            {
                HtmlAgilityPack.HtmlDocument doc = GetWebData<HtmlAgilityPack.HtmlDocument>(selectedItem.VideoUrl);
                IEnumerable<HtmlAgilityPack.HtmlNode> subs = doc.DocumentNode.Descendants("track").Where(t => t.GetAttributeValue("kind", "") == "captions");
                if (subs != null && subs.Count() > 0)
                {
                    ContextMenuEntry subLangs = new ContextMenuEntry() { DisplayText = "Choose a subtitle", Action = ContextMenuEntry.UIAction.ShowList };
                    foreach (HtmlAgilityPack.HtmlNode sub in subs)
                    {
                        string lang = sub.GetAttributeValue("label", "");
                        string url = sub.GetAttributeValue("src", "");
                        if (!string.IsNullOrEmpty(lang) && !string.IsNullOrEmpty(url))
                        {
                            ContextMenuEntry entry = new ContextMenuEntry();
                            entry.DisplayText = string.Format(lang);
                            entry.Other = url;
                            subLangs.SubEntries.Add(entry);
                        }
                    }
                    entries.Add(subLangs);
                }
            }
            return entries;
        }

        public override ContextMenuExecutionResult ExecuteContextMenuEntry(Category selectedCategory, VideoInfo selectedItem, ContextMenuEntry choice)
        {
            if (selectedItem != null && enableSubtitles)
            {
                ContextMenuExecutionResult result = new ContextMenuExecutionResult();
                selectedItem.SubtitleText = GetWebData<string>(choice.Other as string);
                selectedItem.SubtitleText = selectedItem.SubtitleText.Replace("WEBVTT\r\n\r\n", "");
                result.ExecutionResultMessage = selectedItem.Title + " - Subtitle: " + choice.DisplayText;
                result.RefreshCurrentItems = false;
                return result;
            }
            return base.ExecuteContextMenuEntry(selectedCategory, selectedItem, choice);
        }
    }
}