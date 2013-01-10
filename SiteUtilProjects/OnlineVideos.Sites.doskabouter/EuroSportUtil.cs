using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Text;
using System.Net;
using System.Xml;
using System.IO;
using System.Web;

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

            SubcatRegex = new Regex(@"<option value=""UpdateAjaxVod\('(?<url>[^']*)'[^>]*>(?<title>[^<]*)</option>");
            baseUrl = String.Format(@"http://www.eurosportplayer.{0}/", tld);

            CookieContainer cc = new CookieContainer();

            string url = baseUrl + "_wsplayerxrm_/PlayerCrmApi.asmx/Login";
            string postData = @"data={""ul"":""" + emailAddress + @""",""p"":""" + password +
                @"""}&context={""g"":""" + tld.ToUpper() + @""",""d"":""1"",""s"":""1"",""p"":""1"",""b"":""Desktop"",""bp"":""""}";

            string cookies = @"ns_cookietest=true,ns_session=true";
            string[] myCookies = cookies.Split(',');
            foreach (string aCookie in myCookies)
            {
                string[] name_value = aCookie.Split('=');
                Cookie c = new Cookie();
                c.Name = name_value[0];
                c.Value = name_value[1];
                c.Expires = DateTime.Now.AddHours(1);
                c.Domain = new Uri(url).Host;
                cc.Add(c);
            }

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
            category.Url = baseUrl + "tv.shtml";
            category.Name = "Live TV";
            category.Other = kind.Live;
            Settings.Categories.Add(category);

            category = new RssLink();
            category.Url = baseUrl + "vod.shtml";
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
            string data = GetWebData((parentCategory as RssLink).Url, newcc);
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

        public override List<VideoInfo> getVideoList(Category category)
        {
            if (kind.Live.Equals(category.Other))
                return GetVideoListFromLive(category);
            else
                return GetVideoListFromVideo(category);
        }

        private List<VideoInfo> GetVideoListFromVideo(Category category)
        {
            XmlDocument doc = new XmlDocument();
            string webData = GetWebData(((RssLink)category).Url, newcc).Replace(@"xmlns:genExt=""http://twilight.eurosport.com/XsltExtensions/General""", String.Empty);
            doc.LoadXml(webData);
            List<VideoInfo> result = new List<VideoInfo>();
            foreach (XmlNode node in doc.SelectNodes("//array"))
            {
                VideoInfo video = new VideoInfo();
                video.Title = node.SelectSingleNode("title").InnerText;
                video.ImageUrl = node.SelectSingleNode("img").InnerText;
                video.Length = '|' + Translation.Instance.Airdate + ": " + node.SelectSingleNode("date").InnerText;
                video.VideoUrl = baseUrl + node.SelectSingleNode("link").InnerText.TrimStart('/');
                video.Other = category.Other;
                result.Add(video);
            }
            return result;
        }

        private List<VideoInfo> GetVideoListFromLive(Category category)
        {
            string getData = GetWebData(((RssLink)category).Url, newcc);
            Match m = Regex.Match(getData, @"<param\sname=""InitParams""\svalue=""lang=(?<lang>[^,]*),geoloc=(?<geoloc>[^,]*),realip=(?<realip>[^,]*),ut=(?<ut>[^,]*),ht=(?<ht>[^,]*),rt=(?<rt>[^,]*),vidid=(?<vidid>[^,]*),cuvid=(?<cuvid>[^,]*),prdid=(?<prdid>[^""]*)""\s/>");

            string post = String.Format(@"<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
<s:Body xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
<FindDefaultProductShortsByCountryAndService xmlns=""http://tempuri.org/"">
<countryCode>{0}</countryCode>
<type>Live</type>
<partnerCode />
<languageId>{1}</languageId>
<sportId>{2}</sportId>
<realIp>{3}</realIp>
<service>1</service>
<userId>{4}</userId>
<hkey>{5}</hkey>
<responseLangId>{1}</responseLangId>
</FindDefaultProductShortsByCountryAndService>
</s:Body></s:Envelope>", tld.ToUpperInvariant(), m.Groups["lang"].Value, -1, m.Groups["realip"].Value,
                   m.Groups["ut"].Value, m.Groups["ht"].Value);

            string postData = GetWebDataFromPost("http://videoshop.eurosport.com/PlayerProductService.asmx",
                post, @"SOAPAction: ""http://tempuri.org/FindDefaultProductShortsByCountryAndService""");

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(postData);

            nsmRequest = new XmlNamespaceManager(doc.NameTable);
            nsmRequest.AddNamespace("a", "http://tempuri.org/");

            XmlNodeList list = doc.SelectNodes("//a:PlayerProduct", nsmRequest);

            List<VideoInfo> videos = new List<VideoInfo>();
            foreach (XmlNode prod in list)
            {
                VideoInfo video = new VideoInfo();
                video.Title = null;
                XmlNodeList nodeList = prod.SelectNodes("a:livestreams/a:livestream", nsmRequest);
                foreach (XmlNode stream in nodeList)
                {
                    string name = stream.SelectSingleNode("a:name", nsmRequest).InnerText;
                    if (video.Title == null)
                        video.Title = name;
                    else
                    {
                        int ind = 0;
                        while (ind < video.Title.Length && ind < name.Length && video.Title[ind] == name[ind])
                            ind++;
                        video.Title = name.Substring(0, ind);
                    }
                }

                video.ImageUrl = String.Format(@"http://layout.eurosportplayer.{0}/i", tld) + prod.SelectSingleNode("a:vignetteurl", nsmRequest).InnerText;
                XmlNode descr = prod.SelectSingleNode("a:channellivesublabel", nsmRequest);
                if (descr != null)
                    video.Description = descr.InnerText;
                video.Other = nodeList;

                videos.Add(video);
            }

            return videos;

        }

        public override string getUrl(VideoInfo video)
        {
            if (kind.Video.Equals(video.Other))
                return GetUrlFromVideo(video);
            return GetUrlFromLive(video);
        }

        private string GetUrlFromVideo(VideoInfo video)
        {
            string getData = GetWebData(video.VideoUrl, newcc);
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
            XmlNodeList streams = (XmlNodeList)video.Other;
            video.PlaybackOptions = new Dictionary<string, string>();
            foreach (XmlNode stream in streams)
            {
                string securedUrl = stream.SelectSingleNode("a:securedurl", nsmRequest).InnerText;
                XmlNode nameNode = stream.SelectSingleNode("a:label/a:name", nsmRequest);
                string name;
                if (nameNode != null)
                    name = stream.SelectSingleNode("a:label/a:name", nsmRequest).InnerText;
                else
                    name = String.Empty;
                if (!Uri.IsWellFormedUriString(securedUrl, System.UriKind.Absolute))
                {
                    string url = stream.SelectSingleNode("a:url", nsmRequest).InnerText;
                    securedUrl = new Uri(new Uri(url), securedUrl).AbsoluteUri;
                }

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


    }
}
