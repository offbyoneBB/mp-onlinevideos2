using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Xml;
using System.Web;

namespace OnlineVideos.Sites
{
    public class WebDAVUtil : SiteUtilBase
    {
        [Category("OnlineVideosUserConfiguration"), Description("The base url of the webdav server (f.e. https://servername.com:8080")]
        private string basePath = null;

        [Category("OnlineVideosUserConfiguration"), Description("The username for accessing the webdav server")]
        private string userName = null;

        [Category("OnlineVideosUserConfiguration"), Description("The password for accessing the webdav server")]
        private string password = null;

        private string fullBasePath = "";

        private const string PropFindRequestContent =
            "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" +
            "<propfind xmlns=\"DAV:\">" +
            "<prop>" +
            "<resourcetype/>" +
            "</prop>" +
            "</propfind>";

        public override int DiscoverDynamicCategories()
        {
            fullBasePath = basePath.Replace(@"://", @"://" + userName + ":" + password + "@");

            foreach (var cat in getDirectory(basePath))
                Settings.Categories.Add(cat);
            Settings.DynamicCategoriesDiscovered = true;

            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = getDirectory(((RssLink)parentCategory).Url);
            foreach (var cat in parentCategory.SubCategories)
            {
                cat.ParentCategory = parentCategory;
            }
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> res = new List<VideoInfo>();

            Tuple<XmlDocument, XmlNamespaceManager> result = getData(((RssLink)category).Url);

            foreach (XmlNode node in result.Item1.SelectNodes(@"//a:response[not(a:propstat/a:prop/a:resourcetype/a:collection)]", result.Item2))
            {
                VideoInfo vid = new VideoInfo()
                {
                    VideoUrl = fullBasePath + node.SelectSingleNode(".//a:href", result.Item2).InnerText
                };
                int p = vid.VideoUrl.LastIndexOf('/');
                vid.Title = HttpUtility.UrlDecode(vid.VideoUrl.Substring(p + 1));

                TrackingInfo tInfo = new TrackingInfo()
                {
                    Regex = Regex.Match(vid.Title, @"^(?<Title>.*?)\s*-\s*(?<Season>\d+)x(?<Episode>\d+)", RegexOptions.IgnoreCase),
                    VideoKind = VideoKind.TvSeries
                };
                if (tInfo.Season == 0)
                {
                    //probably movie
                    tInfo.Regex = Regex.Match(vid.Title, @"^(?<Title>.*?)\s*-\s*(?<Year>\d{4})", RegexOptions.IgnoreCase);
                    tInfo.VideoKind = VideoKind.Movie;
                }
                vid.Other = tInfo;

                res.Add(vid);
            }
            res.Sort(VideoComparer);
            return res;
        }

        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            if (video.Other is ITrackingInfo)
                return video.Other as ITrackingInfo;

            return base.GetTrackingInfo(video);
        }

        private List<Category> getDirectory(string path)
        {
            List<Category> res = new List<Category>();
            Tuple<XmlDocument, XmlNamespaceManager> result = getData(path);

            bool first = true;
            foreach (XmlNode node in result.Item1.SelectNodes(@"//a:response[a:propstat/a:prop/a:resourcetype/a:collection]", result.Item2))
            {
                RssLink cat = new RssLink()
                {
                    Url = basePath + node.SelectSingleNode(".//a:href", result.Item2).InnerText,
                    HasSubCategories = true
                };
                cat.Name = HttpUtility.UrlDecode(Path.GetFileName(cat.Url.TrimEnd('/')));

                if (!first) res.Add(cat);
                first = false;
            }
            res.Sort(CategoryComparer);
            if (result.Item1.SelectNodes(@"//a:response[not(a:propstat/a:prop/a:resourcetype/a:collection)]", result.Item2).Count != 0)
            {
                res.Add(new RssLink()
                {
                    Url = path,
                    Name = "FolderContent"
                });
            }
            return res;
        }

        private Tuple<XmlDocument, XmlNamespaceManager> getData(string url)
        {
            NameValueCollection headers = new NameValueCollection();
            headers.Add("Depth", "1");
            var data = MyGetWebData(new Uri(url), headers, "PROPFIND", PropFindRequestContent);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(data);
            XmlNamespaceManager mgr = new XmlNamespaceManager(doc.NameTable);
            mgr.AddNamespace("a", "DAV:");

            return new Tuple<XmlDocument, XmlNamespaceManager>(doc, mgr);
        }

        private int CategoryComparer(Category cat1, Category cat2)
        {
            return String.Compare(cat1.Name, cat2.Name);
        }
        private int VideoComparer(VideoInfo vid1, VideoInfo vid2)
        {
            return String.Compare(vid1.Title, vid2.Title);
        }

        private string MyGetWebData(Uri uri, NameValueCollection headers, string method, string content)
        {
            HttpWebResponse response = null;
            try
            {
                // build a CRC of the url and all headers + proxy + cookies for caching
                string requestCRC = Helpers.EncryptionUtils.CalculateCRC32(
                    string.Format("{0}{1}{2}{3}",
                    uri,
                    headers != null ? string.Join("&", (from item in headers.AllKeys select string.Format("{0}={1}", item, headers[item])).ToArray()) : "",
                    "",
                    ""));

                // try cache first
                string cachedData = WebCache.Instance[requestCRC];
                if (cachedData != null) return cachedData;

                // build the request
                HttpWebRequest request = WebRequest.Create(uri) as HttpWebRequest;
                if (request == null) return "";
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate; // turn on automatic decompression of both formats (adds header "AcceptEncoding: gzip,deflate" to the request)
                foreach (var headerName in headers.AllKeys)
                {
                    request.Headers.Set(headerName, headers[headerName]);
                }
                request.Method = method;
                request.Credentials = new NetworkCredential(userName, password);

                if (content != null)
                {
                    request.ContentType = "text/xml";
                    byte[] data = Encoding.UTF8.GetBytes(content);
                    request.ContentLength = data.Length;
                    request.ProtocolVersion = HttpVersion.Version10;
                    Stream requestStream = request.GetRequestStream();
                    requestStream.Write(data, 0, data.Length);
                    requestStream.Close();
                }

                // request the data
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (WebException webEx)
                {
                    Log.Debug(webEx.Message);
                    response = (HttpWebResponse)webEx.Response; // if the server returns a 404 or similar .net will throw a WebException that has the response
                }
                Stream responseStream = response.GetResponseStream();

                // UTF8 is the default encoding as fallback
                Encoding responseEncoding = Encoding.UTF8;
                // try to get the response encoding if one was specified and neither forceUTF8 nor encoding were set as parameters
                if (response.CharacterSet != null && !String.IsNullOrEmpty(response.CharacterSet.Trim())) responseEncoding = Encoding.GetEncoding(response.CharacterSet.Trim(new char[] { ' ', '"' }));

                using (StreamReader reader = new StreamReader(responseStream, responseEncoding, true))
                {
                    string str = reader.ReadToEnd().Trim();
                    // add to cache if HTTP Status was 200 and we got more than 500 bytes (might just be an errorpage otherwise)
                    if (response.StatusCode == HttpStatusCode.OK && str.Length > 500) WebCache.Instance[requestCRC] = str;
                    return str;
                }
            }
            finally
            {
                if (response != null) ((IDisposable)response).Dispose();
            }
        }
    }

}
