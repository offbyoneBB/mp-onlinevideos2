using System;
using MediaPortal.GUI.Library;
using System.Text.RegularExpressions;
using System.Net;
using System.Text;
using System.Xml;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;

namespace OnlineVideos.Sites
{
	public class VeryFunnyAdsUtil : SiteUtilBase
	{		
        public List<VideoInfo> parseEpisodes(String fsUrl)
        {
            List<VideoInfo> loRssItems = new List<VideoInfo>();
            XmlDocument doc = new XmlDocument();

            doc.Load(XmlReader.Create(String.Format("http://tbs.com/veryfunnyads/getCollectionById.do?offset=0&id={0}&sort=&limit=50&daysback=", fsUrl)));            

            XmlNamespaceManager nsMgr = new XmlNamespaceManager(doc.NameTable);           
            XmlNodeList nodeList = doc.SelectNodes("/episodes/episode", nsMgr);
           
            XmlAttributeCollection ac;

            VideoInfo loRssItem;

            foreach (XmlNode chileNode in nodeList)
            {                
                loRssItem = new VideoInfo();

                XmlNode node = chileNode.SelectSingleNode("title",nsMgr);
                loRssItem.Title = node.InnerText;

                //node = chileNode.SelectSingleNode("link");
                //loRssItem.link = node.InnerText;

                node = chileNode.SelectSingleNode("description",nsMgr);
                loRssItem.Description = node.InnerText;

                node = chileNode.SelectSingleNode("fullSizeStillUrl",nsMgr);
                if (node != null)
                {
                    //ac = node.Attributes;
                    loRssItem.ImageUrl = node.InnerText;
                }

                node = chileNode.SelectSingleNode("segments");
                node = node.SelectSingleNode("segment");
                if (node != null)
                {
                    ac = node.Attributes;
                    loRssItem.VideoUrl = ac["id"].InnerText;
                    //loRssItem.VideoUrl = node.InnerText;
                }

                //Log.Write(loRssItem.ToString());
                //loListItem = new GUIListItem(loRssItem.title);
                //loListItem.Path = loRssItem.videoUrl;
                loRssItems.Add(loRssItem);
            }
            return loRssItems;
        }

        public List<Category> parseCollections(String fsUrl)
        {
            List<Category> loRssItems = new List<Category>();
            XmlDocument doc = new XmlDocument();
            doc.Load(XmlReader.Create(fsUrl));

            XmlNamespaceManager nsMgr = new XmlNamespaceManager(doc.NameTable);
            XmlNodeList nodeList = doc.SelectNodes("/collections/collection", nsMgr);
                                   
            foreach (XmlNode chileNode in nodeList)
            {
                RssLink loRssItem = new RssLink();
                loRssItem.Url = chileNode.Attributes["id"].InnerText;
                
                XmlNode node = chileNode.SelectSingleNode("name", nsMgr);
                loRssItem.Name = node.InnerText;                
                loRssItems.Add(loRssItem);
            }
            return loRssItems;
        }

        public override List<Category> getDynamicCategories()
        {            
            //return parseCollections("http://www.tbs.com/veryfunnyads/getCollections.do?id=24823"); - outdated
            return parseCollections("http://www.tbs.com/veryfunnyads/getCollections.do?id=26322");
        }

		public override List<VideoInfo> getVideoList(Category category)
		{
            return parseEpisodes(((RssLink)category).Url);
		}

        public override String getUrl(VideoInfo video, SiteSettings foSite)
		{
            String lsHtml = GetWebData(String.Format("http://www.tbs.com/veryfunnyads/getPlaylistById.do?id={0}", video.VideoUrl));
            Regex regex = new Regex("<url>([^<]*)");
            Match loMatch = regex.Match(lsHtml);
            String lsUrl = "";
            if (loMatch.Success)
            {                
                lsUrl = loMatch.Groups[1].Value;                        
            }
            return lsUrl;
		}		
	}
}
