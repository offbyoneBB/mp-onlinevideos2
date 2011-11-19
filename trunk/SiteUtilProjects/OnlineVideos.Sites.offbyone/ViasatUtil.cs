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

        protected string tv4VideolistDividingRegEx = @"<div\sclass=""module-center-wrapper"">.*?<h2>.*?{0}.*?</h2>(?<data>.*?)</section>";
        protected string tv4VideolistNextPageRegEx = @"<a\s+href=""(?<url>/search\?categoryids=[^""]+)"".*?><span>Visa\sfler</span></a>";
        protected string tv4DynamicSubCategoriesRegEx = @"(<a\s+href=""/program_format_searches\.json\?ids=(?<ids>[^""&]+)[^""]*""><span>Alla</span></a>)|(<input\s+id=""ids""\s+name=""ids""\s+type=""hidden""\s+value=""(?<ids>[^""]*)"".*?/>)";
        protected string tv4DynamicSubCategoriesRegEx2 = @"<li\s+class=""video-panel\s+(?!(clip))[^""]*"">\s*<p[^>]*>\s*<a\s+href=""(?<url>[^""]*)"">\s*<img\s+alt=""[^""]*""\s+src=""(?<thumb>[^""]*)"">\s*</a>\s*</p>\s*<div[^>]*>\s*<h3[^>]*>\s*<a[^>]*>(?<title>[^<]+)</a>\s*</h3>\s*<p[^>]*>\s*(?<desc>[^<]+)</p>";

        Regex regEx_dynamicCategories, regEx_dynamicSubCategories, regEx_dynamicSubCategories2, regEx_tv4VideoList, regEx_tv4VideoListNextPage;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            if (!string.IsNullOrEmpty(tv4DynamicCategoriesRegEx)) regEx_dynamicCategories = new Regex(tv4DynamicCategoriesRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            if (!string.IsNullOrEmpty(tv4DynamicSubCategoriesRegEx)) regEx_dynamicSubCategories = new Regex(tv4DynamicSubCategoriesRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            if (!string.IsNullOrEmpty(tv4DynamicSubCategoriesRegEx2)) regEx_dynamicSubCategories2 = new Regex(tv4DynamicSubCategoriesRegEx2, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            if (!string.IsNullOrEmpty(tv4VideolistRegEx)) regEx_tv4VideoList = new Regex(tv4VideolistRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            if (!string.IsNullOrEmpty(tv4VideolistNextPageRegEx)) regEx_tv4VideoListNextPage = new Regex(tv4VideolistNextPageRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
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
                    if (parentCategory.ParentCategory == null)
                    {
                        parentCategory.SubCategories = new List<Category>();
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
                            cat.SubCategories = new List<Category>();
                            m = m.NextMatch();
                        }
                    }
                    else if (parentCategory.SubCategories != null)
                    {
                        string jsonUrl = "http://www.tv4play.se/program_format_searches.json?ids=";
                        bool found = false;
                        Match m = regEx_dynamicSubCategories.Match(data);
                        if (m.Success)
                        {
                            found = true;
                            jsonUrl += System.Web.HttpUtility.UrlDecode(m.Groups["ids"].Value) + "&rows=100";
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
                                    cat.HasSubCategories = true;
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
                                cat.HasSubCategories = true;
                                cat.ParentCategory = parentCategory;
                                m2 = m2.NextMatch();
                            }
                        }
                    }
                    else
                    {
                        Match m3 = Regex.Match(data, @"<h2><span>(?<title>[^<]+)</span></h2>");
                        parentCategory.SubCategories = new List<Category>();
                        while (m3.Success)
                        {
                            parentCategory.SubCategories.Add(new RssLink() { Name = m3.Groups["title"].Value, ParentCategory = parentCategory, Url = (parentCategory as RssLink).Url, Other = "TV4" });
                            m3 = m3.NextMatch();
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

        int currentStart = 0;
        string pagingUrl = "";
        public override List<VideoInfo> getNextPageVideos()
        {
            return ParseVideos(new RssLink() { Url = pagingUrl, Other = "TV4" });
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            HasNextPage = false;
            currentStart = 0;
            pagingUrl = "";
            return ParseVideos(category);
        }

        List<VideoInfo> ParseVideos(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            if (category.Other == "TV4")
            {
                string data = "";
                if (category.ParentCategory != null)
                {
                    data = GetWebData((category.ParentCategory as RssLink).Url);
                    data = Regex.Match(data, string.Format(tv4VideolistDividingRegEx, category.Name.Replace(" ", "\\s")), RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture).Groups["data"].Value;
                }
                else
                {
                    // called from getNextPageVideos()
                    data = GetWebData((category as RssLink).Url);
                }
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
                HasNextPage = false;
                pagingUrl = "";
                int currentMaxVideos = -1;
                int.TryParse(Regex.Match(data, @"<p\sclass=""info"">Visar\s\d+\sav\s(?<max>\d+)</p>", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture).Groups["max"].Value, out currentMaxVideos);
                if (currentMaxVideos > 0) (category as RssLink).EstimatedVideoCount = (uint)currentMaxVideos;
                m = regEx_tv4VideoListNextPage.Match(data);
                if (m.Success)
                {
                    pagingUrl = HttpUtility.HtmlDecode(m.Groups["url"].Value);
                    if (!Uri.IsWellFormedUriString(pagingUrl, System.UriKind.Absolute)) pagingUrl = new Uri(new Uri(tv4BaseUrl), pagingUrl).AbsoluteUri;



                    string incString = Regex.Match(pagingUrl, @"increment=(\d+)").Groups[1].Value;
                    int inc = 12;
                    if (incString != "") int.TryParse(incString, out inc);

                    string rowsString = Regex.Match(pagingUrl, @"rows=(\d+)").Groups[1].Value;
                    int rows = 12;
                    if (rowsString != "") int.TryParse(rowsString, out rows);

                    pagingUrl = Regex.Replace(pagingUrl, @"rows=(\d+)", "rows=" + inc.ToString());

                    currentStart += inc;

                    if (currentStart < currentMaxVideos)
                    {
                        pagingUrl += "&start=" + currentStart.ToString();

                        HasNextPage = true;
                    }
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
						if (xDoc.SelectSingleNode("GeoLock/Success").InnerText != "false")
						{
							playstr = xDoc.SelectSingleNode("GeoLock/Url").InnerText;
							if (playstr.ToLower().StartsWith("rtmp"))
							{
								playstr = new MPUrlSourceFilter.RtmpUrl(playstr) { SwfUrl = "http://flvplayer.viastream.viasat.tv/flvplayer/play/swf/player100920.swf", SwfVerify = true }.ToString();
							}
						}
						else
						{
							throw new OnlineVideosException(xDoc.SelectSingleNode("GeoLock/Msg").InnerText);
						}
                    }
                }

                return playstr;
            }
        }
    }
}
