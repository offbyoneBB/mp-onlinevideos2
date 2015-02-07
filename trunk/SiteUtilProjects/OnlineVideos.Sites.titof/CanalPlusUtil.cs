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
using System.IO;
using System.Text;

namespace OnlineVideos.Sites
{
    public class CanalPlusUtil : SiteUtilBase
    {
     
        [Category("OnlineVideosConfiguration"), Description("urlFichierXMLListeProgrammes")]
        string urlFichierXMLListeProgrammes = "http://www.canalplus.fr/rest/bootstrap.php?/bigplayer/initPlayer";
        [Category("OnlineVideosConfiguration"), Description("urlFichierXMLEmissions")]
        string urlFichierXMLEmissions = "http://www.canalplus.fr/rest/bootstrap.php?/bigplayer/getMEAs/";
        [Category("OnlineVideosConfiguration"), Description("urlFichierXMLFichiers")]
        string urlFichierXMLFichiers = "http://www.canalplus.fr/rest/bootstrap.php?/bigplayer/getVideos/";

        public override int DiscoverDynamicCategories()
        {
            string pageEmissions = GetWebData(urlFichierXMLListeProgrammes, forceUTF8: true);
                        
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(pageEmissions);

            XmlNodeList listCat = doc.SelectNodes("/INIT_PLAYER/THEMATIQUES/THEMATIQUE");
            
            Settings.Categories = new BindingList<Category>();
            foreach (XmlNode n in listCat)
            {
                RssLink cat = new RssLink();
                cat.Name = FirstLetterUpper(n.SelectSingleNode("NOM").InnerText.Trim());
                cat.HasSubCategories = true;
                cat.Other = n;
                Settings.Categories.Add(cat);

            }
            
            Settings.DynamicCategoriesDiscovered = true;

            return listCat.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();

            XmlNodeList list = ((XmlNode)parentCategory.Other).SelectNodes("SELECTIONS/SELECTION");
            foreach (XmlNode n in list)
            {
                RssLink cat = new RssLink();
                cat.Name = FirstLetterUpper(n.SelectSingleNode("NOM").InnerText);
                cat.Other = n.SelectSingleNode("ID").InnerText;
                cat.ParentCategory = parentCategory;
                parentCategory.SubCategories.Add(cat);
            }

            return list.Count;
            
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            string pageEmissions = GetWebData(urlFichierXMLEmissions + category.Other.ToString(), forceUTF8: true);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(pageEmissions);

            XmlNodeList list = doc.SelectNodes("/MEAS/MEA");

            List<VideoInfo> listVideos = new List<VideoInfo>();
             
            foreach (XmlNode n in list)
            {
                VideoInfo video = new VideoInfo();

                string title = FirstLetterUpper(n.SelectSingleNode("INFOS").SelectSingleNode("TITRAGE").SelectSingleNode("TITRE").InnerText.Trim());
                string title2 = n.SelectSingleNode("INFOS").SelectSingleNode("TITRAGE").SelectSingleNode("SOUS_TITRE").InnerText.Trim();

                if (title2.Length > 0)
                {
                    video.Title = title + " (" + title2 + ")";
                }
                else
                {
                    video.Title = title;
                }

                video.Description = n.SelectSingleNode("INFOS").SelectSingleNode("DESCRIPTION").InnerText;
                video.Thumb = n.SelectSingleNode("MEDIA").SelectSingleNode("IMAGES").SelectSingleNode("GRAND").InnerText;
                video.Other = n.SelectSingleNode("ID").InnerText;
                listVideos.Add(video);
            }
            return listVideos;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string pageVideo = GetWebData(urlFichierXMLFichiers + video.Other.ToString());

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(pageVideo);
            string url = doc.SelectSingleNode("/VIDEOS/VIDEO/MEDIA/VIDEOS/HAUT_DEBIT").InnerText;
            return url;
        }


        // Retourne le string entré en paramètre avec
        // une majuscule comme première lettre
        private string FirstLetterUpper(string str)
        {
            char[] letters = str.ToLower().ToCharArray();
            letters[0] = char.ToUpper(letters[0]);

            return new string(letters);
        }

        
    }
}
