using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Web;
using System.Threading;
using System.Collections.Specialized;
using HybridDSP.Net.HTTP;
using System.ComponentModel;
using RTMP_LIB;

namespace OnlineVideos.Sites
{
    public class YleAreenaUtil : GenericSiteUtil, IRequestHandler
    {

        [Category("OnlineVideosConfiguration"), Description("flashurl to use for the rtmprequests")]
        protected string flashurl = null;
        [Category("OnlineVideosConfiguration"), Description("regex for the a-z video list")]
        protected string atozVideolist = null;
        [Category("OnlineVideosConfiguration"), Description("regex for the a-z categories")]
        protected string atozCategories = null;
        [Category("OnlineVideosConfiguration"), Description("regex for the a-z subcategories")]
        protected string atozSubcategories = null;

        private Regex rtmpUrlRegEx;
        private string clipId;
        private Regex atozRegex;
        private Regex atozSubRegex;
        private Regex atozVideolistRegex;
        private Regex frontpagevideolistRegex;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            RegexOptions defaultRegexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture;

            rtmpUrlRegEx = new Regex(@"""url"":""(?<url>[^""]*)""", defaultRegexOptions);
            atozRegex = new Regex(atozCategories, defaultRegexOptions);
            atozSubRegex = new Regex(atozSubcategories, defaultRegexOptions);
            atozVideolistRegex = new Regex(atozVideolist, defaultRegexOptions);
            frontpagevideolistRegex = regEx_VideoList;
        }

        public override int DiscoverDynamicCategories()
        {
            int nrStatic = Settings.Categories.Count;
            int res = base.DiscoverDynamicCategories();
            for (int i = 0; i < res + nrStatic; i++)
            {
                Settings.Categories[i].HasSubCategories = i < nrStatic;
                if (!Settings.Categories[i].HasSubCategories)
                    Settings.Categories[i].Other = frontpagevideolistRegex;
            }
            return res;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            //atoz
            string webData = GetWebData(((RssLink)parentCategory).Url);
            parentCategory.SubCategories = new List<Category>();
            Match m = atozRegex.Match(webData);
            while (m.Success)
            {
                Category subcat = new Category();
                subcat.ParentCategory = parentCategory;
                subcat.HasSubCategories = true;
                subcat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                subcat.SubCategories = new List<Category>();
                parentCategory.SubCategories.Add(subcat);
                string anch = m.Groups["url"].Value;
                int p = webData.IndexOf(@"<div id=""" + anch + @""">");
                int q = webData.IndexOf(@"<div id=""anchor", p + 1);
                if (q == -1)
                    q = webData.IndexOf(@"</tbody>");
                string subset = webData.Substring(p, q - p);
                Match m2 = atozSubRegex.Match(subset);
                while (m2.Success)
                {
                    RssLink subsubcat = new RssLink();
                    subsubcat.ParentCategory = subcat;
                    subsubcat.Name = HttpUtility.HtmlDecode(m2.Groups["title"].Value);
                    subsubcat.Url = new Uri(new Uri(baseUrl), m2.Groups["url"].Value).AbsoluteUri;
                    subsubcat.Description = String.Format("videos: {0} Audio: {1}", m2.Groups["videocount"].Value, m2.Groups["audiocount"].Value);
                    subsubcat.Other = atozVideolistRegex;
                    subcat.SubCategories.Add(subsubcat);
                    m2 = m2.NextMatch();
                }
                subcat.SubCategoriesDiscovered = true;
                m = m.NextMatch();
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            regEx_VideoList = category.Other as Regex;
            List<VideoInfo> res = base.getVideoList(category);
            foreach (VideoInfo video in res)
            {
                video.Title = HttpUtility.HtmlDecode(video.Title);
                if (String.IsNullOrEmpty(video.Title))
                    video.Title = "No title";
                video.Description = HttpUtility.HtmlDecode(video.Description);
                video.Length = HttpUtility.HtmlDecode(video.Length);
                video.CleanDescriptionAndTitle();
            }
            return res;
        }

        public override string getUrl(VideoInfo video)
        {
            int i = video.VideoUrl.LastIndexOf('/');

            clipId = video.VideoUrl.Substring(i + 1);
            //"1619954";

            if (!ReverseProxy.Instance.HasHandler(this)) ReverseProxy.Instance.AddHandler(this);

            return ReverseProxy.Instance.GetProxyUri(this,
                string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}",
                HttpUtility.UrlEncode("rtmp://" + flashurl + "/AreenaServer/video.mp4")));
        }

        private bool UnknownMethodHandler(string method, AMFObject obj, RTMP rtmp)
        {
            if (method == "authenticationDetails")
            {
                int randomAuth = Convert.ToInt32(obj.GetProperty(3).GetObject().GetProperty("randomAuth").GetNumber());
                int authResult = (randomAuth + 447537687) % 6834253;
                rtmp.SendFlex("authenticateRandomNumber", authResult);
                if (rtmp.GetExpectedPacket("randomNumberAuthenticated", out obj))
                {
                    rtmp.SendRequestData("e0", "session/authenticate/1");
                    rtmp.GetExpectedPacket("rpcResult", out obj);
                    rtmp.SendRequestData("e1", "clips/info/" + clipId);
                    if (rtmp.GetExpectedPacket("rpcResult", out obj))
                    {
                        string s = obj.GetProperty(4).GetString();
                        Match m = rtmpUrlRegEx.Match(s);
                        if (m.Success)
                        {
                            string t = m.Groups["url"].Value;
                            t = t.Replace(@"\/", "/");
                            //rtmp://flashu.yle.fi/AreenaServer/maailma/1/61/99/1619955_691405.mp4
                            string ext = Path.GetExtension(t).Trim('.');
                            t = Path.ChangeExtension(t, String.Empty).Trim('.');
                            rtmp.Link.playpath = t.Replace(@"rtmp://" + flashurl + "/AreenaServer/", ext + ":");
                            rtmp.SendRequestData("e3", "clips/featured/" + clipId);
                            rtmp.SendCreateStream();
                        }
                        return false;
                    }
                }
            }
            return false;
        }

        #region IRequestHandler
        bool invalidHeader = false;

        public bool DetectInvalidPackageHeader()
        {
            return invalidHeader;
        }

        public void HandleRequest(string url, HTTPServerRequest request, HTTPServerResponse response)
        {
            Thread.CurrentThread.Name = "RTMPYleProxy";
            Logger.Log("Request from yle url=" + url);
            RTMP rtmp = null;
            try
            {
                NameValueCollection paramsHash = System.Web.HttpUtility.ParseQueryString(new Uri(url).Query);

                Link link = Link.FromRtmpUrl(new Uri(paramsHash["rtmpurl"]));
                link.flashVer = "WIN 10,0,32,18";
                link.swfUrl = @"http://areena.yle.fi/player/Application.swf?build=2";
                link.tcUrl = @"rtmp://" + flashurl + ":1935/AreenaServer";
                link.pageUrl = @"http://areena.yle.fi/video/" + clipId;


                rtmp = new RTMP() { Link = link };
                rtmp.SkipCreateStream = true;
                rtmp.MethodHookHandler = UnknownMethodHandler;

                RTMPRequestHandler.ConnectAndGetStream(rtmp, request, response, ref invalidHeader);
            }
            finally
            {
                if (rtmp != null) rtmp.Close();
            }

            Logger.Log("Request finished.");
        }
        #endregion
    }
}
