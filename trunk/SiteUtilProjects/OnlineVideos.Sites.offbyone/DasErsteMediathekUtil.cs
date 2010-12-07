using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{    
    public class DasErsteMediathekUtil : SiteUtilBase
    {        
        public enum VideoQuality { Low, High, Max };

        [Category("OnlineVideosUserConfiguration"), Description("Choose your preferred quality for the videos according to bandwidth.")]
        VideoQuality videoQuality = VideoQuality.High;

        [Category("OnlineVideosConfiguration")]
        string categoriesRegEx = @"<div\sclass=""mt-reset\smt-categories"">\s*
<ul>\s*
(?:<li><a\shref=""(?<Url>[^""]+)""[^>]*>(?<Title>[^<]+)</a></li>\s*)+
</ul>\s*
</div>";
        [Category("OnlineVideosConfiguration")]
        string subcategoriesRegEx = @"<div\sclass=""mt-media_item"">\s*
<div\sclass=""mt-image"">\s*
<img\s(data-)?src=""(?<ImageUrl>[^""]+)""[^>]*>\s*
</div>\s*
<h3\sclass=""mt-title""><a\shref=""(?<Url>[^""]+)""\s[^>]*>(?<Title>[^<]+)</a></h3>";
        [Category("OnlineVideosConfiguration")]
        string extraSubCatPagesRegEx = @"<option\svalue=""(?<Url>[^""]+)"">[^<]*</option>";
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for videos.")]
        string videoListRegEx = @"<div\sclass=""mt-media_item[^""]*"">\s*
<div\sclass=""mt-image"">\s*
<span\sclass=""mt-icon\smt-icon_video""></span>\s*
<img\s(data-)?src=""(?<ImageUrl>[^""]+)""\s[^/]*/>\s*
</div>\s*
<h3\sclass=""mt-title""><a\shref=""(?<Url>[^""]+)""[^>]*>(?<Title>[^<]+)</a></h3>\s*
<p[^>]*>[^<]*</p>\s*
<p\sclass=""mt-airtime_channel""><span\sclass=""mt-airtime"">(?<Duration>[^<]+)</span>";
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for a next page link.")]
        string nextPageRegEx = @"<a\s+href=""(?<NextPageUrl>[^""]+)""\s[^>]*>Weiter</a>";
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for a previous page link.")]
        string prevPageRegEx = @"<a\s+href=""(?<PrevPageUrl>[^""]+)""\s[^>]*>Zurück</a>";
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for playback urls.")]
        string videoUrlOptionsRegEx = @"mediaCollection.addMediaStream\((?<Info>[^)]+)";
        [Category("OnlineVideosConfiguration"), Description("Format string used as Url for getting the results of a search. {0} will be replaced with the query.")]
        string searchUrl = "http://www.ardmediathek.de/ard/servlet/content/3517006?inhalt=tv&s={0}";

        Regex regEx_Categories, regEx_Subcategories, regEx_VideoList, regEx_extraSubCatPages, regEx_NextPage, regEx_PrevPage, regEx_VideoUrlOptions;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_Categories = new Regex(categoriesRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
            regEx_Subcategories = new Regex(subcategoriesRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
            regEx_VideoList = new Regex(videoListRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
            regEx_extraSubCatPages = new Regex(extraSubCatPagesRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant);
            regEx_NextPage = new Regex(nextPageRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
            regEx_PrevPage = new Regex(prevPageRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
            regEx_VideoUrlOptions = new Regex(videoUrlOptionsRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            string dataPage = GetWebData("http://www.ardmediathek.de/ard/servlet/");
            Match m = regEx_Categories.Match(dataPage);
            if(m.Success)
            {
                for (int i = 0; i < m.Groups["Title"].Captures.Count; i++)
                {
                    RssLink item = new RssLink() { HasSubCategories = true, SubCategories = new List<Category>() };
                    item.Name = HttpUtility.HtmlDecode(m.Groups["Title"].Captures[i].Value);
                    item.Url = m.Groups["Url"].Captures[i].Value;
                    item.Url = item.Url.Substring(item.Url.IndexOf("?"));
                    item.Url = "http://www.ardmediathek.de/ard/servlet/ajax-cache/3516706/view=switch/clipFilter=fernsehen/content=fernsehen/documentId=" + HttpUtility.ParseQueryString(item.Url)["documentId"] + "/index.html";                    
                    Settings.Categories.Add(item);
                }
            }
            Settings.DynamicCategoriesDiscovered = true;            
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string dataPage = GetWebData(((RssLink)parentCategory).Url);

            List<string> additionalPageUrls = new List<string>();
            Match mPage = regEx_extraSubCatPages.Match(dataPage);
            while (mPage.Success)
            {
                additionalPageUrls.Add(mPage.Groups["Url"].Value);
                mPage = mPage.NextMatch();
            }
            Match m = regEx_Subcategories.Match(dataPage);
            while (m.Success)
            {
                RssLink subCat = new RssLink()
                {
                    Name = HttpUtility.HtmlDecode(m.Groups["Title"].Value),
                    Url = m.Groups["Url"].Value,                    
                    Thumb = "http://www.ardmediathek.de" + m.Groups["ImageUrl"].Value,
                    ParentCategory = parentCategory
                };
                subCat.Url = subCat.Url.Substring(subCat.Url.IndexOf("?"));
                subCat.Url = "http://www.ardmediathek.de/ard/servlet/ajax-cache/3516962/view=list/documentId=" + HttpUtility.ParseQueryString(subCat.Url)["documentId"] + "/index.html";
                
                parentCategory.SubCategories.Add(subCat);
                m = m.NextMatch();
            }

            if (additionalPageUrls.Count > 0)
            {
                System.Threading.ManualResetEvent[] threadWaitHandles = new System.Threading.ManualResetEvent[additionalPageUrls.Count];
                for (int i = 0; i < additionalPageUrls.Count; i++)
                {
                    threadWaitHandles[i] = new System.Threading.ManualResetEvent(false);
                    new System.Threading.Thread(delegate(object o)
                        {
                            int o_i = (int)o;
                            string addDataPage = GetWebData("http://www.ardmediathek.de" + additionalPageUrls[o_i]);
                            Match addM = regEx_Subcategories.Match(addDataPage);
                            if (o_i > 0) System.Threading.WaitHandle.WaitAny(new System.Threading.ManualResetEvent[] { threadWaitHandles[o_i - 1] });
                            while (addM.Success)
                            {
                                RssLink subCat = new RssLink()
                                {
                                    Name = addM.Groups["Title"].Value,
                                    Url = addM.Groups["Url"].Value,
                                    Thumb = "http://www.ardmediathek.de" + addM.Groups["ImageUrl"].Value,
                                    ParentCategory = parentCategory
                                };
                                subCat.Url = subCat.Url.Substring(subCat.Url.IndexOf("?"));
                                subCat.Url = "http://www.ardmediathek.de/ard/servlet/ajax-cache/3516962/view=list/documentId=" + HttpUtility.ParseQueryString(subCat.Url)["documentId"] + "/index.html";

                                parentCategory.SubCategories.Add(subCat);
                                addM = addM.NextMatch();
                            }

                            threadWaitHandles[o_i].Set();
                        }) { IsBackground = true }.Start(i);
                }
                System.Threading.WaitHandle.WaitAll(threadWaitHandles);
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }
        
        public override String getUrl(VideoInfo video)
        {
            if (video.PlaybackOptions == null || video.PlaybackOptions.Count == 0)
            {
                string dataPage = GetWebData(video.VideoUrl);
                video.PlaybackOptions = new Dictionary<string, string>();
                Match match = regEx_VideoUrlOptions.Match(dataPage);
                List<string[]> options = new List<string[]>();
                while (match.Success)
                {
                    string[] infos = match.Groups["Info"].Value.Split(',');
                    for (int i = 0; i < infos.Length; i++) infos[i] = infos[i].Trim(new char[] { '"', ' ' });
                    options.Add(infos);
                    match = match.NextMatch();
                }
                options.Sort(new Comparison<string[]>(delegate(string[] a, string[] b)
                    {
                        return int.Parse(a[1]).CompareTo(int.Parse(b[1]));
                    }));
                foreach(string[] infos in options)
                {
                    int type = int.Parse(infos[0]);
                    VideoQuality quality = (VideoQuality)int.Parse(infos[1]);
                    string resultUrl = "";
                    if (infos[infos.Length - 2].Contains("rtmp"))
                    {
                        Uri uri = new Uri(infos[infos.Length - 2]);
                        if (uri.Host == "gffstream.fcod.llnwd.net")
                        {
                            resultUrl = uri.OriginalString.Trim('/') + "/" + infos[infos.Length - 1].Trim(new char[] { '"', ' ' });
                        }
                        else
                        {
                            resultUrl = ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                                string.Format("http://127.0.0.1/stream.flv?hostname={0}&port={1}&app={2}&tcUrl={3}&playpath={4}",
                                    HttpUtility.UrlEncode(uri.Host),
                                    "1935",
                                    HttpUtility.UrlEncode(infos[infos.Length - 2].Substring(infos[infos.Length - 2].IndexOf('/', uri.Host.Length)).Trim('/')),
                                    HttpUtility.UrlEncode(infos[infos.Length - 2]),
                                    HttpUtility.UrlEncode(infos[infos.Length - 1].Trim(new char[] { '"', ' ' }))));
                        }
                        video.PlaybackOptions.Add(string.Format("{0} | rtmp:// | {1}", quality.ToString().PadRight(4, ' '), infos[infos.Length - 1].ToLower().Contains("mp4:") ? ".mp4" : ".flv"), resultUrl);
                    }
                    else
                    {
                        resultUrl = infos[infos.Length - 1].Trim(new char[] { '"', ' ' });                        
                        if (!resultUrl.EndsWith(".mp3"))
                        {
                            try
                            {
                                Uri uri = new Uri(resultUrl);
                                video.PlaybackOptions.Add(string.Format("{0} | {1}:// | {2}", quality.ToString().PadRight(4, ' '), uri.Scheme, System.IO.Path.GetExtension(resultUrl)), uri.AbsoluteUri);
                                if (resultUrl.EndsWith(".asx"))
                                {
                                    resultUrl = ParseASX(resultUrl)[0];
                                    uri = new Uri(resultUrl);
                                    video.PlaybackOptions.Add(string.Format("{0} | {1}:// | {2}", quality.ToString().PadRight(4, ' '), uri.Scheme, System.IO.Path.GetExtension(resultUrl)), uri.AbsoluteUri);
                                }                            
                            }
                            catch { }
                        }
                    }
                }
            }

            if (video.PlaybackOptions == null || video.PlaybackOptions.Count == 0)
            {
                return ""; // no url to play available
            }
            else if (video.PlaybackOptions.Count == 1 || videoQuality == VideoQuality.Low)
            {
                //user wants low quality or only one playback option -> use first
                string[] values = new string[video.PlaybackOptions.Count];
                video.PlaybackOptions.Values.CopyTo(values, 0);
                return values[0];
            }
            else if (videoQuality == VideoQuality.Max)
            {
                // take highest available quality
                string[] values = new string[video.PlaybackOptions.Count];
                video.PlaybackOptions.Values.CopyTo(values, 0);
                return values[values.Length - 1];
            }
            else // choose a high quality from options (first below Max)
            {
                string[] keys = new string[video.PlaybackOptions.Count];
                video.PlaybackOptions.Keys.CopyTo(keys, 0);
                int index = keys.Length - 1;
                while (index > 0 && keys[index].StartsWith(VideoQuality.Max.ToString())) index--;
                return video.PlaybackOptions[keys[index]];
            }
        }

        protected List<VideoInfo> getVideoListForCurrentCategory()
        {
            List<VideoInfo> listOfLinks = new List<VideoInfo>();
            string dataPage = GetWebData(lastQueriedCategoryUrl);
            Match m = regEx_VideoList.Match(dataPage);
            while (m.Success)
            {
                VideoInfo videoInfo = new VideoInfo();
                videoInfo.Title = HttpUtility.HtmlDecode(m.Groups["Title"].Value);
                videoInfo.VideoUrl = "http://www.ardmediathek.de" + m.Groups["Url"].Value;
                videoInfo.Length = m.Groups["Duration"].Value;
                videoInfo.ImageUrl = "http://www.ardmediathek.de" + m.Groups["ImageUrl"].Value;
                listOfLinks.Add(videoInfo);
                m = m.NextMatch();
            }
            
            Match mN = regEx_NextPage.Match(dataPage);
            lastQueriedCategoryHasNextPage = mN.Success;
            Match mP = regEx_PrevPage.Match(dataPage);
            lastQueriedCategoryHasPreviousPage = mP.Success;

            return listOfLinks;            
        }

        string lastQueriedCategoryUrl = "";
        uint lastQueriedCategoryPageIndex = 1;
        public override List<VideoInfo> getVideoList(Category category)
        {
            lastQueriedCategoryUrl = (category as RssLink).Url;
            lastQueriedCategoryPageIndex = 1;
            lastQueriedCategoryHasPreviousPage = false;
            lastQueriedCategoryHasNextPage = false;        
            return getVideoListForCurrentCategory();
        }

        #region Next/Previous Page

        bool lastQueriedCategoryHasNextPage = false;
        public override bool HasNextPage
        {
            get { return lastQueriedCategoryHasNextPage; }
        }

        bool lastQueriedCategoryHasPreviousPage = false;
        public override bool HasPreviousPage
        {
            get { return lastQueriedCategoryHasPreviousPage; }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            if (lastQueriedCategoryHasNextPage)
            {
                lastQueriedCategoryPageIndex++;
                if (lastQueriedCategoryUrl.IndexOf("goto=") < 0)
                {
                    lastQueriedCategoryUrl = lastQueriedCategoryUrl.Insert(lastQueriedCategoryUrl.LastIndexOf("/"), "/goto=" + lastQueriedCategoryPageIndex);
                }
                else
                {
                    lastQueriedCategoryUrl = lastQueriedCategoryUrl.Replace("goto=" + (lastQueriedCategoryPageIndex - 1), "goto=" + lastQueriedCategoryPageIndex);
                }
                return getVideoListForCurrentCategory();
            }
            else
            {
                return new List<VideoInfo>();
            }
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            if (lastQueriedCategoryHasPreviousPage)
            {
                lastQueriedCategoryPageIndex--;
                lastQueriedCategoryUrl = lastQueriedCategoryUrl.Replace("goto=" + (lastQueriedCategoryPageIndex + 1), "goto=" + lastQueriedCategoryPageIndex);
                return getVideoListForCurrentCategory();
            }
            else
            {
                return new List<VideoInfo>();
            }
        }

        #endregion

        #region Search

        public override bool CanSearch { get { return true; } }

        public override List<VideoInfo> Search(string query)
        {
            return getVideoList(new RssLink() { Url = string.Format(searchUrl, query) });
        }

        #endregion
    }
}

