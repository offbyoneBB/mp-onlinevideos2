using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Text;
using System.Net;
using System.Xml;
using System.IO;
using System.Web;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class EuroSportUtil : SiteUtilBase
    {

        [Category("OnlineVideosUserConfiguration"), Description("The tld for the eurosportplayer url, e.g. nl, co.uk or de")]
        string tld = null;

        [Category("OnlineVideosUserConfiguration"), Description("Email address of your eurosport account")]
        string emailAddress = null;
        [Category("OnlineVideosUserConfiguration"), Description("Password of your eurosport account")]
        string password = null;

        private string baseUrl;
        private Regex SubcatRegex;

        private XmlNamespaceManager nsmRequest;
        private enum kind { Live, Video };
        private CookieContainer newcc = new CookieContainer();

        public override int DiscoverDynamicCategories()
        {
            if (tld == null || emailAddress == null || password == null)
                return 0;
            Log.Debug("tld, emailaddress and password != null");

            SubcatRegex = new Regex(@"<li\sclass=""vod-menu-element-sports-element""\sdata-sporturl=""(?<url>[^""]*)""\sdata-filter=""sports"">(?<title>[^<]*)</li>");
            baseUrl = String.Format(@"http://www.eurosportplayer.{0}/", tld);

            CookieContainer cc = new CookieContainer();

            string url = baseUrl + "_wsplayerxrm_/PlayerCrmApi_v5.svc/Login";
            string postData =
                @"{""data"":""{\""ul\"":\""" + emailAddress + @"\"",\""p\"":\""" + password +
                @"\"",\""r\"":false}"",""context"":""{\""g\"":\""" + tld.ToUpper() + @"\"",\""d\"":\""1\"",\""s\"":\""1\"",\""p\"":\""1\"",\""b\"":\""Desktop\"",\""bp\"":\""\""}""}";

            string res = GetWebDataFromPost(url, postData, cc);
            if (!res.Contains(@"<Success>1</Success>"))
            {
                Log.Error("Eurosport: login unsuccessfull");
                Log.Debug("login unsuccessfull");// so it's in mediaportal.log as wel ass onlinevideos.log
            }

            CookieCollection ccol = cc.GetCookies(new Uri(baseUrl));
            foreach (Cookie c in ccol)
            {
                Log.Debug("Add cookie " + c.ToString());
                newcc.Add(c);
            }


            RssLink category = new RssLink();
            category.Url = baseUrl + "live.shtml";
            category.Name = "Live TV";
            category.Other = kind.Live;
            Settings.Categories.Add(category);

            category = new RssLink();
            category.Url = baseUrl + "on-demand.shtml";
            category.Name = "Videos";
            category.Other = kind.Video;
            category.HasSubCategories = true;
            Settings.Categories.Add(category);

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;

        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            // currently: only for Videos
            string data = GetWebData((parentCategory as RssLink).Url, cookies: newcc);
            if (!string.IsNullOrEmpty(data))
            {
                parentCategory.SubCategories = new List<Category>();
                Match m = SubcatRegex.Match(data);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Url = baseUrl + m.Groups["url"].Value.TrimStart('/');
                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim());
                    cat.ParentCategory = parentCategory;
                    cat.Other = parentCategory.Other;
                    parentCategory.SubCategories.Add(cat);
                    m = m.NextMatch();
                }
                parentCategory.SubCategoriesDiscovered = true;
            }
            return parentCategory.SubCategories == null ? 0 : parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            if (kind.Live.Equals(category.Other))
                return GetVideoListFromLive(category);
            else
                return GetVideoListFromVideo(category);
        }

        private List<VideoInfo> GetVideoListFromVideo(Category category)
        {
            XmlDocument doc = new XmlDocument();
            string webData = GetWebData(((RssLink)category).Url, cookies: newcc);
            doc.LoadXml(webData);
            List<VideoInfo> result = new List<VideoInfo>();
            foreach (XmlNode node in doc.SelectNodes("//catchups"))
            {
                VideoInfo video = new VideoInfo();
                video.Title = node.SelectSingleNode("titlecatchup").InnerText;
                video.ImageUrl = node.SelectSingleNode("thumbnail/url").InnerText;
                video.Length = Utils.PlainTextFromHtml(node.SelectSingleNode("durationInSeconds").InnerText);
                video.Airdate = Utils.PlainTextFromHtml(node.SelectSingleNode("startdate/date").InnerText);
                video.VideoUrl = baseUrl + node.SelectSingleNode("url").InnerText.TrimStart('/');
                video.Other = category.Other;
                result.Add(video);
            }
            return result;
        }

        private string getValue(string webData, string id)
        {
            Match m = Regex.Match(webData, @"Ply\.[^\.]*\.add\('" + id + @"',\s*'(?<value>[^']*)'");
            if (m.Success)
                return m.Groups["value"].Value;
            return null;
        }

        private List<VideoInfo> GetVideoListFromLive(Category category)
        {
            string webData = GetWebData(((RssLink)category).Url, cookies: newcc);

            string url = baseUrl + String.Format(@"_wsvideoshop_/JsonProductService.svc/GetAllProducts?device=1&isocode={0}&languageid={1}&hkey={2}&userid={3}",
                tld.ToUpperInvariant(), getValue(webData, "languageid"),
                getValue(webData, "hashkey"), getValue(webData, "userid"));
            webData = GetWebData(url, cookies: newcc);

            JToken alldata = JObject.Parse(webData) as JToken;
            JArray jVideos = alldata["PlayerObj"] as JArray;
            List<VideoInfo> videos = new List<VideoInfo>();

            foreach (JToken jVideo in jVideos)
            {
                VideoInfo video = new VideoInfo();
                video.Title = jVideo["channellabel"].Value<string>();

                JArray jLiveStreams = jVideo["livestreams"] as JArray;

                foreach (JToken stream in jLiveStreams)
                {
                    string name = stream["name"].Value<string>();
                    if (!String.IsNullOrEmpty(name))
                        video.Title = name;
                }

                video.ImageUrl = String.Format(@"http://layout.eurosportplayer.{0}/i", tld) + jVideo["vignetteurl"].Value<string>();
                video.Description = jVideo["channellivesublabel"].Value<string>();
                video.Other = jLiveStreams;
                videos.Add(video);
            }
            return videos;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            if (kind.Video.Equals(video.Other))
                return GetUrlFromVideo(video);
            return GetUrlFromLive(video);
        }

        private string GetUrlFromVideo(VideoInfo video)
        {
            string getData = GetWebData(video.VideoUrl, cookies: newcc);
            Match m = Regex.Match(getData, @"<param\sname=""InitParams""\svalue=""lang=(?<lang>[^,]*),geoloc=(?<geoloc>[^,]*),realip=(?<realip>[^,]*),ut=(?<ut>[^,]*),ht=(?<ht>[^,]*),rt=(?<rt>[^,]*),vidid=(?<vidid>[^,]*),cuvid=(?<cuvid>[^,]*),prdid=(?<prdid>[^""]*)""\s/>");
            string postData;
            bool catchUp = m.Groups["vidid"].Value == "-1";
            if (catchUp)
            {
                string post = String.Format(@"<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
<s:Body xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
<GetCatchUpVideoSecurized xmlns=""http://tempuri.org/"">
<catchUpVideoId>{0}</catchUpVideoId>
<geolocCountry>{1}</geolocCountry>
<realIp>{4}</realIp>
<userId>{5}</userId>
<hkey>{6}</hkey>
<responseLangId>{2}</responseLangId>
</GetCatchUpVideoSecurized>
</s:Body></s:Envelope>", m.Groups["cuvid"].Value, tld.ToUpperInvariant(), m.Groups["lang"].Value, 1, m.Groups["realip"].Value,
                       m.Groups["ut"].Value, m.Groups["ht"].Value);

                postData = GetWebDataFromPost("http://videoshop.eurosport.com/PlayerCatchupService.asmx",
                    post, @"SOAPAction: ""http://tempuri.org/GetCatchUpVideoSecurized""");
            }
            else
            {

                string post = String.Format(@"<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
<s:Body xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
<GetVideoSecurizedAsync xmlns=""http://tempuri.org/"">
<videoId>{0}</videoId>
<videoPartnerCode />
<countryCode>{1}</countryCode>
<videoLanguageId>{2}</videoLanguageId>
<service>{3}</service>
<realIp>{4}</realIp>
<userId>{5}</userId>
<hkey>{6}</hkey>
<responseLangId>{2}</responseLangId>
</GetVideoSecurizedAsync>
</s:Body></s:Envelope>", m.Groups["vidid"].Value, tld.ToUpperInvariant(), m.Groups["lang"].Value, 1, m.Groups["realip"].Value,
                       m.Groups["ut"].Value, m.Groups["ht"].Value);

                postData = GetWebDataFromPost("http://videoshop.eurosport.com/PlayerVideoService.asmx",
                    post, @"SOAPAction: ""http://tempuri.org/GetVideoSecurizedAsync""");
            }
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(postData);

            nsmRequest = new XmlNamespaceManager(doc.NameTable);
            nsmRequest.AddNamespace("a", "http://tempuri.org/");

            XmlNode uri = catchUp ? doc.SelectSingleNode("//a:catchupstream/a:securedurl", nsmRequest) :
                doc.SelectSingleNode("//a:playlistitem/a:uri", nsmRequest);
            if (uri != null)
                return uri.InnerText;
            return null;
        }

        private string GetUrlFromLive(VideoInfo video)
        {
            JArray streams = (JArray)video.Other;
            video.PlaybackOptions = new Dictionary<string, string>();
            foreach (JToken stream in streams)
            {
                string securedUrl = stream["securedurl"].Value<string>();
                string name = (video.PlaybackOptions.Count + 1).ToString();
                video.PlaybackOptions.Add(name, securedUrl);
            }

            string resultUrl;
            if (video.PlaybackOptions.Count == 0) return "";// if no match, return empty url -> error
            else
            {
                // return first found url as default
                var enumer = video.PlaybackOptions.GetEnumerator();
                enumer.MoveNext();
                resultUrl = enumer.Current.Value;
            }
            if (video.PlaybackOptions.Count == 1) video.PlaybackOptions = null;// only one url found, PlaybackOptions not needed

            return resultUrl;
        }

        private static string GetWebDataFromPost(string url, string postData, string headerExtra)
        {
            Log.Debug("get webdata from {0}", url);
            Log.Debug("postdata = " + postData);
            Log.Debug("headerExtra = " + headerExtra);

            // request the data
            byte[] data = Encoding.UTF8.GetBytes(postData);

            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            if (request == null) return "";
            request.Method = "POST";
            request.ContentType = "text/xml; charset=utf-8";
            request.UserAgent = OnlineVideoSettings.Instance.UserAgent;
            request.Timeout = 15000;
            request.ContentLength = data.Length;
            request.ProtocolVersion = HttpVersion.Version10;
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
            request.Headers.Add(headerExtra);

            Stream requestStream = request.GetRequestStream();
            requestStream.Write(data, 0, data.Length);
            requestStream.Close();
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                Stream responseStream;
                if (response.ContentEncoding.ToLower().Contains("gzip"))
                    responseStream = new System.IO.Compression.GZipStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                else if (response.ContentEncoding.ToLower().Contains("deflate"))
                    responseStream = new System.IO.Compression.DeflateStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                else
                    responseStream = response.GetResponseStream();

                Encoding encoding = Encoding.UTF8;
                encoding = Encoding.GetEncoding(response.CharacterSet.Trim(new char[] { ' ', '"' }));

                StreamReader reader = new StreamReader(responseStream, encoding, true);
                string str = reader.ReadToEnd();
                return str.Trim();
            }

        }


        private static string GetWebDataFromPost(string url, string postData, CookieContainer cc)
        {
            Log.Debug("get webdata from {0}", url);
            Log.Debug("postdata = " + postData);

            // request the data
            byte[] data = Encoding.UTF8.GetBytes(postData);

            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            if (request == null) return "";
            request.Method = "POST";
            request.ContentType = "application/json";
            request.UserAgent = OnlineVideoSettings.Instance.UserAgent;
            request.Timeout = 15000;
            request.ContentLength = data.Length;
            request.ProtocolVersion = HttpVersion.Version10;
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
            request.CookieContainer = cc;

            Stream requestStream = request.GetRequestStream();
            requestStream.Write(data, 0, data.Length);
            requestStream.Close();
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                Stream responseStream;
                if (response.ContentEncoding.ToLower().Contains("gzip"))
                    responseStream = new System.IO.Compression.GZipStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                else if (response.ContentEncoding.ToLower().Contains("deflate"))
                    responseStream = new System.IO.Compression.DeflateStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                else
                    responseStream = response.GetResponseStream();

                Encoding encoding = Encoding.UTF8;
                encoding = Encoding.GetEncoding(response.CharacterSet.Trim(new char[] { ' ', '"' }));

                StreamReader reader = new StreamReader(responseStream, encoding, true);
                string str = reader.ReadToEnd();
                return str.Trim();
            }

        }


    }
}
