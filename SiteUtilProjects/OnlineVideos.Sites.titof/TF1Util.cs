using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public partial class TF1Util : SiteUtilBase
    {
        protected int indexPage = 1;
        protected List<string> listPages = new List<string>();
        
        private static Regex videoIdByMediaRegex = new Regex(@"id=""media""\svalue=""(?<videoId>[^""]*)""",
                                                        RegexOptions.Compiled);
        private static Regex videoIdByMediaIdRegex = new Regex(@"mediaId\s*:\s*(?<videoId>[^,]*),",
                                                               RegexOptions.Compiled);
        private static Regex videoIdByToolbarRegex = new Regex(@"new\sNewtoolbar\(""entry"",""(?<videoId>[^""]*)""",
                                                               RegexOptions.Compiled);

        public override int DiscoverDynamicCategories()
        {           
            RssLink cat = new RssLink();
            cat.Url = "http://www.tf1.fr/series-etrangeres/index-UyBUSVRSRQ==.html";
            cat.Name = "Séries étrangères";
            cat.Other = "les-episodes-serie-tv/";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://www.tf1.fr/fictions-francaises/index-UyBUSVRSRQ==.html";
            cat.Name = "Fictions françaises";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://www.tf1.fr/telerealites/index-UyBUSVRSRQ==.html";
            cat.Name = "Télé-réalité";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://www.tf1.fr/magazine/index-UyBUSVRSRQ==.html";
            cat.Name = "Magazine";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://www.tf1.fr/divertissement/index-UyBUSVRSRQ==.html";
            cat.Name = "Divertissement";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://www.tf1.fr/programmes-tv-info/index-UyBUSVRSRQ==.html";
            cat.Name = "Info";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://www.tf1.fr/sport/index-UyBUSVRSRQ==.html";
            cat.Name = "Sport";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://www.tf1.fr/jeux-tv/index-UyBUSVRSRQ==.html";
            cat.Name = "Jeux TV";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://www.tf1.fr/programmes-tv-jeunesse/index-UyBUSVRSRQ==.html";
            cat.Name = "Jeunesse";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://www.tf1.fr/programmes-tv-jeunesse/index-UyBUSVRSRQ==.html";
            cat.Name = "Jeunesse (Tous)";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            Settings.DynamicCategoriesDiscovered = true;

            return 10;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            List<RssLink> listDates = new List<RssLink>();

            string webData = GetWebData((parentCategory as RssLink).Url);
            Regex r = null;
            Match m = null;

            if (parentCategory.Name.Equals("Jeunesse (Tous)"))
            {
                r = new Regex(@"(?<=<a\shref=""/programmes-tv-jeunesse/""\s>\sAccueil.*)(?<!MultiUnesCategories.*)<li.*?class=""[^""]*"">.*?<a.*?href=""(?<url>[^""]*)"".*?>(?<title>[^<]*)</a>.*?</li>",
                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);

                m = r.Match(webData);
                while (m.Success)
                {
                    if (m.Groups["url"].Value.StartsWith("http"))
                    {
                        RssLink date = new RssLink();
                        date.Url = m.Groups["url"].Value;
                        date.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                        date.ParentCategory = parentCategory;
                        listDates.Add(date);
                    }
                    
                    m = m.NextMatch();
                }

                parentCategory.SubCategories.AddRange(listDates.ToArray());

                return listDates.Count;
            }
            else if (parentCategory.Name.Equals("Jeunesse"))
            {
                r = new Regex(@"<div\sclass=""visuel"">\s<a\sonmousedown=""[^""]*""\shref=""[^""]*""\s*rel=""nofollow""><img\ssrc=""(?<thumb>[^""]*)""\salt=""[^""]*""\scp=""[^""]*""></a>\s</div>\s</div>\s<div\sclass=""[^""]*"">\s<h3\sclass=""[^""]*""><a\shref=""(?<url>[^""]*)""\sclass=""[^""]*"">(?<title>[^<]*)</a></h3>",
                 RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);

                m = r.Match(webData);
                while (m.Success)
                {
                    if (m.Groups["url"].Value.StartsWith("http"))
                    {
                        RssLink date = new RssLink();
                        date.Url = m.Groups["url"].Value;
                        date.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                        date.Thumb = m.Groups["thumb"].Value;
                        date.ParentCategory = parentCategory;
                        listDates.Add(date);
                    }

                    m = m.NextMatch();
                }

                parentCategory.SubCategories.AddRange(listDates.ToArray());

                return listDates.Count;
            }

            


            r = new Regex(@"<li\sclass=""prg.*?"">.*?<img\ssrc=""(?<thumb>[^""]*)""\salt=""[^""]*""></a><h2\sclass=""t4\sf1""><a\sclass=""cc""\shref=""(?<url>[^""]*)"">(?<title>[^<]*)</a></h2>",
                RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            
            m = r.Match(webData);
            while (m.Success)
            {
                RssLink date = new RssLink();
                date.Url =  m.Groups["url"].Value;
                date.Name =  m.Groups["title"].Value;
                date.Thumb = m.Groups["thumb"].Value;
                date.ParentCategory = parentCategory;
                listDates.Add(date);
                m = m.NextMatch();
            }

            //Recherche des pages suivantes
            r = new Regex(@"<a\shref=""(?<url>[^""]*)""\sclass=""c2\st3"">(?<title>[^<]*)</a></li>",
                RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            m = r.Match(webData);
            
            //Pour chaque page suivante
            while (m.Success)
            {
                string webData2 = GetWebData("http://www.tf1.fr" + m.Groups["url"].Value);

                Regex r2 = new Regex(@"<li\sclass=""prg.*?"">.*?<img\ssrc=""(?<thumb>[^""]*)""\salt=""[^""]*""></a><h2\sclass=""t4\sf1""><a\sclass=""cc""\shref=""(?<url>[^""]*)"">(?<title>[^<]*)</a></h2>",
                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);

                Match m2 = r2.Match(webData2);
                while (m2.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Url = m2.Groups["url"].Value;
                    cat.Name = m2.Groups["title"].Value;
                    cat.Thumb = m2.Groups["thumb"].Value;
                    cat.ParentCategory = parentCategory;
                    listDates.Add(cat);
                    m2 = m2.NextMatch();
                }
                m = m.NextMatch();
            }

            parentCategory.SubCategories.AddRange(listDates.ToArray());

            return listDates.Count;
   
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string baseVideos = "http://videos.tf1.fr";
            string url = (category as RssLink).Url;

            if (url.StartsWith("http://"))
            {
                url = url.Replace("http://www.tf1.fr", baseVideos);
            }
            else
            {
                url = baseVideos + url;
            }

            List<VideoInfo> listVideos = new List<VideoInfo>();
            string webData = "";
            try
            {
                webData = GetWebData(url);
            }
            catch (Exception)
            {
                return listVideos;
            }

            if (listPages.Count == 0)
            {
                listPages.Add(url);
                Regex reg_nextPage = new Regex(@"<li\s(?:class=""[^""]*"")?><a\shref=""(?<url>[^""]*)""\sclass="".*?""\s>(?<title>[^<]*)</a></li>",
                RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
                Match m_nextPage = reg_nextPage.Match(webData);

                while (m_nextPage.Success)
                {
                    listPages.Add(m_nextPage.Groups["url"].Value);
                    m_nextPage = m_nextPage.NextMatch();
                }

            }

            string regex_Type1 = @"<li\sclass=""teaser[^""]*""><a[^>]*><img\ssrc=""(?<thumb>[^""]*)""[^>]*>(<span\sclass=""[^""]*"">.*?</span>)?</a><div\sclass=""description""><div\sclass=""[^""]*"">(?<duree>[^<]*)</div><h2\sclass=""[^""]*""><a\shref=""(?<url>[^""]*)""\sclass=""[^""]*"">(?<title>[^<]*)</a></h2><p\sclass=""[^""]*"">(?<description>[^<]*)</p>";
            
            Regex r = new Regex(regex_Type1,
                RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            Match m = r.Match(webData);
             
            while (m.Success)
            {
                VideoInfo video = new VideoInfo();
                video.VideoUrl = baseVideos + m.Groups["url"].Value;
                video.Title = m.Groups["title"].Value;
                video.Length = m.Groups["duree"].Value;
                video.Description = m.Groups["description"].Value;
                video.ImageUrl = m.Groups["thumb"].Value;

                listVideos.Add(video);
                m = m.NextMatch();
            }
            
            return listVideos;
            
        }

        public override bool HasNextPage
        {
            get
            {
                return listPages != null && listPages.Count > 1 && indexPage < listPages.Count;
            }
            protected set
            {
                base.HasNextPage = value;
            }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            RssLink cat = new RssLink();
            indexPage++;
            cat.Url = listPages[indexPage -1];
            return getVideoList(cat);
        }

        public override bool HasPreviousPage
        {
            get
            {
                return listPages != null && listPages.Count > 1 && indexPage > 1;
            }
            protected set
            {
                base.HasPreviousPage = value;
            }
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            indexPage--;
            RssLink cat = new RssLink();
            cat.Url = listPages[indexPage -1];
            return getVideoList(cat);
        }

        public override List<string> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {            
            return _getVideosUrl(video);
        }

        public static List<string> _getVideosUrl(VideoInfo video)
        {
            List<string> listUrls = new List<string>();

            string webData = GetWebData(video.VideoUrl);
            string id = string.Empty;
            
            // check to see if videoId is in following format
            // <input type="hidden" id="media" value="10151643" />
            Match byMedia = videoIdByMediaRegex.Match(webData);
            if (byMedia.Success) { id = byMedia.Groups["videoId"].Value; }
            
            if (string.IsNullOrEmpty(id))
            {
                // if videoId is still empty, check if videoId is in following format
                // mediaId : 2283580,
                Match byMediaId = videoIdByMediaIdRegex.Match(webData);
                if (byMediaId.Success) { id = byMediaId.Groups["videoId"].Value; }
            }
            
            if (string.IsNullOrEmpty(id))
            {
                // if videoId is still empty, check if videoId is in following format
                // new Newtoolbar("entry","9986091"
                Match byToolbar = videoIdByToolbarRegex.Match(webData);
                if (byToolbar.Success) { id = byToolbar.Groups["videoId"].Value; }
            }
            
            if (string.IsNullOrEmpty(id))
            {
                // if videoId is still empty, log warning and return
                Log.Warn(@"Could not find videoId for {0} at URL: {1}", video.Title, video.VideoUrl);                
                return listUrls;
            }

            //Récupération du json
            webData = GetWebData("http://www.wat.tv/interface/contentv3/" + id);

            JObject j = JObject.Parse(webData);

            foreach (var jObject in j)
            {
                if (jObject.Key.Equals("media"))
                {
                    if (jObject.Value["files"] as JArray != null)
                    {
                        //Parcours tous les fichiers 
                        foreach (var jSubCategoryObject in jObject.Value["files"] as JArray)
                        {
                            string web = "";
                            id = jSubCategoryObject.Value<string>("id");
                            string hd = jSubCategoryObject.Value<string>("hasHD");
                            if (hd != null && hd.ToLower().Equals("true"))
                            {
                                web = "webhd";

                            }
                            else
                            {
                                web = "web";
                            }
                            string timeToken = getTimeToken();
                            string md5 = toMD52("9b673b13fa4682ed14c3cfa5af5310274b514c4133e9b3a81e6e3aba00912564/" + web + "/" + id + "" + timeToken);
                            string finalURL = getFinalUrl(md5, "http://www.wat.tv/get/" + web + "/" + id, timeToken, id);
                           
                            if (finalURL.StartsWith("http"))
                            {
                                listUrls.Add(new MPUrlSourceFilter.HttpUrl(finalURL) { UserAgent = OnlineVideoSettings.Instance.UserAgent}.ToString());
                            }
                            else
                            {
                                listUrls.Add(new MPUrlSourceFilter.RtmpUrl(finalURL) { SwfUrl = "http://www.wat.tv/images/v30/PlayerWat.swf", SwfVerify = true, ReceiveDataTimeout = 40000 }.ToString());
                            }
                        }
                        break;
                    }

                }
            }

            return listUrls;
        }

        private static string getFinalUrl(string token, string url, string timeToken, string id)
        {
            string webData = GetWebData(url + "?domain=videos.tf1.fr&country=FR&getURL=1&version=LNX%2010,0,45,2&token=" + token + "/" + timeToken, null, null, null, false, false, "Mozilla/5.0 (Windows; U; Windows NT 6.1; de; rv:1.9.1.3) Gecko/20090824 Firefox/3.5.3");
            if (webData.Contains("rtmpte://"))
            {
                webData = webData.Replace(webData.Substring(0, webData.IndexOf("://")), "rtmpe");
            }
            if (webData.Contains("rtmpe://"))
            {
                webData = webData.Replace(webData.Substring(0, webData.IndexOf("://")), "rtmpe");
            }
            if (webData.Contains("rtmp://"))
            {
                webData = webData.Replace(webData.Substring(0, webData.IndexOf("://")), "rtmp");
            }
            if (webData.Contains("rtmpt://"))
            {
                webData = webData.Replace(webData.Substring(0, webData.IndexOf("://")), "rtmp");
            }
            Log.Info("ReturnURL : " + webData);
            
            return webData;
        }

        private static string getTimeToken()
        {
            int delta = -3509;//delta temporaire entre mon PC et le serveur wat
            int time = Convert.ToInt32(GetTime() / 1000) + delta;
            string timesec = System.Convert.ToString(time, 16).ToLower();
            return timesec;
        }
   
        private static Int64 GetTime()
        {
            //Int64 retval=0;
           
            //string s = GetWebData("http://wiilook.netau.net/script/time.php");
            //s = s.Substring(1, s.IndexOf("<") - 1);
            //return Int64.Parse(s);
			Int64 retval=0;
            DateTime st= new DateTime(1970,1,1);
            TimeSpan t= (DateTime.Now.ToUniversalTime()-st);
            retval = (Int64)(t.TotalMilliseconds + 0.5);
            return retval;
            //DateTime st= new DateTime(1970,1,1);
            //TimeSpan t= (DateTime.Now.ToUniversalTime()-st);
            //retval = (Int64)(t.TotalMilliseconds + 0.5);
            //return retval;
        }

        /// <summary>
        /// Creates an MD5 hash of the input string as a 32 character hexadecimal string.
        /// </summary>
        /// <param name="input">Text to generate has for.</param>
        /// <returns>Hash as 32 character hexadecimal string.</returns>
        public static string toMD52(string input)
        {
            System.Security.Cryptography.MD5 md5Hasher;
            byte[] data;
            int count;
            StringBuilder result;

            md5Hasher = System.Security.Cryptography.MD5.Create();
            data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));

            // Loop through each byte of the hashed data and format each one as a hexadecimal string.
            result = new StringBuilder();
            for (count = 0; count < data.Length; count++)
            {
                result.Append(data[count].ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
            }

            return result.ToString();
        }
    }
}
