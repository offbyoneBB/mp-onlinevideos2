using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using System.Net;
using System.Xml;
using OnlineVideos.Sites.Cornerstone;

namespace OnlineVideos.Sites
{
    public class ScriptUtil : SiteUtilBase
    {
        [Category("OnlineVideosConfiguration"), System.ComponentModel.Description("Script file used for site scraping")]
        public string scriptFile;

        private ScriptableScraper _scraper;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            _scraper = new ScriptableScraper(new FileInfo(scriptFile));
            base.Settings.IsEnabled = true;
            base.Settings.Language = _scraper.Language;
            base.Settings.Name = _scraper.Name;
        }

        
        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            Dictionary<string, string> paramList = new Dictionary<string, string>();
            Dictionary<string, string> results;
            results = _scraper.Execute("get_categories", paramList);
            int count = 0;
            while (results.ContainsKey("category[" + count + "].url"))
            {
                string prefix = "category[" + count + "].";
                count++;
                string name;
                if (!results.TryGetValue(prefix + "name", out name))
                    continue;
                RssLink rssLink = new RssLink();
                Type t = rssLink.GetType();
                PropertyInfo[] pi = t.GetProperties();
                foreach (PropertyInfo prop in pi)
                {
                    if (prop.CanWrite && prop.PropertyType.FullName == "System.String")
                    {
                        string value;
                        bool success = results.TryGetValue(prefix+prop.Name.ToLower(), out value);
                        if (success)
                            prop.SetValue(rssLink, value, null);
                    }
                }
                Settings.Categories.Add(rssLink);
            }
            return count;
        }

        /// <summary>
        /// This is the only function a subclass has to implement. It's called when a user selects a category in the GUI. 
        /// It should return a list of videos for that category, reset the paging indexes, remember this category, whatever is needed to hold state.
        /// </summary>
        /// <param name="category">The <see cref="Category"/> that was selected by the user.</param>
        /// <returns>a list of <see cref="VideoInfo"/> object for display</returns>
        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> loVideoList = new List<VideoInfo>();
            Dictionary<string, string> paramList = new Dictionary<string, string>();
            Dictionary<string, string> results;
            RssLink rssLink = category as RssLink;
            if (rssLink == null)
                return loVideoList;
            foreach (PropertyInfo prop in rssLink.GetType().GetProperties())
            {
                if (prop.GetValue(category, null) != null)
                    paramList.Add("category." + prop.Name.ToLower(), prop.GetValue(category, null).ToString());
            }
            results = _scraper.Execute("get_videolist", paramList);
            int count = 0;
            while (results.ContainsKey("video[" + count + "].title"))
            {
                string prefix = "video[" + count + "].";
                count++;
                VideoInfo videoInfo = new VideoInfo();
                foreach (PropertyInfo prop in videoInfo.GetType().GetProperties())
                {
                    if (prop.CanWrite && prop.PropertyType.FullName == "System.String")
                    {
                        string value;
                        bool success = results.TryGetValue(prefix + prop.Name.ToLower(), out value);
                        if (success)
                            prop.SetValue(videoInfo, value, null);
                    }
                }
                loVideoList.Add(videoInfo);
            }
            return loVideoList;
        }

        public override string getUrl(VideoInfo video)
        {
            Dictionary<string, string> paramList = new Dictionary<string, string>();
            Dictionary<string, string> results;
            string url = string.Empty;
            foreach (PropertyInfo prop in video.GetType().GetProperties())
            {
                if (prop.GetValue(video, null) != null)
                    paramList.Add("video." + prop.Name.ToLower(), prop.GetValue(video, null).ToString());
            }
            results = _scraper.Execute("get_videourl", paramList);
            results.TryGetValue("videourl", out url);
            return url;
        }
    }
}
