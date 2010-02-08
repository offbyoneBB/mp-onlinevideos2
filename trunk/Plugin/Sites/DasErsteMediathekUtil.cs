using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{    
    public class DasErsteMediathekUtil : SiteUtilBase
    {
        /*
        public enum VideoQuality { Low, High };

        [Category("OnlineVideosUserConfiguration"), Description("Low or high quality for the videos according to bandwidth.")]
        VideoQuality videoQuality = VideoQuality.High;
        */

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            string dataPage = GetWebData("http://www.ardmediathek.de/ard/servlet/");

            string categories_Regex = @"<div\sclass=""mt-reset\smt-categories"">\s*
<ul>\s*
(?:<li><a\shref=""(?<Url>[^""]+)""[^>]*>(?<Title>[^<]+)</a></li>\s*)+
</ul>\s*
</div>";
            Regex regex_Categories = new Regex(categories_Regex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

            Match m = regex_Categories.Match(dataPage);
            if(m.Success)
            {
                for (int i = 0; i < m.Groups["Title"].Captures.Count; i++)
                {
                    RssLink item = new RssLink() { HasSubCategories = true };
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
            parentCategory.SubCategories = new List<Category>();

            string dataPage = GetWebData(((RssLink)parentCategory).Url);

            List<string> additionalPageUrls = new List<string>();
            string additionalPages_Regex = @"<option\svalue=""(?<Url>[^""]+)"">[^<]*</option>";
            Regex regex_AdditionalPages = new Regex(additionalPages_Regex, RegexOptions.Compiled | RegexOptions.CultureInvariant);
            Match mPage = regex_AdditionalPages.Match(dataPage);
            while (mPage.Success)
            {
                additionalPageUrls.Add(mPage.Groups["Url"].Value);
                mPage = mPage.NextMatch();
            }

            string subcategories_Regex = @"<div\sclass=""mt-media_item"">\s*
<div\sclass=""mt-image"">\s*
<img\ssrc=""(?<ImageUrl>[^""]+)""[^/]*/>\s*
</div>\s*
<h3\sclass=""mt-title""><a\shref=""(?<Url>[^""]+)""\s[^>]*>(?<Title>[^<]+)</a></h3>";                        
            Regex regex_Subcategories = new Regex(subcategories_Regex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

            Match m = regex_Subcategories.Match(dataPage);
            while (m.Success)
            {
                RssLink subCat = new RssLink()
                {
                    Name = m.Groups["Title"].Value,
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
                            Match addM = regex_Subcategories.Match(addDataPage);
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
            string dataPage = GetWebData(video.VideoUrl);

            Dictionary<int, Dictionary<int, string>> urls = new Dictionary<int, Dictionary<int, string>>();

            Regex regex_Options = new Regex(@"mediaCollection.addMediaStream\((?<Info>[^)]+)");
            Match match = regex_Options.Match(dataPage);
            while (match.Success)
            {
                string[] infos = match.Groups["Info"].Value.Split(new char[] { ',' });
                int type = int.Parse(infos[0].Trim(new char[] { '"', ' ' }));
                int quality = int.Parse(infos[1].Trim(new char[] { '"', ' ' }));
                string resultUrl = "";
                if (infos[infos.Length - 2].Contains("rtmp"))
                {
                    Uri uri = new Uri(infos[infos.Length - 2].Trim(new char[] { '"', ' ' }));
                    if (uri.Host == "gffstream.fcod.llnwd.net")
                    {
                        resultUrl = string.Format("http://127.0.0.1:{0}/stream.flv?rtmpurl={1}",
                            OnlineVideoSettings.RTMP_PROXY_PORT,
                            HttpUtility.UrlEncode(uri.OriginalString + "/" + infos[infos.Length - 1].Trim(new char[] { '"', ' ' })));
                    }
                    else
                    {
                        resultUrl = string.Format("http://127.0.0.1:{5}/stream.flv?hostname={0}&port={1}&app={2}&tcUrl={3}&playpath={4}",
                            HttpUtility.UrlEncode(uri.Host),
                            "1935",
                            HttpUtility.UrlEncode(uri.Segments[1].Trim(new char[] { '/' })),
                            HttpUtility.UrlEncode("rtmp://" + uri.Host + ":1935" + uri.Segments[0] + uri.Segments[1].Trim(new char[] { '/' })),
                            HttpUtility.UrlEncode(infos[infos.Length - 1].Trim(new char[] { '"', ' ' })),
                            OnlineVideoSettings.RTMP_PROXY_PORT);
                    }
                }
                else
                {
                    resultUrl = infos[infos.Length - 1].Trim(new char[] { '"', ' ' });
                    if (resultUrl.EndsWith(".asx"))
                    {
                        resultUrl = ParseASX(resultUrl)[0];
                    }
                }
                if (!urls.ContainsKey(type)) urls.Add(type, new Dictionary<int, string>());
                if (!urls[type].ContainsKey(quality)) urls[type].Add(quality, "");
                urls[type][quality] = resultUrl;
                match = match.NextMatch();
            }            
            List<int> qualities = new List<int>();
            foreach(int quality in urls[0].Keys) qualities.Add(quality);
            qualities.Sort();
            return urls[0][qualities[qualities.Count-1]];
        }

        protected List<VideoInfo> getVideoListForCurrentCategory()
        {
            List<VideoInfo> listOfLinks = new List<VideoInfo>();

            string dataPage = GetWebData(lastQueriedCategoryUrl);

            string videoList_Regex = @"<div\sclass=""mt-media_item"">\s*
<div\sclass=""mt-image"">\s*
<span\sclass=""mt-icon\smt-icon_video""></span>\s*
<img\ssrc=""(?<ImageUrl>[^""]+)""\s[^/]*/>\s*
</div>\s*
<h3\sclass=""mt-title""><a\shref=""(?<Url>[^""]+)""[^>]*>(?<Title>[^<]+)</a></h3>\s*
<p[^>]*>[^<]*</p>\s*
<p\sclass=""mt-airtime_channel""><span\sclass=""mt-airtime"">(?<Duration>[^<]+)</span>";
            Regex regex_VideoList = new Regex(videoList_Regex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

            Match m = regex_VideoList.Match(dataPage);
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

            string nextPageRegExp = @"<a\s+href=""(?<NextPageUrl>[^""]+)""\s[^>]*>Weiter</a>";
            string prevPageRegExp = @"<a\s+href=""(?<PrevPageUrl>[^""]+)""\s[^>]*>Zurück</a>";
            Regex regex_NextPage = new Regex(nextPageRegExp, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
            Regex regex_PrevPage = new Regex(prevPageRegExp, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
            
            Match mN = regex_NextPage.Match(dataPage);
            lastQueriedCategoryHasNextPage = mN.Success;
            Match mP = regex_PrevPage.Match(dataPage);
            lastQueriedCategoryHasPreviousPage = mP.Success;

            return listOfLinks;            
        }

        string lastQueriedCategoryUrl = "";
        uint lastQueriedCategoryPageIndex = 1;
        bool lastQueriedCategoryHasPreviousPage = false;
        bool lastQueriedCategoryHasNextPage = false;        
        public override List<VideoInfo> getVideoList(Category category)
        {
            lastQueriedCategoryUrl = (category as RssLink).Url;
            lastQueriedCategoryPageIndex = 1;
            lastQueriedCategoryHasPreviousPage = false;
            lastQueriedCategoryHasNextPage = false;        
            return getVideoListForCurrentCategory();
        }        
        
        public override bool HasNextPage
        {
            get { return lastQueriedCategoryHasNextPage; }
        }        

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
    }
}

