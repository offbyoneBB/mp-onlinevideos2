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
        public string ScriptFile;

        private int _currentPageNumber = 0;
        private Category _currentCategory = null;
        private string _currentSiteUrl = string.Empty;

        private ScriptableScraper _scraper;

        public override bool HasNextPage
        {
            get
            {
                return true;
            }
        }

        public override bool HasPreviousPage
        {
            get
            {
                return true;
            }
        }

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            _scraper = new ScriptableScraper(new FileInfo(ScriptFile));
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
            if (_currentCategory != null && category != _currentCategory)
            {
                _currentSiteUrl = string.Empty;
                _currentPageNumber = 0;
            }
            _currentCategory = category;

            List<VideoInfo> loVideoList = new List<VideoInfo>();
            Dictionary<string, string> paramList = new Dictionary<string, string>();

            addDefaultParam(paramList);
            addCategoryToParams(category, paramList);


            Dictionary<string, string> results = _scraper.Execute("get_videolist", paramList);

            int count = 0;
            
            pharseReturnedValues(results);

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

        public override List<VideoInfo> getNextPageVideos()
        {
            Dictionary<string, string> paramList = new Dictionary<string, string>();
            Dictionary<string, string> results;
            addDefaultParam(paramList);
            
            if (_currentCategory != null)
                addCategoryToParams(_currentCategory, paramList);

            results = _scraper.Execute("get_next_videolist", paramList);
            pharseReturnedValues(results);

            return getVideoList(_currentCategory);
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            Dictionary<string, string> paramList = new Dictionary<string, string>();
            Dictionary<string, string> results;
            addDefaultParam(paramList);

            if (_currentCategory != null)
                addCategoryToParams(_currentCategory, paramList);

            results = _scraper.Execute("get_prev_videolist", paramList);
            pharseReturnedValues(results);

            return getVideoList(_currentCategory);
        }

        public override List<VideoInfo> Search(string query)
        {
            return base.Search(query);
        }


        #region private methods

        static void addCategoryToParams(Category category, IDictionary<string, string> paramList)
        {
            RssLink rssLink = category as RssLink;
            if (rssLink == null)
                return ;
            foreach (PropertyInfo prop in rssLink.GetType().GetProperties())
            {
                if (prop.GetValue(category, null) != null)
                    paramList.Add("category." + prop.Name.ToLower(), prop.GetValue(category, null).ToString());
            }
        }

        void addDefaultParam(IDictionary<string, string> paramList)
        {
            paramList.Add("current.pagenumber", _currentPageNumber.ToString());
            paramList.Add("current.pageurl", _currentSiteUrl);
        }

        void pharseReturnedValues(IDictionary<string, string> paramList)
        {
            paramList.TryGetValue("current.pageurl", out _currentSiteUrl);
            
            string value;
            if (paramList.TryGetValue("current.pagenumber", out value))
            {
                int i = 0;
                if (int.TryParse(value, out i))
                    _currentPageNumber = i;
            }
        }

        #endregion
    }
}
