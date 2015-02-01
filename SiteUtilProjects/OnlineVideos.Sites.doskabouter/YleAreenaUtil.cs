using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Text.RegularExpressions;
using System.IO;
using OnlineVideos.MPUrlSourceFilter;
using System.ComponentModel;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class YleAreenaUtil : GenericSiteUtil
    {
        private string bareUrl;
        int newStart;

        public override int DiscoverDynamicCategories()
        {
            int res = base.DiscoverDynamicCategories();
            foreach (Category cat in Settings.Categories)
                cat.HasSubCategories = true;
            return res;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string url = ((RssLink)parentCategory).Url;
            bool isAZ = !url.Contains("/tv");
            if (isAZ)
                return SubcatFromAZ((RssLink)parentCategory);
            parentCategory.SubCategories = new List<Category>();
            string catUrl = ((RssLink)parentCategory).Url + "/kaikki.json?from=0&to=24";
            string webData = GetWebData(catUrl, forceUTF8: true);
            JToken j = JToken.Parse(webData);
            JArray orders = j["filters"]["jarjestys"] as JArray;
            parentCategory.SubCategories = new List<Category>();
            foreach (JToken order in orders)
            {
                string orderBy = order.Value<string>("key");
                RssLink subcat = new RssLink()
                {
                    Name = orderBy,
                    Url = ((RssLink)parentCategory).Url + "/kaikki.json?jarjestys=" + orderBy + '&',
                    ParentCategory = parentCategory,
                };
                parentCategory.SubCategories.Add(subcat);
            }
            parentCategory.SubCategoriesDiscovered = true;

            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            return getVideos(((RssLink)category).Url, 0);
        }

        private List<VideoInfo> getVideos(string url, int startNr)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            bareUrl = url;
            newStart = startNr + 24;
            string webData = GetWebData(String.Format(url + "from={0}&to={1}", startNr, newStart), forceUTF8: true);
            JToken j = JToken.Parse(webData);
            /*if (startNr == 0 && false)// only for not a-z
            {
                JArray orders = j["filters"]["jarjestys"] as JArray;
                orderByList = new Dictionary<string, string>();
                foreach (JToken order in orders)
                    orderByList.Add(order.Value<string>("key"), order.Value<string>("key"));
            }*/
            JArray videos = j["search"]["results"] as JArray;
            foreach (JToken jvid in videos)
            {
                JToken images = jvid["images"];
                VideoInfo video = new VideoInfo()
                {
                    Title = jvid.Value<string>("title"),
                    Description = jvid.Value<string>("desc"),
                    Length = jvid.Value<string>("durationSec"),
                    Airdate = jvid.Value<string>("published"),
                    ImageUrl = images.Value<string>("XL"),
                    VideoUrl = String.Format(@"http://areena.yle.fi/tv/{0}.json", jvid.Value<string>("id"))
                };
                result.Add(video);
            }
            nextPageAvailable = result.Count >= 24;
            return result;
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            return getVideos(bareUrl, newStart);
        }


        private int SubcatFromAZ(RssLink parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            string webData = GetWebData(parentCategory.Url, forceUTF8: true);
            string[] parts = webData.Split(new[] { @"<li class=""h2"">" }, StringSplitOptions.RemoveEmptyEntries);
            Regex r = new Regex(@"<li>\s*<a\sclass=""aotip""\stitle=""""\s*href=""(?<url>[^""]*)""\s>\s*<span\sclass=""world[^""]*"">(?<title>[^<]*)</span>\s*</a>\s*<div\sclass=""mini-epg-tooltip\sao""\sstyle=""display:none;"">\s*<h1\sclass=""h3"">[^<]*</h1>\s*<div\sclass=""desc"">(?:(?!<img).)*<img\ssrc=""(?<thumb>[^""]*)""\s(?:(?!class=""short"").)*class=""short"">\s*(?<description>[^<]*)<", defaultRegexOptions);
            foreach (string part in parts)
            {
                int p = part.IndexOf('<');
                if (p > 0)
                {
                    RssLink chr = new RssLink()
                    {
                        Name = part.Substring(0, p).Trim(),
                        SubCategories = new List<Category>(),
                        SubCategoriesDiscovered = true,
                        HasSubCategories = true,
                        ParentCategory = parentCategory
                    };
                    Match m = r.Match(part);
                    while (m.Success)
                    {
                        RssLink subcat = new RssLink()
                        {
                            Name = m.Groups["title"].Value,
                            Url = m.Groups["url"].Value,
                            Thumb = m.Groups["thumb"].Value,
                            Description = m.Groups["description"].Value,
                            ParentCategory = chr,
                            HasSubCategories = true,
                            SubCategoriesDiscovered = true,
                            SubCategories = new List<Category>()
                        };
                        chr.SubCategories.Add(subcat);
                        subcat.SubCategories.Add(new RssLink()
                        {
                            Name = "Ohjelmat",
                            Url = subcat.Url + ".json?sisalto=ohjelmat&",
                            ParentCategory = subcat,
                            Other = true
                        });
                        subcat.SubCategories.Add(new RssLink()
                        {
                            Name = "Muut videot",
                            Url = subcat.Url + ".json?sisalto=muut&",
                            ParentCategory = subcat,
                            Other = true
                        });
                        m = m.NextMatch();
                    }
                    if (chr.SubCategories.Count > 0)
                        parentCategory.SubCategories.Add(chr);
                }

            }
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            JToken videoInfo = GetWebData<JToken>(video.VideoUrl);
            JArray subTitles = videoInfo["media"]["subtitles"] as JArray;
            if (subTitles != null && subTitles.Count > 0)
            {
                try
                {
                    video.SubtitleText = GetWebData(subTitles[0].Value<string>("url"), forceUTF8: true);
                }
                catch
                {
                }
            }

            string papiurl = base.GetVideoUrl(video);

            string data = GetWebData(papiurl);
            byte[] bytes = Convert.FromBase64String(data);
            RijndaelManaged rijndael = new RijndaelManaged();
            byte[] iv = new byte[16];
            Array.Copy(bytes, iv, 16);
            rijndael.IV = iv;
            rijndael.Key = Encoding.ASCII.GetBytes("hjsadf89hk123ghk");
            rijndael.Mode = CipherMode.CFB;
            rijndael.Padding = PaddingMode.Zeros;
            ICryptoTransform decryptor = rijndael.CreateDecryptor(rijndael.Key, rijndael.IV);
            int padLen = 16 - bytes.Length % 16;
            byte[] newbytes = new byte[bytes.Length - 16 + padLen];
            Array.Copy(bytes, 16, newbytes, 0, bytes.Length - 16);
            Array.Clear(newbytes, newbytes.Length - padLen, padLen);
            string result = null;
            using (MemoryStream msDecrypt = new MemoryStream(newbytes))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        result = srDecrypt.ReadToEnd();
                        int p = result.IndexOf("</media>");
                        result = result.Substring(0, p + 8);
                    }
                }
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);
            XmlNode urlNode = doc.SelectSingleNode("//media/onlineAsset/url");
            string rtmpUrl = urlNode.SelectSingleNode("connect").InnerText;
            string stream = urlNode.SelectSingleNode("stream").InnerText;
            RtmpUrl theUrl = new RtmpUrl(rtmpUrl.Split('?')[0])
            {
                PlayPath = stream,
                SwfUrl = @"http://areena.yle.fi/static/player/1.2.8/flowplayer/flowplayer.commercial-3.2.7-encrypted.swf",
                App = "ondemand?" + rtmpUrl.Split('?')[1],
                PageUrl = video.VideoUrl.Substring(0, video.VideoUrl.Length - 5),
                TcUrl = rtmpUrl
            };
            return theUrl.ToString();
        }

    }
}