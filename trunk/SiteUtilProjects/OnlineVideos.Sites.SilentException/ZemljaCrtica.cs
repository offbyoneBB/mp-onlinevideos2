using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class SilentException_ZemljaCrtica : GenericSiteUtil
    {
        private RegexOptions defaultRegexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.ExplicitCapture;
        [Category("OnlineVideosConfiguration"), Description("RegEx to fetch category description")]
        string sRegexCatDescription = @"<span\s+id=""ctl00_lbl_txt"">\s*?<p>\s*?<img.*?/>(?<description>.*?)</span>";
        [Category("OnlineVideosConfiguration"), Description("RegEx to fetch category names")]
        private string sRegexCatName = @"<div\s+id\s*?=\s*?""grad"">\s*?<a\s+href\s*?=\s*?(?:'|"")({0})(?:'|"").*?>\s*?(?<title>.*?)\s*?(?:\(\d*\))\s*?</a>";

        Regex regexCatDescription;

        public override void Initialize(SiteSettings siteSettings)
        {
            regexCatDescription = new Regex(sRegexCatDescription, defaultRegexOptions);

            base.Initialize(siteSettings);
        }

        public override int DiscoverDynamicCategories()
        {
            int result = base.DiscoverDynamicCategories();

            string data = GetWebData(baseUrl, cookies: GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders);

            if (!string.IsNullOrEmpty(data) && Settings.Categories != null)
            {
                //names
                //foreach (Category cat in Settings.Categories)
                for (int i = Settings.Categories.Count - 1; i >= 0; i--)
                {
                    Category cat = Settings.Categories[i];
                    if (cat is RssLink)
                    {
                        string altThumb = cat.Thumb.Replace("/images/", "/userfiles/image/");
                        if (!string.IsNullOrEmpty(altThumb))
                            //cat.Thumb = string.IsNullOrEmpty(cat.Thumb) ? altThumb : string.Format("{0};{1})", cat.Thumb, altThumb);
                            cat.Thumb = altThumb;

                        Uri uri = new Uri(((RssLink)cat).Url);

                        Regex regexCatName = new Regex(string.Format(sRegexCatName, uri.LocalPath), defaultRegexOptions);
                        Match m = regexCatName.Match(data);
                        if (m.Success)
                        {
                            cat.Name = m.Groups["title"].Value;
                        }
                        else
                        {
                            Settings.Categories.RemoveAt(i);
                        }
                        //string catName = System.IO.Path.GetFileNameWithoutExtension(uri.LocalPath); 
                        //catName = catName.Replace('_', ' ');
                        //catName = Regex.Replace(catName, @"(\s|^)-(\w)", @"$1($2", RegexOptions.IgnoreCase);
                        //catName = Regex.Replace(catName, @"(\w)-(\s|$)", @"$1)$2", RegexOptions.IgnoreCase);
                        //cat.Name = catName;
                    }
                }
                //sort
                List<Category> sortedCategories = Settings.Categories.OrderBy(c => c.Name).ToList();
                Settings.Categories.Clear();
                foreach (Category cat in sortedCategories)
                {
                    Settings.Categories.Add(cat);
                }

                result = Settings.Categories.Count;
            }
            else
            {
                if (Settings.Categories != null)
                    Settings.Categories.Clear();
                Settings.DynamicCategoriesDiscovered = false;
                result = 0;
            }

            return result;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            // get web data
            string url = ((RssLink)category).Url;
            string data = GetWebData(url, cookies: GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders);
            List<VideoInfo> result = Parse(url, data);

            // set descriptions
            SetCategoryDescription(category, data);
            if (result != null)
            {
                foreach (VideoInfo video in result)
                {
                    video.Description = category.Description;
                }
            }

            return result;
        }

        private void SetCategoryDescription(Category category, string webData)
        {
            if (!string.IsNullOrEmpty(webData))
            {
                Match m = regexCatDescription.Match(webData);
                if (m.Success)
                {
                    category.Description = Clean(m.Groups["description"].Value);
                }

            }
        }

        private string Clean(string input)
        {
            string result = input;
            if (!string.IsNullOrEmpty(result))
            {
                // Remove <br/>
                result = Regex.Replace(result, @"< *br */*>", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                // Replace <p> with \n
                result = Regex.Replace(result, @"< *p */*>", "\n", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                // Clean other
                result = Helpers.StringUtils.PlainTextFromHtml(result);
            }
            return result;
        }
    
    }
}
