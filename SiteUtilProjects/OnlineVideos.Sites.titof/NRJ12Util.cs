using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Web;
using System.Net;
using System.Xml;
using System.Text.RegularExpressions;
using System.Collections;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class NRJ12Util : GenericSiteUtil
    {
        //http://www.nrj-play.fr/nrj12/api/getreplaytvlist
        //http://www.nrj-play.fr/nrj12/api/getreplaytvcollection
        //http://www.nrj-play.fr/nrj12/api/getonairtvlist
        //http://www.nrj-play.fr/nrj12/api/getscheduletv

        //http://www.nrj-play.fr/cherie25/api/getreplaytvlist
        //http://www.nrj-play.fr/cherie25/api/getreplaytvcollection
        //http://www.nrj-play.fr/cherie25/api/getonairtvlist


        //http://www.nrj12.fr/programmetv/getreplaytvlist/?c=5&p=androidtab

        //http://www.nrj-play.fr/nrj12/api/getreplaytvlist?p=androidtab

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            RssLink cat = null;

            cat = new RssLink()
            {
                Url = "%DIRECT%",
                Name = "En Direct",
                Other = "root",
                Thumb = "",
                HasSubCategories = false,
            };
            Settings.Categories.Add(cat);

            var result = Helper.TvLogoDB.LogoChannel.GetChannel("nrj 12");
            cat = new RssLink()
            {
                Url = "http://www.nrj-play.fr/nrj12/api/getreplaytvlist",
                Name = "NRJ 12",
                Other = "root",
                Thumb = result.FirstOrDefault().LogoWide,
                HasSubCategories = false,
            };
            Settings.Categories.Add(cat);

            result = Helper.TvLogoDB.LogoChannel.GetChannel("chérie 25");
            cat = new RssLink()
            {
                Url = "http://www.nrj-play.fr/cherie25/api/getreplaytvlist",
                Name = "Chérie 25",
                Other = "root",
                Thumb = result.FirstOrDefault().LogoWide,
                HasSubCategories = false,
            };
            Settings.Categories.Add(cat);

            Settings.DynamicCategoriesDiscovered = true;

            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            RssLink parent = parentCategory as RssLink;
            parent.SubCategories = new List<Category>();
            if (parentCategory.Other.ToString() == "root" && parent.Url == "%DIRECT%")
            {
                

            }
            else if (parentCategory.Other.ToString() == "root")
            {
                //Category cat = new Category();
                //cat.Other = "http://www.nrj-play.fr/nrj12/api/getreplaytvlist";
                //cat.Name = "Replay";
                //cat.HasSubCategories = false;

                //parentCategory.SubCategories.Add(cat);

            }

            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            string sUrl = ((RssLink)category).Url;
            List<VideoInfo> tlist = new List<VideoInfo>();
            if (sUrl.Contains("%DIRECT%"))
            {
                var result = Helper.TvLogoDB.LogoChannel.GetChannel("nrj 12");
                VideoInfo nfp = new VideoInfo()
                {
                    Title = "NRJ 12",
                    Description = "Visionner le direct",
                    VideoUrl = "http://nrj-apple-live.adaptive.level3.net/apple/nrj/nrj/nrj12hi.m3u8",
                    Thumb = result.FirstOrDefault().LogoWide,
                };
                tlist.Add(nfp);

                result = Helper.TvLogoDB.LogoChannel.GetChannel("chérie 25");
                nfp = new VideoInfo()
                {
                    Title = "Chérie 25",
                    Description = "Visionner le direct",
                    VideoUrl = "http://nrj-apple-live.adaptive.level3.net/apple/nrj/nrjcheriehd/cherie25hi.m3u8",
                    Thumb = result.FirstOrDefault().LogoWide,
                };
                tlist.Add(nfp);

                return tlist;
            }

            XmlDocument xml = GetWebData<XmlDocument>(sUrl);
            if (xml != null)
            {
                string url = string.Empty;
                XmlNodeList xList = xml.SelectNodes(@"//programs/program");

                foreach (XmlNode sd in xList)
                {
                    if (sd != null)
                    {
                        VideoInfo nfp = new VideoInfo();

                        string title = sd.SelectSingleNode("title").InnerText;
                        string subtitle = sd.SelectSingleNode("subtitle").InnerText;
                        string duration = sd.SelectSingleNode("duration").InnerText;
                        string videourl = sd.SelectSingleNode("offres/offre/videos/video").InnerText;
                        string date = sd.SelectSingleNode("offres/offre/broadcastdate").InnerText;
                        string photo = sd.SelectSingleNode("photos/photo").InnerText;
                        string desc = sd.SelectSingleNode("stories/story").InnerText;

                        nfp.Title = title + " - " + subtitle;
                        nfp.Length = duration;
                        nfp.VideoUrl = videourl;
                        nfp.Thumb = photo;
                        nfp.Airdate = date;
                        nfp.Description = desc;

                        tlist.Add(nfp);
                    }
                } 
            }

            return tlist;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            return video.VideoUrl;
        }

        protected string GetSubString(string s, string start, string until)
        {
            int p = s.IndexOf(start);
            if (p == -1) return String.Empty;
            p += start.Length;
            if (until == null) return s.Substring(p);
            int q = s.IndexOf(until, p);
            if (q == -1) return s.Substring(p);
            return s.Substring(p, q - p);
        }

    }
}
