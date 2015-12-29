﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OnlineVideos._3rdParty.Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class PluzzUtil : GenericSiteUtil
    {
        #region Fields

        private string channelCatalog = "http://pluzz.webservices.francetelevisions.fr/pluzz/liste/type/replay/nb/100/chaine/%chaine%/";
        private string imgURL = "http://refonte.webservices.francetelevisions.fr/";
        private string showInfo = "http://webservices.francetelevisions.fr/tools/getInfosOeuvre/v2/?idDiffusion=%s&catalogue=Pluzz";

        #endregion Fields

        #region Methods

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            RssLink cat = null;
            cat = new RssLink();
            cat.Url = channelCatalog.Replace("%chaine%", "la_1ere_reunion%2Cla_1ere_guyane%2Cla_1ere_polynesie%2Cla_1ere_martinique%2Cla_1ere_mayotte%2Cla_1ere_nouvellecaledonie%2Cla_1ere_guadeloupe%2Cla_1ere_wallisetfutuna%2Cla_1ere_saintpierreetmiquelon"); ;
            cat.Name = "La 1ère";
            cat.Other = "root";
            cat.Thumb = "http://www.francetelevisions.fr/sites/default/files/images/2015/07/08/F1.png";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = channelCatalog.Replace("%chaine%", "france2"); ;
            cat.Name = "France 2";
            cat.Thumb = "France 2.png";
            cat.Other = "root";
            cat.Thumb = "http://www.francetelevisions.fr/sites/default/files/images/2015/07/08/F2.png";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = channelCatalog.Replace("%chaine%", "france3");
            cat.Name = "France 3";
            cat.Other = "root";
            cat.Thumb = "http://www.francetelevisions.fr/sites/default/files/images/2015/07/08/F3.png";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = channelCatalog.Replace("%chaine%", "france4");
            cat.Name = "France 4";
            cat.Other = "root";
            cat.HasSubCategories = true;
            cat.Thumb = "http://www.francetelevisions.fr/sites/default/files/images/2015/07/08/F4.png";
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = channelCatalog.Replace("%chaine%", "france5");
            cat.Name = "France 5";
            cat.Other = "root";
            cat.Thumb = "http://www.francetelevisions.fr/sites/default/files/images/2015/07/08/F5.png";

            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = channelCatalog.Replace("%chaine%", "franceo");
            cat.Name = "France Ô";
            cat.Other = "root";
            cat.Thumb = "http://www.francetelevisions.fr/sites/default/files/images/2015/07/08/FO.png";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://pluzz.webservices.francetelevisions.fr/pluzz/liste/type/replay/nb/100/rubrique/jeunesse/";
            cat.Name = "Jeunesse";
            cat.Other = cat.Url;
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://pluzz.webservices.francetelevisions.fr/pluzz/liste/type/replay/nb/100/rubrique/sport/";
            cat.Name = "Sport";
            cat.Other = cat.Url;
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://pluzz.webservices.francetelevisions.fr/pluzz/liste/type/replay/nb/100/rubrique/documentaire/";
            cat.Name = "Documentaire";
            cat.Other = cat.Url;
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://pluzz.webservices.francetelevisions.fr/pluzz/liste/type/replay/nb/100/rubrique/serie--fiction/";
            cat.Name = "Série & Fiction";
            cat.Other = cat.Url;
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://pluzz.webservices.francetelevisions.fr/pluzz/liste/type/replay/nb/100/rubrique/info/";
            cat.Name = "Info";
            cat.Other = cat.Url;
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://pluzz.webservices.francetelevisions.fr/pluzz/liste/type/replay/nb/100/rubrique/culture/";
            cat.Name = "Culture";
            cat.Other = cat.Url;
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://pluzz.webservices.francetelevisions.fr/pluzz/liste/type/replay/nb/100/rubrique/magazine/";
            cat.Name = "Magazine";
            cat.Other = cat.Url;
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            Settings.DynamicCategoriesDiscovered = true;

            //http://pluzz.webservices.francetelevisions.fr/pluzz/liste/type/replay/nb/100/rubrique/jeunesse/
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            RssLink parent = parentCategory as RssLink;
            if (parentCategory.Other.ToString() == "root")
            {
                parent.SubCategories = new List<Category>();

                Category cat = new Category();
                cat.Other = parent.Url + "rubrique/info/";
                cat.Name = "Info";
                cat.HasSubCategories = false;

                parentCategory.SubCategories.Add(cat);

                cat = new Category();
                cat.Other = parent.Url + "rubrique/documentaire/";
                cat.Name = "Documentaire";
                cat.HasSubCategories = false;
                parentCategory.SubCategories.Add(cat);

                cat = new RssLink();
                cat.Other = parent.Url + "rubrique/seriefiction/";
                cat.Name = "Série & Fiction";
                cat.HasSubCategories = false;
                parentCategory.SubCategories.Add(cat);

                cat = new Category();
                cat.Other = parent.Url + "rubrique/magazine/";
                cat.Name = "Magazine";
                cat.HasSubCategories = false;
                Settings.Categories.Add(cat);
                parentCategory.SubCategories.Add(cat);

                cat = new Category();
                cat.Other = parent.Url + "rubrique/culture/";
                cat.Name = "Culture";
                cat.HasSubCategories = false;
                Settings.Categories.Add(cat);
                parentCategory.SubCategories.Add(cat);

                cat = new Category();
                cat.Other = parent.Url + "rubrique/jeunesse/";
                cat.Name = "Jeunesse";
                cat.HasSubCategories = false;
                parentCategory.SubCategories.Add(cat);

                cat = new RssLink();
                cat.Other = parent.Url + "rubrique/divertissement/";
                cat.Name = "Divertissement";
                cat.HasSubCategories = false;
                parentCategory.SubCategories.Add(cat);

                cat = new Category();
                cat.Other = parent.Url + "rubrique/sport/";
                cat.Name = "Sport";
                cat.HasSubCategories = false;
                parentCategory.SubCategories.Add(cat);

                cat = new Category();
                cat.Other = parent.Url + "rubrique/jeu/";
                cat.Name = "Jeu";
                cat.HasSubCategories = false;
                parentCategory.SubCategories.Add(cat);
            }

            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            string webData = GetWebData(category.Other.ToString());
            if (string.IsNullOrEmpty(webData)) return new List<VideoInfo>();
            List<VideoInfo> tlist = GetVideo(webData);
            return tlist;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string moreinfo = GetWebData(showInfo.Replace("%s", video.Other.ToString()));
            JObject obj1 = JObject.Parse(moreinfo);
            JArray tarr = (JArray)obj1["videos"];

            string surl = (string)tarr[2]["url"];

            M3U.M3U.M3UPlaylist play = new M3U.M3U.M3UPlaylist();
            play.Configuration.Depth = 1;
            play.Read(surl);
            IEnumerable<OnlineVideos.Sites.M3U.M3U.M3UComponent> telem = from item in play.OrderBy("BRANDWITH")
                                                                         select item;

            if (telem.Count() > 3)
            {
                video.PlaybackOptions = new Dictionary<string, string>();
                video.PlaybackOptions.Add("LOW", telem.ToList()[telem.Count() - 3].Path);
                video.PlaybackOptions.Add("SD", telem.ToList()[telem.Count() - 2].Path);
                video.PlaybackOptions.Add("HD", telem.ToList()[telem.Count() - 1].Path);
            }
            return telem.Last().Path;
        }

        private List<VideoInfo> GetVideo(string webData)
        {
            List<VideoInfo> tReturn = new List<VideoInfo>();

            JObject obj = JObject.Parse(webData);

            JArray wbdata = (JArray)obj["reponse"]["emissions"];
            foreach (JObject item in wbdata)
            {
                try
                {
                    string siddiff = (string)item["id_diffusion"];

                    string thumb = string.Empty;
                    try
                    {
                        thumb = (string)item["image_300"];
                        thumb = imgURL + thumb;
                    }
                    catch { }

                    VideoInfo nfo = new VideoInfo()
                    {
                        Title = (string)item["titre_programme"],
                        Thumb = (thumb ?? String.Empty),
                        Other = siddiff,
                        StartTime = (string)item["date_diffusion"],
                        Description = (string)item["accroche_programme"]
                    };
                    tReturn.Add(nfo);
                }
                catch { }
            }
            return tReturn;
        }

        #endregion Methods
    }
}