using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Xml;
using System.IO;
using System.Web;
using System.Net;
using System.Text.RegularExpressions;
using System.Drawing;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class SesameStreetUtil : GenericSiteUtil
    {

        public override int DiscoverDynamicCategories()
        {
            int res = base.DiscoverDynamicCategories();
            foreach (RssLink cat in Settings.Categories)
            {
                cat.HasSubCategories = cat.Url.IndexOf('?') == -1;
                if (!String.IsNullOrEmpty(cat.Thumb))
                {
                    cat.Other = cat.Thumb;
                    cat.Thumb = null;
                }
            }
            return res;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string url = ((RssLink)parentCategory).Url;

            List<Category> categories = new List<Category>();
            string webData = GetWebData(url);
            webData = GetSubString(webData, "main-section", "footer");
            int splitInd = webData.IndexOf("orange");
            int nWithImage = 0;

            if (!string.IsNullOrEmpty(webData))
            {
                List<string> names = new List<string>();
                Match m = regEx_dynamicSubCategories.Match(webData);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    if (!names.Contains(cat.Name))
                    {
                        cat.Url = m.Groups["url"].Value;
                        cat.HasSubCategories = false;
                        categories.Add(cat);
                        cat.ParentCategory = parentCategory;
                        names.Add(cat.Name);
                        if (splitInd == -1 || m.Index <= splitInd)
                            nWithImage++;
                    }
                    m = m.NextMatch();
                }

                if (parentCategory.Other != null)
                {

                    string pngName = (string)parentCategory.Other;
                    try
                    {
                        Bitmap png = null;

                        string bareFinalUrl = System.IO.Path.ChangeExtension(pngName, String.Empty);
                        for (int i = 0; i < nWithImage; i++)
                        {
                            string finalUrl = bareFinalUrl + '_' + i.ToString() + ".PNG";
                            categories[i].Thumb = finalUrl;
                            string imageLocation = Utils.GetThumbFile(finalUrl);
                            if (!File.Exists(imageLocation))
                            {
                                if (png == null)
                                {
                                    WebRequest request = WebRequest.Create(pngName);
                                    WebResponse response = request.GetResponse();
                                    Stream responseStream = response.GetResponseStream();
                                    png = new Bitmap(responseStream);
                                }
                                int newHeight = png.Height / nWithImage;
                                Bitmap newPng = new Bitmap(png.Width, newHeight);
                                Graphics g = Graphics.FromImage(newPng);
                                g.DrawImage(png, 0, -i * newHeight);
                                g.Dispose();

                                newPng.Save(imageLocation);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        Log.Info("image not found : " + pngName);
                    }

                }
                parentCategory.SubCategoriesDiscovered = true;
            }

            parentCategory.SubCategories = categories;
            return parentCategory.SubCategories.Count;
        }

        public override String GetVideoUrl(VideoInfo video)
        {
            string vidUrl = base.GetVideoUrl(video);
            string videoId = vidUrl.Split('/')[3];

            string postData = String.Format(@"uid={0}&type=video&capabilities=%7B%22isIE%22%3Atrue%2C%22isFirefox%22%3Afalse%2C%22isChrome%22%3Afalse%2C%22isWebKit%22%3Afalse%2C%22isMobile%22%3Afalse%2C%22isTablet%22%3Afalse%2C%22isHandset%22%3Afalse%2C%22isIOS%22%3Afalse%2C%22isIOSHandset%22%3Afalse%2C%22isIOSTablet%22%3Afalse%2C%22isAndroid%22%3Afalse%2C%22isAndroidTablet%22%3Afalse%2C%22isAndroidHandset%22%3Afalse%2C%22isKindle%22%3Afalse%2C%22hasCanvasSupport%22%3Afalse%2C%22hasTouchSupport%22%3Afalse%2C%22hasFlashSupport%22%3Atrue%2C%22hasVideoSupport%22%3Afalse%7D&context=%7B%22userId%22%3A%2210097%22%2C%22groupId%22%3A%2210171%22%2C%22privateLayout%22%3Afalse%2C%22layoutId%22%3A%221368%22%7D&serviceClassName=org.sesameworkshop.service.UmpServiceUtil&serviceMethodName=getMediaItem&serviceParameters=%5B%22uid%22%2C%22type%22%2C%22capabilities%22%2C%22context%22%5D&doAsUserId=",
                videoId);

            string data = GetWebData(@"http://www.sesamestreet.org/c/portal/json_service", postData, referer: vidUrl);

            JObject contentData = (JObject)JObject.Parse(data);
            string url = null;
            JArray items = contentData["content"]["source"] as JArray;
            if (items != null)
            {
                video.PlaybackOptions = new Dictionary<string, string>();
                foreach (JToken item in items)
                {
                    url = item.Value<string>("fileName");
                    int bitRate = item.Value<int>("bitRate");
                    bitRate = Convert.ToInt32(bitRate / 1024);
                    video.PlaybackOptions.Add(bitRate.ToString() + 'K', url);
                }
            }
            return url;
        }

        private string GetSubString(string s, string start, string until)
        {
            int p = s.IndexOf(start);
            if (p == -1) return String.Empty;
            p += start.Length;
            int q = s.IndexOf(until, p);
            if (q == -1) return s.Substring(p);
            return s.Substring(p, q - p);
        }
    }
}
