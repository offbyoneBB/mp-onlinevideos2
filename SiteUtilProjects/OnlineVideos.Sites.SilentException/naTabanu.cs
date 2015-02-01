using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using OnlineVideos.Hoster.Base;

namespace OnlineVideos.Sites
{
    public class SilentException_NaTabanu : GenericSiteUtil
    {
        [Category("OnlineVideosUserConfiguration"), Description("If true, highest quality MP4 playback option will be used, otherwise use highest quality FLV")]
        bool useMP4Playback = true;

        [Category("OnlineVideosUserConfiguration"), Description("Auto play playback option chosen above")]
        bool autoChoose = false;

        [Category("OnlineVideosUserConfiguration"), Description("Categories to be skipped (not shown in GUI). Delimited by comma (',')")]
        string skipCategories = "smešne vesti,kontakt,galerija,forum,donacije,igrice";

        [Category("OnlineVideosUserConfiguration"), Description("Sub categories to be skipped (not shown in GUI). Delimited by comma (',')")]
        string skipSubCategories = "učesnici takmičari";

        //not used anymore
        //[Category("OnlineVideosConfiguration"), Description("RegEx to additionally fetch subcategories")]
        string subCategoryRegex = @"<li class=""level1""><a href=""([^""]*)"" class=""level1 topdaddy""><span><categoryName></span></a><ul>(?<subcategories>.*?)</ul>";

        [Category("OnlineVideosConfiguration"), Description("URL to additionally fetch series")]
        string seriesCategoryUrl = "http://www.natabanu.com/spisak-svih-online-serija.html";

        [Category("OnlineVideosConfiguration"), Description("RegEx to additionally fetch series")]
        string seriesCategoryRegex = @"<li><a\shref.*?>\s*?<img.*?>\s*?</a>\s*?<a\shref=""(?<url>[^""]*)""\sclass=""title"">(?<title>[^<]*)</a>\s*?</li>";

        [Category("OnlineVideosConfiguration"), Description("Alternative RegEx to get the playlist")]
        string altFileUrlRegex = @"<iframe title=""YouTube video player"".*?class=""youtube-player"".*?src=""(?<m0>[^""]*)""";

        [Category("OnlineVideosConfiguration"), Description("Second alternative RegEx to get the playlist")]
        string altFileUrlRegex2 = @"<object\s(style=""([^""]*)"")?.*?data=""(?<m0>[^""]*)""\stype=""application/x-shockwave-flash""\s?>";

        public override int DiscoverDynamicCategories()
        {
            int result = base.DiscoverDynamicCategories();

            RssLink mainPageCat = new RssLink();
            mainPageCat.HasSubCategories = false;
            mainPageCat.Name = "Novo";
            mainPageCat.Url = baseUrl;

            Settings.Categories.Insert(0, mainPageCat);
            result = Settings.Categories.Count;

            List<Category> toRemove = new List<Category>();

            string[] skip = skipCategories.Split(new string[] {","}, StringSplitOptions.RemoveEmptyEntries);
            if (Settings.Categories != null && Settings.Categories.Count > 0) 
            {
                foreach (string s in skip)
                {
                    foreach (Category cat in Settings.Categories)
                    {
                        if (cat.Name.ToUpperInvariant() == s.ToUpperInvariant())
                        {
                            if (!toRemove.Contains(cat))
                                toRemove.Add(cat);
                        }
                    }
                }
                foreach (Category cat in toRemove)
                    Settings.Categories.Remove(cat);

                result = Settings.Categories.Count;
            }

            return result;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            // code not used, always go to else!
            if (false && parentCategory is RssLink && regEx_dynamicSubCategories != null)
            {
                string data = GetWebData((parentCategory as RssLink).Url, cookies: GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders);
                if (!string.IsNullOrEmpty(data))
                {
                    parentCategory.SubCategories = new List<Category>();
                    Regex regEx_dyNamicSubCategory = new Regex(subCategoryRegex.Replace("<categoryName>", parentCategory.Name));
                    Match m = regEx_dyNamicSubCategory.Match(data);
                    while (m.Success)
                    {
                        string subCategories = m.Groups["subcategories"].Value;
                        if (!string.IsNullOrEmpty(subCategories))
                        {
                            Match m2 = regEx_dynamicSubCategories.Match(subCategories);
                            while (m2.Success)
                            {
                                RssLink cat = new RssLink();
                                cat.Url = m2.Groups["url"].Value;
                                if (!string.IsNullOrEmpty(dynamicSubCategoryUrlFormatString)) cat.Url = string.Format(dynamicSubCategoryUrlFormatString, cat.Url);
								cat.Url = ApplyUrlDecoding(cat.Url, dynamicSubCategoryUrlDecoding);
                                if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(baseUrl), cat.Url).AbsoluteUri;
                                cat.Name = System.Web.HttpUtility.HtmlDecode(m2.Groups["title"].Value.Trim());
                                cat.Thumb = m2.Groups["thumb"].Value;
                                if (!String.IsNullOrEmpty(cat.Thumb) && !Uri.IsWellFormedUriString(cat.Thumb, System.UriKind.Absolute)) cat.Thumb = new Uri(new Uri(baseUrl), cat.Thumb).AbsoluteUri;
                                cat.Description = m2.Groups["description"].Value;
                                cat.ParentCategory = parentCategory;
                                parentCategory.SubCategories.Add(cat);
                                m2 = m2.NextMatch();
                            }
                        }
                        m = m.NextMatch();
                    }
                    parentCategory.SubCategoriesDiscovered = true;
                }

                List<Category> toRemove = new List<Category>();

                string[] skip = skipSubCategories.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                if (parentCategory.SubCategories != null && parentCategory.SubCategories.Count > 0)
                {
                    foreach (string s in skip)
                    {
                        foreach (Category cat in parentCategory.SubCategories)
                        {
                            if (cat.Name.ToUpperInvariant() == s.ToUpperInvariant())
                            {
                                if (!toRemove.Contains(cat))
                                    toRemove.Add(cat);
                            }
                        }
                    }
                    foreach (Category cat in toRemove)
                        parentCategory.SubCategories.Remove(cat);
                }
                
                return parentCategory.SubCategories == null ? 0 : parentCategory.SubCategories.Count;
            }
            else
            {
                int result = base.DiscoverSubCategories(parentCategory);
                if (result == 0 && parentCategory.Name.Equals("Serije"))
                  result = DiscoverDynamicSeries(parentCategory);
                RssLink catAll = new RssLink();
                catAll.Url = (parentCategory as RssLink).Url;
                catAll.Name = "Sve";
                catAll.Thumb = "";
                catAll.Description = "";
                catAll.ParentCategory = parentCategory;
                parentCategory.SubCategories.Insert(0, catAll);
                return result;
            }
        }

        private int DiscoverDynamicSeries(Category parentCategory)
        {
            int result = 0;
            string data = GetWebData(seriesCategoryUrl, cookies: GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders);
            if (!string.IsNullOrEmpty(data) && !string.IsNullOrEmpty(seriesCategoryRegex))
            {
                Regex regEx_seriesCategory = new Regex(seriesCategoryRegex);
                Match m = regEx_seriesCategory.Match(data);
                while (m.Success)
                {
                    string url = m.Groups["url"].Value;
                    string title = m.Groups["title"].Value;
                    if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(title) && !HasSubCategory(parentCategory, url, title))
                    {
                        RssLink newCat = new RssLink();
                        newCat.Url = url;
                        newCat.Name = title;
                        newCat.Thumb = "";
                        newCat.Description = "";
                        newCat.ParentCategory = parentCategory;
                        parentCategory.SubCategories.Add(newCat);
                        result++;
                    }
                    m = m.NextMatch();
                }
            }
            return result;
        }

        private bool HasSubCategory(Category parentCategory, string url, string title)
        {
            foreach (Category cat in parentCategory.SubCategories)
            {
                if (cat is RssLink && (cat as RssLink).Url.Equals(url) && (cat as RssLink).Name.Equals(title))
                {
                    return true;    
                }
            }
            return false;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string result = base.GetVideoUrl(video);

            if (string.IsNullOrEmpty(result))
            {
                Regex playlist = regEx_PlaylistUrl;
                Regex file = regEx_FileUrl;

                regEx_PlaylistUrl = null;
                regEx_FileUrl = new Regex(altFileUrlRegex);
                try
                {
                    result = base.GetVideoUrl(video);
                    if (string.IsNullOrEmpty(result))
                    {
                        regEx_FileUrl = new Regex(altFileUrlRegex2);
                        result = base.GetVideoUrl(video);
                    }
                }
                finally
                {
                    regEx_PlaylistUrl = playlist;
                    regEx_FileUrl = file;
                }
            }

            return result;
        }

        public override List<String> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<String> urls = new List<String>();
            string resultUrl = GetVideoUrl(video);

            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
            {
                List<string> valueList = video.PlaybackOptions.Values.ToList();
                video.PlaybackOptions.Clear();
                foreach (string value in valueList)
                {
                    urls.Add(value);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(resultUrl))
                {
                    urls.Add(resultUrl);

                    Uri uri = new Uri(resultUrl);
                    foreach (HosterBase hosterUtil in HosterFactory.GetAllHosters())
                        if (uri.Host.ToLower().Contains(hosterUtil.getHosterUrl().ToLower()))
                        {
                            Dictionary<string, string> options = hosterUtil.getPlaybackOptions(resultUrl);
                            if (options != null && options.Count > 0)
                            {
                                string chosenoption = options.Last().Key;
                                List<string> availoptions = options.Keys.ToList();
                                for (int i = availoptions.Count - 1; i >= 0; i--)
                                {
                                    if (useMP4Playback && availoptions[i].IndexOf("mp4", StringComparison.InvariantCultureIgnoreCase) > -1)
                                    {
                                        chosenoption = availoptions[i];
                                        break;
                                    }
                                    else if (!useMP4Playback && availoptions[i].IndexOf("flv", StringComparison.InvariantCultureIgnoreCase) > -1)
                                    {
                                        chosenoption = availoptions[i];
                                        break;
                                    }
                                }

                                if (autoChoose)
                                {
                                    urls.Clear();
                                    urls.Add(options[chosenoption]);
                                    break;
                                }
                                else
                                {
                                    urls.Clear();
                                    video.PlaybackOptions = options;
                                    urls.Add(options[chosenoption]);
                                    break;
                                }
                            }
                        }
                }
            }

            return urls;
        }

        public override string GetPlaylistItemVideoUrl(VideoInfo clonedVideoInfo, string chosenPlaybackOption, bool inPlaylist = false)
        {
            Uri uri = new Uri(clonedVideoInfo.VideoUrl);
            //chosenPlaybackOption = "";
            clonedVideoInfo.PlaybackOptions = new Dictionary<string, string>();
            foreach (HosterBase hosterUtil in HosterFactory.GetAllHosters())
                if (uri.Host.ToLower().Contains(hosterUtil.getHosterUrl().ToLower()))
                {
                    Dictionary<string, string> options = hosterUtil.getPlaybackOptions(clonedVideoInfo.VideoUrl);
                    if (options != null && options.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(chosenPlaybackOption) && options.Keys.Contains(chosenPlaybackOption))
                        {
                            return options[chosenPlaybackOption];
                        }
                        else
                        {
                            string chosenoption = options.Last().Key;
                            List<string> availoptions = options.Keys.ToList();
                            for (int i = availoptions.Count - 1; i >= 0; i--)
                            {
                                if (useMP4Playback && availoptions[i].IndexOf("mp4", StringComparison.InvariantCultureIgnoreCase) > -1)
                                {
                                    chosenoption = availoptions[i];
                                    break;
                                }
                                else if (!useMP4Playback && availoptions[i].IndexOf("flv", StringComparison.InvariantCultureIgnoreCase) > -1)
                                {
                                    chosenoption = availoptions[i]; 
                                    break;
                                }
                            }

                            if (autoChoose)
                            {
                                return options[chosenoption];
                            }

                            clonedVideoInfo.PlaybackOptions = options;
                            return options[chosenoption];
                        }
                    }
                }

            return clonedVideoInfo.VideoUrl;
        }
    }
}
