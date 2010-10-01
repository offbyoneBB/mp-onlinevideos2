using System;
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
        protected string tv4DynamicCategoriesRegEx = @"<a\s+href=""(?<url>[^""]+\D)""\s+class=""play"">(?<title>[^""]*)</a>";

        protected string tv4DynamicSubCategoriesRegEx = @"<li\s+class=""button[^""]*"">\W+<h3><a\s+class=""\{tabUrl:\'[^=]+=(?<url>\d\.\d+)&amp;ajax=selection[^>]+>(?<title>[^<]+)</a>";

        Regex regEx_dynamicCategories, regEx_dynamicSubCategories;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            if (!string.IsNullOrEmpty(tv4DynamicCategoriesRegEx)) regEx_dynamicCategories = new Regex(tv4DynamicCategoriesRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            if (!string.IsNullOrEmpty(tv4DynamicSubCategoriesRegEx)) regEx_dynamicSubCategories = new Regex(tv4DynamicSubCategoriesRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
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
                    Match m = parentCategory.ParentCategory == null ? regEx_dynamicCategories.Match(data) : regEx_dynamicSubCategories.Match(data);
                    while (m.Success)
                    {
                        RssLink cat = new RssLink();
                        cat.Url = m.Groups["url"].Value;
                        if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(tv4BaseUrl), cat.Url).AbsoluteUri;
                        cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim().Replace('\n', ' '));
                        parentCategory.SubCategories.Add(cat);
                        cat.Other = "TV4";
                        cat.HasSubCategories = parentCategory.ParentCategory == null;
                        cat.ParentCategory = parentCategory;
                        m = m.NextMatch();
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

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            if (category.Other == "TV4")
            {
                XmlDocument xDoc = GetWebData<XmlDocument>((category as RssLink).Url);
                XmlNamespaceManager nsmRequest = new XmlNamespaceManager(xDoc.NameTable);
                nsmRequest.AddNamespace("ns1", "http://www.tv4.se/xml/contentinfo");
                foreach (XmlElement e in xDoc.SelectNodes("//ns1:content[ns1:contentType/text() = 'VIDEO']", nsmRequest))
                {
                    VideoInfo video = new VideoInfo();
                    video.Title = e.SelectSingleNode("ns1:title", nsmRequest).InnerText;
                    video.Description = e.SelectSingleNode("ns1:text", nsmRequest).InnerText;
                    DateTime parsedPubDate;
                    video.Length = DateTime.TryParse(e.SelectSingleNode("ns1:publishedDate", nsmRequest).InnerText, out parsedPubDate) ? parsedPubDate.ToString("g", OnlineVideoSettings.Instance.Locale) : "";
                    video.ImageUrl = e.SelectSingleNode("ns1:w219imageUrl", nsmRequest).InnerText;
                    video.VideoUrl = e.SelectSingleNode("ns1:vmanProgramId", nsmRequest).InnerText;
                    video.Other = "TV4";
                    videos.Add(video);
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
                    if (result == string.Empty) result = host + videoElem.GetAttribute("src");
                    video.PlaybackOptions.Add(string.Format("{0} kbps", int.Parse(videoElem.GetAttribute("system-bitrate")) / 1000), host + videoElem.GetAttribute("src"));
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
