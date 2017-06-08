using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Net;
using System.Linq;
using System.Xml;
using System.Web;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class EuroSportUtil : SiteUtilBase
    {

        [Category("OnlineVideosUserConfiguration"), Description("The tld for the eurosportplayer url, e.g. nl, uk or de")]
        private string tld = null;

        [Category("OnlineVideosUserConfiguration"), Description("The country for the eurosportplayer url, usually the same as the tld, but for some reason the uk needs to fill in 'gb' here")]
        private string country = null;

        [Category("OnlineVideosUserConfiguration"), Description("Email address of your eurosport account")]
        private string emailAddress = null;
        [Category("OnlineVideosUserConfiguration"), Description("Password of your eurosport account")]
        private string password = null;
        [Category("OnlineVideosUserConfiguration"), Description("Language id (5 for dutch, others: ask doskabouter)")]
        private string languageId = null;

        private string baseUrl;
        private Regex SubcatRegex;

        private enum kind { Live, Video };
        private CookieContainer newcc = new CookieContainer();
        private string context = null;
        private string hkey = null;
        private string userId = null;

        public override int DiscoverDynamicCategories()
        {
            if (tld == null || emailAddress == null || password == null || languageId == null || country == null)
                return 0;
            Log.Debug("tld, emailaddress, password, languageid and country != null");

            SubcatRegex = new Regex(@"<li\sclass=""vod-menu-element-sports-element""\sdata-sporturl=""(?<url>[^""]*)""\sdata-filter=""sports"">(?<title>[^<]*)</li>");
            baseUrl = String.Format(@"https://{0}.eurosportplayer.com/", tld);

            CookieContainer cc = new CookieContainer();

            string url = baseUrl + "_wsplayerxrm_/PlayerCrmApi_v6.svc/Login";
            context = @"{""g"":""" + country.ToUpperInvariant() + @""",""d"":""1"",""s"":""1"",""p"":""1"",""b"":""apple""," +
                @"""bp"":"""",""st"":""Eurosport"",""li"":""" + languageId + @""",""pc"":""ply"",""drp"":""171""}";
            string postData = @"{""data"":""{\""l\"":\""" + emailAddress + @"\"",\""p\"":\""" + password + @"\"",\""r\"":false}""," +
                @"""context"":""" + context.Replace(@"""", @"\""") + @"""}";

            string res = GetWebDataFromPost(url, postData, cc);
            if (!res.Contains(@"""Success"":1"))
            {
                Log.Error("Eurosport: login unsuccessfull");
                Log.Debug("login unsuccessfull");// so it's in mediaportal.log as wel ass onlinevideos.log
                Log.Debug("result: " + res);
            }

            CookieCollection ccol = cc.GetCookies(new Uri(baseUrl));
            foreach (Cookie c in ccol)
            {
                Log.Debug("Add cookie " + c.ToString());
                switch (c.Name)
                {
                    case "PlayerAccess":
                        {
                            var m = Regex.Match(c.Value, @"%22hashkey%22%3a%22(?<val>[^%]*)%22");
                            if (m.Success)
                                hkey = m.Groups["val"].Value;
                            break;
                        }
                    case "PlayerDatas":
                        {
                            var m = Regex.Match(c.Value, @"%22userid%22%3a%22(?<val>[^%]*)%22");
                            if (m.Success)
                                userId = m.Groups["val"].Value;
                            break;
                        }
                }
                newcc.Add(c);
            }


            RssLink category = new RssLink();
            category.Url = baseUrl + "live.shtml";
            category.Name = "Live TV";
            category.Other = kind.Live;
            Settings.Categories.Add(category);

            category = new RssLink();
            category.Url = baseUrl + "videos-home.xml";
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
            string urlPart = @"{""userid"":""" + userId + @""",""hkey"":""" + hkey + @""",""languageid"":""" + languageId + @"""}";
            var context2 = @"{""p"": ""1"", ""s"": ""1"", ""b"": ""apple"", ""d"": ""2"", ""g"": """ + country.ToUpperInvariant() + @"""}";

            var jData = GetWebData<JObject>(@"http://videoshop.ws.eurosport.com/JsonProductService.svc/GetAllCatchupCache?data=" + HttpUtility.UrlEncode(urlPart) +
                @"&context=" + HttpUtility.UrlEncode(context2), cookies: newcc);
            Log.Debug("discsubcats " + jData.ToString());

            parentCategory.SubCategories = new List<Category>();
            Dictionary<int, Category> cats = new Dictionary<int, Category>();
            foreach (var sport in jData["PlayerObj"]["sports"])
            {
                RssLink cat = new RssLink();
                cat.Thumb = sport.Value<string>("pictureurl");
                cat.Name = sport.Value<string>("name");
                cat.ParentCategory = parentCategory;
                cat.Other = new List<VideoInfo>();
                cats.Add(sport.Value<int>("id"), cat);
                parentCategory.SubCategories.Add(cat);
            }

            foreach (var catchup in jData["PlayerObj"]["catchups"])
            {
                VideoInfo video = new VideoInfo();
                video.Title = catchup.Value<string>("titlecatchup");
                video.Thumb = catchup.Value<string>("pictureurl");

                video.Length = Helpers.TimeUtils.TimeFromSeconds(catchup.Value<string>("durationInSeconds"));
                if (catchup["startdate"] != null)
                    video.Airdate = catchup["startdate"].Value<string>("date");
                video.Description = catchup.Value<string>("description");
                video.VideoUrl = catchup["catchupstreams"][0].Value<string>("url");

                video.Other = kind.Video;
                ((List<VideoInfo>)cats[catchup["sport"].Value<int>("id")].Other).Add(video);

            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories == null ? 0 : parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            if (category.Other is List<VideoInfo>)
                return (List<VideoInfo>)category.Other;
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
            foreach (XmlNode node in doc.DocumentElement.SelectNodes("/xml/div/div/span[@class='video-slide__wrapper']"))
            {
                VideoInfo video = new VideoInfo();
                var jData = JObject.Parse(node.Attributes["data-video"].Value);
                video.Title = jData.Value<string>("title");
                video.Thumb = node.SelectSingleNode("img").Attributes["src"].Value;
                video.Length = Helpers.TimeUtils.TimeFromSeconds(jData.Value<string>("duration"));
                video.Airdate = jData.Value<string>("startdate");
                video.VideoUrl = baseUrl + @"/videos-player.xml?mode=Catchup&catchupvideoId=" + jData.Value<string>("id") + @"&part=0&streamlanguageid=" + languageId + @"&type=RelatedContent";
                video.Description = jData.Value<string>("description");
                video.Other = jData.Value<string>("id");
                result.Add(video);
            }
            return result;
        }

        private string getValue(string webData, string id)
        {
            Match m = Regex.Match(webData, @"" + id + @":'(?<value>[^']*)'");
            if (m.Success)
                return m.Groups["value"].Value;
            return null;
        }

        private List<VideoInfo> GetVideoListFromLive(Category category)
        {
            string webData = GetWebData(((RssLink)category).Url, cookies: newcc);
            string data = @"{""languageid"":""" + getValue(webData, "languageid") + @""",""withouttvscheduleliveevents"":true,""guest"":false}";
            string url2 = baseUrl + @"_wsvideoshop_/JsonProductService.svc/GetAllChannelsCache?data=" + HttpUtility.UrlEncode(data) + "&context=" + HttpUtility.UrlEncode(context);

            webData = GetWebData(url2, cookies: newcc);

            JToken alldata = JObject.Parse(webData) as JToken;
            JArray jChannels = alldata["PlayerObj"] as JArray;
            List<VideoInfo> videos = new List<VideoInfo>();

            foreach (JToken jChannel in jChannels)
            {
                VideoInfo video = new VideoInfo();
                video.Title = jChannel["title"].Value<string>();

                JArray jLiveStreams = jChannel["streams"] as JArray;
                video.Other = jChannel;
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
            string urlPart = @"{""userid"":""" + userId + @""",""hkey"":""" + hkey + @"""}";


            var tokenData = GetWebData<JObject>(@"http://videoshop.ws.eurosport.com/JsonProductService.svc/GetToken?data=" + HttpUtility.UrlEncode(urlPart) +
                @"&context=" + HttpUtility.UrlEncode(context), cookies: newcc);
            Log.Debug("tokendata " + tokenData.ToString());

            string token = tokenData["PlayerObj"].Value<string>("token");

            string getData = GetWebData(video.VideoUrl + '&' + token, cookies: newcc);
            Log.Debug("GetUrlFromVideo " + getData);

            video.PlaybackOptions = Helpers.HlsPlaylistParser.GetPlaybackOptions(getData, video.VideoUrl);
            if (video.PlaybackOptions == null || video.PlaybackOptions.Count == 0) return null;
            else
            if (video.PlaybackOptions.Count == 1)
            {
                string resultUrl = video.PlaybackOptions.First().Value;
                video.PlaybackOptions = null;// only one url found, PlaybackOptions not needed
                return resultUrl;
            }
            else
            {
                return video.PlaybackOptions.First().Value;
            }
        }

        private string GetUrlFromLive(VideoInfo video)
        {
            JToken channel = (JToken)video.Other;
            JArray streams = channel["streams"] as JArray;

            video.PlaybackOptions = new Dictionary<string, string>();
            foreach (JToken stream in streams)
            {
                string postData = @"{""data"":""{\""userid\"":\""" + userId + @"\"",\""hkey\"":\""" + hkey + @"\"",\""urls\"":\""[{\\\""id\\\"":" + channel["id"].Value<string>() +
                    @",\\\""format\\\"":" + stream["format"].Value<string>() + @",\\\""url\\\"":\\\""" +
                    stream["url"].Value<string>() + @"\\\"",\\\""rescueurl\\\"":\\\""" +
                    stream["rescueurl"].Value<string>() + @"\\\""}]\""}""," + @"""context"":""" + context.Replace(@"""", @"\""") + @"""}";
                string data = GetWebDataFromPost(baseUrl + "/_wsvideoshop_/JsonProductService.svc/SecurizeUrls", postData, newcc);
                var jData = JObject.Parse(data);
                string name = (video.PlaybackOptions.Count + 1).ToString();
                video.PlaybackOptions.Add(name, jData["PlayerObj"][0].Value<string>("url"));
            }

            return video.GetPreferredUrl(true);
        }

        private static string GetWebDataFromPost(string url, string postData, CookieContainer cc)
        {
            var headers = new NameValueCollection();
            headers["Content-type"] = "application/json";
            headers.Add("Accept", "*/*");
            headers.Add("User-Agent", OnlineVideoSettings.Instance.UserAgent);
            Log.Debug("get webdata from {0}", url);
            Log.Debug("postdata = " + postData);
            return WebCache.Instance.GetWebData(url, postData, cookies: cc, headers: headers);
        }

        public string GetToken()
        {
            string urlPart = @"{""userid"":""" + userId + @""",""hkey"":""" + hkey + @"""}";

            var tokenData = GetWebData<JObject>(@"http://videoshop.ws.eurosport.com/JsonProductService.svc/GetToken?data=" + HttpUtility.UrlEncode(urlPart) +
                @"&context=" + HttpUtility.UrlEncode(context), cookies: newcc);

            return tokenData["PlayerObj"].Value<string>("token");
        }


    }

}
