using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using System.Text.RegularExpressions;
using MediaPortal.GUI.Library;

namespace OnlineVideos.Sites
{
	/// <summary>
	/// Description of CnnUtil.
	/// </summary>
	public class CnnUtil: SiteUtilBase
	{
		
		public override string getUrl(OnlineVideos.VideoInfo video, OnlineVideos.SiteSettings foSite)
		{
			return String.Format("http://vid.cnn.com/cnn/big{0}_576x324_dl.flv",video.VideoUrl);
		}
		
        public override List<VideoInfo> getVideoList(Category category)
		{
			List<VideoInfo> videoList = new List<VideoInfo>();
			//XmlTextReader r  = new XmlTextReader("http://www.cnn.com/.element/ssi/www/auto/2.0/video/xml/by_section_us.xml?0.8373572623494244");			
            XmlTextReader r = new XmlTextReader(((RssLink)category).Url);			
			VideoInfo video = null;
			XmlDocument doc = new XmlDocument();
			doc.Load(r);
			XmlNode root = doc.SelectSingleNode("//cnn_video/video");
			XmlNodeList nodeList;
			nodeList = root.SelectNodes("//cnn_video/video");
            Log.Info("Cnn videos found:"+nodeList.Count);
			//XmlAttributeCollection ac;
			foreach(XmlNode child in nodeList)
			{
                try
                {
                    video = new VideoInfo();

                    for (int i = 0; i < child.ChildNodes.Count; i++)
                    {

                        XmlNode n = child.ChildNodes[i];

                        switch (n.Name)
                        {
                            case "video_id":
                                video.VideoUrl = Regex.Split(n.InnerText, "/video")[1];
                                break;
                            case "image_url":
                                video.ImageUrl = n.InnerText;
                                break;
                            case "tease_txt":
                                video.Title = n.InnerText;
                                break;
                            case "vid_duration":
                                video.Length = n.InnerText;
                                break;
                        }
                    }
                    videoList.Add(video);
                }
                catch (Exception e) {
                    Log.Info("Failed ToString parse video:"+ child.Value);
                    Log.Error(e);
                    
                }
			}
			return videoList;
		}
	}
}
