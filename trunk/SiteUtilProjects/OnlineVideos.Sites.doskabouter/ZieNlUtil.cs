using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Linq;
using System.IO;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class ZieNlUtil : GenericSiteUtil
    {
        Regex finalRegex;
        Regex jsonRegex;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            finalRegex = new Regex(@"<h3>\s*<a\sid=""component_[^_]*_title""\srel=""(?<url>[^""]*)""\shref=""[^""]*"">\s*(?<title>[^<]*)</a>\s*</h3>", defaultRegexOptions);
            jsonRegex = new Regex(@"new\sEsl_Component_List\('component_[^']*',\s(?<content>{[^}]*})\)", defaultRegexOptions);
        }

        public override int DiscoverDynamicCategories()
        {
            int res = base.DiscoverDynamicCategories();
            foreach (RssLink cat in Settings.Categories)
                cat.Other = true;// may check second menu line
            return res;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (true.Equals(parentCategory.Other))
            {
                base.DiscoverSubCategories(parentCategory);
                foreach (RssLink subcat in parentCategory.SubCategories)
                    subcat.HasSubCategories = true;
            }
            AddFinalLists((RssLink)parentCategory);
            return parentCategory.SubCategories == null ? 0 : parentCategory.SubCategories.Count;
        }

        private void AddFinalLists(RssLink parentCategory)
        {
            if (parentCategory.SubCategories == null) parentCategory.SubCategories = new List<Category>();
            string webData = GetWebData(parentCategory.Url);
            string[] subs = webData.Split(new[] { "component list alpha" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string sub in subs)
            {
                Match m = finalRegex.Match(sub);
                if (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Name = m.Groups["title"].Value.Trim();
                    cat.ParentCategory = parentCategory;
                    cat.Other = sub;
                    parentCategory.SubCategories.Add(cat);
                }
            }
            parentCategory.SubCategoriesDiscovered = true;
        }

        private List<VideoInfo> ParseData(string jsonData)
        {
            JToken json = JToken.Parse(jsonData);
            string content = null;
            StringBuilder sb = new StringBuilder();
            int pageNr = 1;
            sb.AppendFormat(@"http://www.zie.nl/getcomponent/id/{0}/?action=gotoPage", json.Value<string>("id"));
            foreach (JProperty j in json)
            {
                if (j.Name == "content")
                    content = j.Value.ToString();
                else
                    if (j.Name == "gotoPage")
                        pageNr = Int32.Parse(j.Value.ToString());
                    else
                    {
                        sb.Append("&transport[");
                        sb.Append(j.Name);
                        sb.Append("]=");
                        sb.Append(j.Value.ToString());
                    }
            }
            sb.AppendFormat(@"&transport[gotoPage]={0}", pageNr + 1);
            if (pageNr > 1)
                sb.AppendFormat(@"&transport[page]={0}", pageNr);
            nextPageUrl = sb.ToString();
            nextPageAvailable = true;
            return base.Parse(baseUrl, content);
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            string webData = (string)category.Other;
            string jsonData = jsonRegex.Match(webData).Groups["content"].Value;
            return ParseData(jsonData);
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            string webData = MyGetWebData(nextPageUrl);
            return ParseData(webData);
        }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            string webData = GetWebData(searchUrl, string.Format(searchPostString, query));
            string jsonData = jsonRegex.Match(webData).Groups["content"].Value;
            return ParseData(jsonData).ConvertAll<SearchResultItem>(v => v as SearchResultItem);
        }

        private static string MyGetWebData(string url)
        {
            // copied from SiteUtilBase. Changed the following to prevent escaping of [ and ]
            // Uri uri = new Uri(url, true);
            // HttpWebRequest request = WebRequest.Create(uri) as HttpWebRequest;

            HttpWebResponse response = null;
            try
            {
                NameValueCollection headers = new NameValueCollection();
                headers.Add("Accept", "*/*"); // accept any content type
                headers.Add("User-Agent", OnlineVideoSettings.Instance.UserAgent);

                // build a CRC of the url and all headers + proxy + cookies for caching
                string requestCRC = Utils.EncryptLine(
                    string.Format("{0}{1}{2}{3}",
                    url,
                    string.Join("&", (
                    from item in headers.AllKeys select string.Format("{0}={1}", item, headers[item])).ToArray()),
                    "",
                    ""));

                // try cache first
                string cachedData = WebCache.Instance[requestCRC];
                if (cachedData != null) return cachedData;

                // build the request
                Uri uri = new Uri(url, true);
                HttpWebRequest request = WebRequest.Create(uri) as HttpWebRequest;
                if (request == null) return "";
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate; // turn on automatic decompression of both formats so we can say we accept them
                request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate"); // we accept compressed content
                if (headers != null) // add user defined headers
                {
                    foreach (var headerName in headers.AllKeys)
                    {
                        switch (headerName.ToLowerInvariant())
                        {
                            case "accept":
                                request.Accept = headers[headerName];
                                break;
                            case "user-agent":
                                request.UserAgent = headers[headerName];
                                break;
                            case "referer":
                                request.Referer = headers[headerName];
                                break;
                            default:
                                request.Headers.Set(headerName, headers[headerName]);
                                break;
                        }
                    }
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
                // the caller did specify a forced encoding
                // the caller wants to force UTF8

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
                // disable unsafe header parsing if it was enabled
            }
        }




    }
}
