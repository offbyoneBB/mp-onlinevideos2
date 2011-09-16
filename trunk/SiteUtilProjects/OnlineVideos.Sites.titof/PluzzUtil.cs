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

namespace OnlineVideos.Sites
{
    public class PluzzUtil : GenericSiteUtil
    {
        
        public override int DiscoverDynamicCategories()
        {           
            RssLink cat = new RssLink();
            cat.Url = baseUrl + "/jt/";
            cat.Name = "JT";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = baseUrl + "/decouverte/";
            cat.Name = "Découverte";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = baseUrl + "/series---fictions/";
            cat.Name = "Séries - Fictions";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = baseUrl + "/vie-pratique/";
            cat.Name = "Vie pratique";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = baseUrl + "/culture/";
            cat.Name = "Culture";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = baseUrl + "/actu---societe/";
            cat.Name = "Actu - Société";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = baseUrl + "/ludo/";
            cat.Name = "Ludo";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = baseUrl + "/divertissement/";
            cat.Name = "Divertissement";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = baseUrl + "/sports/";
            cat.Name = "Sports";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "/france2/";
            cat.Name = "France 2";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "/france3/";
            cat.Name = "France 3";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "/france4/";
            cat.Name = "France 4";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "/france5/";
            cat.Name = "France 5";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "/franceo/";
            cat.Name = "France Ô";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);
        
            Settings.DynamicCategoriesDiscovered = true;

            return 14;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            XmlDocument doc = new XmlDocument();
            string webData = GetWebData("http://www.pluzz.fr/appftv/webservices/video/catchup/getListeAutocompletion.php?support=1");
            doc.LoadXml(webData);
            Log.Info("load xml");
            XmlNodeList list = doc.SelectNodes("/listeAutocompletion/item[@class='programme' and @chaine_principale='" + (parentCategory as RssLink).Url.Replace("/", "") + "']");
            Log.Info("load list");
            foreach (XmlNode n in list)
            {
                RssLink cat = new RssLink();
                cat.Url = baseUrl + "/recherche.html?q=" + n.InnerText;
                cat.Name = n.InnerText;
                cat.ParentCategory = parentCategory;
                parentCategory.SubCategories.Add(cat);
            }


            return parentCategory.SubCategories.Count;
        }


        public override List<string> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {            
            List<string> listUrls = new List<string>();

            string webData = GetWebData(video.VideoUrl);
            string id = Regex.Match(webData, @"<a\shref=""http://info\.francetelevisions\.fr/\?id-video=(?<url>[^""]*)""\sid=""current_video""\sclass=""video""\stype=""video/cappuccino"">").Groups["url"].Value;

            webData = GetWebData(baseUrl + "/appftv/webservices/video/getInfosVideo.php?src=cappuccino&video-type=simple&template=ftvi&template-format=complet&id-externe=" + id);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(webData);
            string ficName = doc.SelectSingleNode("/element/video/fichiers/fichier/nom").InnerText;
            string ficPath = doc.SelectSingleNode("/element/video/fichiers/fichier/chemin").InnerText;
            string prefix = "";
            if (ficName.EndsWith(".mp4"))
            {
                prefix = "rtmp://videozones-rtmp.francetv.fr/ondemand/mp4:cappuccino/publication";
            }
            else
            {
                prefix = "mms://a988.v101995.c10199.e.vm.akamaistream.net/7/988/10199/3f97c7e6/ftvigrp.download.akamai.com/10199/cappuccino/production/publication";
            }
            string url = prefix + ficPath + ficName;
                            
            listUrls.Add(url);
            return listUrls;
           
        }
   
    }
}
