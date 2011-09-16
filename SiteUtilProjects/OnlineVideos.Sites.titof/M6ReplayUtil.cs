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
    public class M6ReplayUtil : SiteUtilBase
    {
        [Category("OnlineVideosConfiguration"), Description("CatalogueWeb")]
        protected string catalogueWeb = "http://www.m6replay.fr/catalogue/catalogueWeb3.xml";
        [Category("OnlineVideosConfiguration"), Description("ThumbURL")]
        protected string thumbURL = "http://images.m6replay.fr";
        [Category("OnlineVideosConfiguration"), Description("ServerURL1")]
        protected string serverURL1 = "rtmp://groupemsix.fcod.llnwd.net/a3100/d1/";
        [Category("OnlineVideosConfiguration"), Description("ServerURL2")]
        protected string serverURL2 = "rtmpe://m6replayfs.fplive.net/m6replay/streaming/";
        [Category("OnlineVideosConfiguration"), Description("ServerURL3")]
        protected string serverURL3 = "rtmpe://m6dev.fcod.llnwd.net:443/a3100/d1/";
        [Category("OnlineVideosConfiguration"), Description("PlayerURL")]
        protected string playerURL = "http://l3.player.M6.fr/swf/ReplayPlayer_20110228.swf";
        [Category("OnlineVideosConfiguration"), Description("PlayerSize")]
        protected string playerSize = "1197361";
        [Category("OnlineVideosConfiguration"), Description("PlayerSHA")]
        protected string playerSHA = "2166742885D94CD229060D98EE976F78F16953EAB0ECC431736DFFD153C7EAA4";



        private XmlDocument doc = new XmlDocument();
            
        public override int DiscoverDynamicCategories()
        {
            string pageEmissions = GetWebData(catalogueWeb);

            //Si le fichier est crypté (cas M6, pas W9)
            if (!pageEmissions.Contains(@"<?xml version=""1.0"" encoding=""UTF-8""?>"))
            {
                pageEmissions = Decrypt(pageEmissions, "ElFsg.Ot");
            }           
            
            doc = new XmlDocument();
            doc.LoadXml(pageEmissions);
            
            XmlNodeList listCat = doc.SelectNodes("/template_exchange_WEB/categorie");
            
            Settings.Categories = new BindingList<Category>();
            foreach (XmlNode n in listCat)
            {
                RssLink cat = new RssLink();
                cat.Name = n.SelectSingleNode("nom").InnerText;
                cat.Thumb = thumbURL + n.Attributes["big_img_url"].Value;
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

            XmlNodeList list = ((XmlNode)parentCategory.Other).SelectNodes("categorie");
            foreach (XmlNode n in list)
            {
                RssLink cat = new RssLink();
                cat.Name = n.SelectSingleNode("nom").InnerText;
                cat.Thumb = thumbURL + n.Attributes["big_img_url"].Value;
                cat.ParentCategory = parentCategory;
                cat.Other = n;
                parentCategory.SubCategories.Add(cat);
            }
           
            return list.Count;
            
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> listVideos = new List<VideoInfo>();
                
            XmlNodeList list = ((XmlNode)category.Other).SelectNodes("produit");
            foreach (XmlNode n in list)
            {
                VideoInfo video = new VideoInfo();

                video.PlaybackOptions = new Dictionary<string, string>();

                foreach (XmlNode media in n.SelectNodes("fichemedia"))
                {
                    string resultUrl1 = ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                                    string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&swfurl={1}&swfsize={2}&swfhash={3}",
                                        System.Web.HttpUtility.UrlEncode(serverURL1 + media.Attributes["video_url"].Value)
                                        , playerURL, 
                                        playerSize, playerSHA
                                    ));
                    video.PlaybackOptions.Add(media.Attributes["langue"].Value + " : Serveur 1", resultUrl1);

                    string resultUrl2 = ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                                    string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&swfurl={1}&swfsize={2}&swfhash={3}",
                                        System.Web.HttpUtility.UrlEncode(serverURL2 + media.Attributes["video_url"].Value)
                                        , playerURL,
                                        playerSize, playerSHA
                                    ));
                    video.PlaybackOptions.Add(media.Attributes["langue"].Value + " : Serveur 2", resultUrl2);

                    string resultUrl3 = ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                                    string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&swfurl={1}&swfsize={2}&swfhash={3}",
                                        System.Web.HttpUtility.UrlEncode(serverURL3 + media.Attributes["video_url"].Value)
                                        , playerURL,
                                        playerSize, playerSHA
                                    ));
                    video.PlaybackOptions.Add(media.Attributes["langue"].Value + " : Serveur 3", resultUrl3);
                }

                video.VideoUrl = "";
                video.Title = n.SelectSingleNode("nom").InnerText;
                video.Description = n.SelectSingleNode("resume").InnerText;
                video.ImageUrl = thumbURL + n.Attributes["big_img_url"].Value;
                video.Length = n.SelectSingleNode("fichemedia").Attributes["duree"].Value;
                listVideos.Add(video);
            }
            return listVideos;
        }

        public override string getUrl(VideoInfo video)
        {
            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
            {
                var enumer = video.PlaybackOptions.GetEnumerator();
                enumer.MoveNext();
                return enumer.Current.Value;
            }
            return "";
        }

        /// <summary>
        /// Décrypte une chaine cryptée à partir d'un chiffreur symétrique
        /// </summary>
        /// <param name="base64String">chaine cryptée</param>
        /// <param name="pass">Mot de passe utilisé pour dériver la clé</param>
        /// <returns>Chaine décryptée</returns>
        private static string Decrypt(string base64String, string pass)
        {
            string result = string.Empty;

            System.Security.Cryptography.DESCryptoServiceProvider des =
                new System.Security.Cryptography.DESCryptoServiceProvider();
            des.Mode = CipherMode.ECB;
            des.IV = new byte[8];
            System.Security.Cryptography.PasswordDeriveBytes pdb =
                new System.Security.Cryptography.PasswordDeriveBytes(pass, new byte[0]);
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();

            des.Key = encoding.GetBytes(pass);
            byte[] encryptedBytes = Convert.FromBase64String(base64String);

            using (MemoryStream ms = new MemoryStream(base64String.Length))
            {
                using (System.Security.Cryptography.CryptoStream decStream =
                    new System.Security.Cryptography.CryptoStream(ms, des.CreateDecryptor(),
                        System.Security.Cryptography.CryptoStreamMode.Write))
                {
                    decStream.Write(encryptedBytes, 0, encryptedBytes.Length);
                    decStream.FlushFinalBlock();
                    byte[] plainBytes = new byte[ms.Length];
                    ms.Position = 0;
                    ms.Read(plainBytes, 0, (int)ms.Length);
                    result = Encoding.UTF8.GetString(plainBytes);
                }
            }
            return result;
        }      
    }
}
