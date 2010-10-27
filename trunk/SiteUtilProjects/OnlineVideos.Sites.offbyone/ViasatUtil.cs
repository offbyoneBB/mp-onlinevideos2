using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Xml;
using System.Web;

namespace OnlineVideos.Sites
{
    public class ViasatUtil : SiteUtilBase
    {
        enum ViasatChannel { Sport = 1, TV3, TV6, TV8 };

        [Category("OnlineVideosConfiguration"), Description("TV4 Base Url")]
        protected string tv4BaseUrl = "http://www.tv4play.se/";
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the tv4BaseUrl for dynamic categories. Group names: 'url', 'title'.")]
        protected string tv4DynamicCategoriesRegEx;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the videos from html. Group names: 'url', 'title', 'thumb', 'date'.")]
        protected string tv4VideolistRegEx;
        protected string tv4VideolistRegEx2 = @"(?<=(Visa\sfler.*))<input\stype=""hidden""\sname=""(?<name>[^""]*)""\svalue=""(?<value>[^""]*)"">";
        protected string tv4DynamicSubCategoriesRegEx = @"(?<!(/search/partial.*))<input\stype=""hidden""\sname=""(?<name>[^""]*)""\svalue=""(?<value>[^""]*)"">";
        protected string tv4DynamicSubCategoriesRegEx2 = @"<li\s+class=""video-panel\s+(?!(clip))[^""]*"">\s*<p[^>]*>\s*<a\s+href=""(?<url>[^""]*)"">\s*<img\s+alt=""[^""]*""\s+src=""(?<thumb>[^""]*)"">\s*</a>\s*</p>\s*<div[^>]*>\s*<h3[^>]*>\s*<a[^>]*>(?<title>[^<]+)</a>\s*</h3>\s*<p[^>]*>\s*(?<desc>[^<]+)</p>";                

        Regex regEx_dynamicCategories, regEx_dynamicSubCategories, regEx_dynamicSubCategories2, regEx_tv4VideoList, regEx_tv4VideoList2;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            if (!string.IsNullOrEmpty(tv4DynamicCategoriesRegEx)) regEx_dynamicCategories = new Regex(tv4DynamicCategoriesRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            if (!string.IsNullOrEmpty(tv4DynamicSubCategoriesRegEx)) regEx_dynamicSubCategories = new Regex(tv4DynamicSubCategoriesRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            if (!string.IsNullOrEmpty(tv4DynamicSubCategoriesRegEx2)) regEx_dynamicSubCategories2 = new Regex(tv4DynamicSubCategoriesRegEx2, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            if (!string.IsNullOrEmpty(tv4VideolistRegEx)) regEx_tv4VideoList = new Regex(tv4VideolistRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            if (!string.IsNullOrEmpty(tv4VideolistRegEx2)) regEx_tv4VideoList2 = new Regex(tv4VideolistRegEx2, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            foreach (int i in Enum.GetValues(typeof(ViasatChannel)))
            {
                Category cat = new Category() { Name = ((ViasatChannel)i).ToString(), HasSubCategories = true, SubCategoriesDiscovered = false };
                Settings.Categories.Add(cat);
            }
            Settings.Categories.Add(new RssLink() { Name = "TV4", HasSubCategories = true, SubCategoriesDiscovered = false, Url = tv4BaseUrl, Other = "TV4" });

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (parentCategory.Other == "TV4")
            {
                string data = GetWebData((parentCategory as RssLink).Url);
                if (!string.IsNullOrEmpty(data))
                {
                    parentCategory.SubCategories = new List<Category>();

                    if (parentCategory.ParentCategory == null)
                    {
                        Match m = regEx_dynamicCategories.Match(data);
                        while (m.Success)
                        {
                            RssLink cat = new RssLink();
                            cat.Url = m.Groups["url"].Value;
                            if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(tv4BaseUrl), cat.Url).AbsoluteUri;
                            cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim().Replace('\n', ' '));
                            parentCategory.SubCategories.Add(cat);
                            cat.Other = "TV4";
                            cat.HasSubCategories = true;
                            cat.ParentCategory = parentCategory;
                            m = m.NextMatch();
                        }
                    }
                    else
                    {
                        string jsonUrl = "http://www.tv4play.se/programformatsearch?";
                        bool found = false;
                        Match m = regEx_dynamicSubCategories.Match(data);
                        while (m.Success)
                        {
                            found = true;
                            jsonUrl += string.Format("{0}{1}={2}", jsonUrl.EndsWith("?") ? "" : "&", m.Groups["name"].Value, m.Groups["name"].Value == "rows" ? "100" : System.Web.HttpUtility.UrlEncode(m.Groups["value"].Value));
                            m = m.NextMatch();
                        }
                        if (found)
                        {
                            Newtonsoft.Json.Linq.JObject json = GetWebData<Newtonsoft.Json.Linq.JObject>(jsonUrl);
                            if (json != null)
                            {
                                foreach (var result in json["results"])
                                {
                                    RssLink cat = new RssLink();
                                    cat.Url = result.Value<string>("href");
                                    if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(tv4BaseUrl), cat.Url).AbsoluteUri;
                                    cat.Name = result.Value<string>("name");
                                    cat.Thumb = result.Value<string>("smallformatimage");
                                    cat.Description = result.Value<string>("text");
                                    cat.Other = "TV4";
                                    parentCategory.SubCategories.Add(cat);
                                    cat.ParentCategory = parentCategory;
                                }
                            }
                        }
                        else
                        {
                            Match m2 = regEx_dynamicSubCategories2.Match(data);
                            while (m2.Success)
                            {
                                RssLink cat = new RssLink();
                                cat.Url = m2.Groups["url"].Value;
                                if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(tv4BaseUrl), cat.Url).AbsoluteUri;
                                cat.Name = HttpUtility.HtmlDecode(m2.Groups["title"].Value.Trim().Replace('\n', ' '));
                                cat.Thumb = m2.Groups["thumb"].Value;
                                cat.Description = HttpUtility.HtmlDecode(m2.Groups["desc"].Value.Trim());
                                cat.Other = "TV4";
                                parentCategory.SubCategories.Add(cat);
                                cat.ParentCategory = parentCategory;
                                m2 = m2.NextMatch();
                            }
                        }
                    }
                }
            }
            else
            {
                Category channelCategory = parentCategory;
                while (channelCategory.ParentCategory != null) channelCategory = channelCategory.ParentCategory;
                ViasatChannel channel = (ViasatChannel)Enum.Parse(typeof(ViasatChannel), channelCategory.Name);
                string id = parentCategory is RssLink ? ((RssLink)parentCategory).Url : "0";
                XmlDocument xDoc;
                if (channel != ViasatChannel.Sport)
                {
                    xDoc = GetWebData<XmlDocument>("http://viastream.viasat.tv/siteMapData/se/" + ((int)channel).ToString() + "se/" + id);
                }
                else
                {
                    xDoc = GetWebData<XmlDocument>("http://viastream.player.mtgnewmedia.se/xml/xmltoplayer.php?type=siteMapData&channel=" + ((int)channel).ToString() + "se&country=se&category=" + id);
                }
                parentCategory.SubCategories = new List<Category>();
                foreach (XmlElement e in xDoc.SelectNodes("//siteMapNode"))
                {
                    RssLink subCat = new RssLink() { Name = e.Attributes["title"].Value, Url = e.Attributes["id"].Value, ParentCategory = parentCategory };
                    if (e.Attributes["children"].Value == "true") subCat.HasSubCategories = true;
                    parentCategory.SubCategories.Add(subCat);
                }
            }
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        int pagingCurrentStart = 0;
        string pagingUrl;
        public override List<VideoInfo> getNextPageVideos()
        {
            string rowsString = Regex.Match(pagingUrl, @"rows=(\d+)").Groups[1].Value;
            int rows = 12;
            if (rowsString != "") int.TryParse(rowsString, out rows);
            int incrementIndex = pagingUrl.IndexOf("start=");
            if (incrementIndex < 0)
            {
                pagingCurrentStart += rows;
                pagingUrl += "&start=" + pagingCurrentStart.ToString();
            }
            HasPreviousPage = true;
            return ParseVideos(new RssLink() { Url = pagingUrl, Other = "TV4" });
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            string rowsString = Regex.Match(pagingUrl, @"rows=(\d+)").Groups[1].Value;
            int rows = 12;
            if (rowsString != "") int.TryParse(rowsString, out rows);
            int incrementIndex = pagingUrl.IndexOf("start=");
            if (incrementIndex < 0)
            {
                pagingCurrentStart -= rows;
                if (pagingCurrentStart <= 0)
                {
                    pagingCurrentStart = 0;
                    HasPreviousPage = false;
                }
                else
                    pagingUrl += "&start=" + pagingCurrentStart.ToString();
            }
            return ParseVideos(new RssLink() { Url = pagingUrl, Other = "TV4" });
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            pagingCurrentStart = 0;
            HasNextPage = false;
            HasPreviousPage = false;
            return ParseVideos(category);
        }

        List<VideoInfo> ParseVideos(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            if (category.Other == "TV4")
            {
                string data = GetWebData((category as RssLink).Url);
                Match m = regEx_tv4VideoList.Match(data);
                while (m.Success)
                {
                    VideoInfo video = new VideoInfo();
                    video.Title = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim().Replace('\n', ' '));
                    video.Length = HttpUtility.HtmlDecode(m.Groups["date"].Value.Trim());
                    video.ImageUrl = m.Groups["thumb"].Value;
                    video.VideoUrl = m.Groups["url"].Value;
                    video.Other = "TV4";
                    videos.Add(video);
                    m = m.NextMatch();
                }
                pagingUrl = "http://www.tv4play.se/search/partial?";
                m = regEx_tv4VideoList2.Match(data);
                while (m.Success)
                {
                    HasNextPage = true;
                    pagingUrl += string.Format("{0}{1}={2}", pagingUrl.EndsWith("?") ? "" : "&", m.Groups["name"].Value, System.Web.HttpUtility.UrlEncode(m.Groups["value"].Value));
                    m = m.NextMatch();
                }
            }
            else
            {
                Category channelCategory = category;
                while (channelCategory.ParentCategory != null) channelCategory = channelCategory.ParentCategory;
                ViasatChannel channel = (ViasatChannel)Enum.Parse(typeof(ViasatChannel), channelCategory.Name);
                string doc = "";
                if (channel != ViasatChannel.Sport)
                {
                    doc = GetWebData("http://viastream.viasat.tv/Products/Category/" + ((RssLink)category).Url);
                }
                else
                {
                    doc = GetWebData("http://viastream.player.mtgnewmedia.se/xml/xmltoplayer.php?type=Products&category=" + ((RssLink)category).Url);
                }
                XmlDocument xDoc = new XmlDocument();
                xDoc.LoadXml(doc);
                foreach (XmlElement e in xDoc.SelectNodes("//Product"))
                {
                    VideoInfo video = new VideoInfo();
                    video.Title = e.SelectSingleNode("Title").InnerText;
                    video.VideoUrl = e.SelectSingleNode("ProductId").InnerText;
                    video.Other = channel.ToString();
                    videos.Add(video);
                }
            }
            return videos;
        }

        public override string getUrl(VideoInfo video)
        {
            if (video.Other == "TV4")
            {
                string result = string.Empty;
                video.PlaybackOptions = new Dictionary<string, string>();
                XmlDocument xDoc = GetWebData<XmlDocument>(string.Format("http://anytime.tv4.se/webtv/metafileFlash.smil?p={0}&bw=1000&emulate=true&sl=true", video.VideoUrl));
                string host = xDoc.SelectSingleNode("//meta[@base]/@base").Value;
                foreach (XmlElement videoElem in xDoc.SelectNodes("//video[@src]"))
                {
                    result = host + videoElem.GetAttribute("src");
                    video.PlaybackOptions.Add(string.Format("{0} kbps", int.Parse(videoElem.GetAttribute("system-bitrate")) / 1000), result);
                }
                return result;
            }
            else
            {
                ViasatChannel channel = (ViasatChannel)Enum.Parse(typeof(ViasatChannel), (string)video.Other);                
                string doc = "";
                if (channel != ViasatChannel.Sport)
                {
                    doc = GetWebData("http://viastream.viasat.tv/Products/" + video.VideoUrl);
                }
                else
                {
                    doc = GetWebData("http://viastream.player.mtgnewmedia.se/xml/xmltoplayer.php?type=Products&clipid=" + video.VideoUrl);
                }

                doc = System.Text.RegularExpressions.Regex.Replace(doc, "&(?!amp;)", "&amp;");
                XmlDocument xDoc = new XmlDocument();
                xDoc.LoadXml(doc);

                string playstr = xDoc.SelectSingleNode("Products/Product/Videos/Video/Url").InnerText;

                XmlNode geo;
                if ((geo = xDoc.SelectSingleNode("Products/Product/Geoblock")) != null)
                {
                    if (geo.InnerText == "true")
                    {
                        xDoc.LoadXml(GetWebData(playstr));
                        playstr = xDoc.SelectSingleNode("GeoLock/Url").InnerText;

                        if (playstr.ToLower().StartsWith("rtmp"))
                        {
                            playstr = ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                                string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&swfurl{1}=&swfhash={2}&swfsize={3}",
                                System.Web.HttpUtility.UrlEncode(playstr),
                                System.Web.HttpUtility.UrlEncode("http://flvplayer.viastream.viasat.tv/flvplayer/play/swf/player100920.swf"),
                                "9d14a8849c059734a62544c44bdc252fe6961c00f50739c4cd12fca42a503b41",
                                1326663));
                        }
                    }
                }

                return playstr;
            }
        }
    }
}
