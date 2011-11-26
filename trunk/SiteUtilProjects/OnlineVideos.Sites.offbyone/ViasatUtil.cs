using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Xml;
using System.Web;

namespace OnlineVideos.Sites
{
    public class ViasatUtil : SiteUtilBase
    {
        enum ViasatChannel { Sport = 1, TV3, TV6, TV8 };

		[Category("OnlineVideosConfiguration"), Description("Url of the swf file that used for playing the videos and rtmp verification")]
		protected string swfUrl = "http://flvplayer.viastream.viasat.tv/flvplayer/play/swf/player.swf";

		protected string redirectedSwfUrl;

        public override int DiscoverDynamicCategories()
        {
			redirectedSwfUrl = GetRedirectedUrl(swfUrl); // rtmplib does not work with redirected urls to swf files - we find the actual url here

            Settings.Categories.Clear();
            foreach (int i in Enum.GetValues(typeof(ViasatChannel)))
            {
                Category cat = new Category() { Name = ((ViasatChannel)i).ToString(), HasSubCategories = true, SubCategoriesDiscovered = false };
                Settings.Categories.Add(cat);
            }
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

		public override int DiscoverSubCategories(Category parentCategory)
		{
			Category channelCategory = parentCategory;
			while (channelCategory.ParentCategory != null) channelCategory = channelCategory.ParentCategory;
			ViasatChannel channel = (ViasatChannel)Enum.Parse(typeof(ViasatChannel), channelCategory.Name);
			string id = parentCategory is RssLink ? ((RssLink)parentCategory).Url : "0";
			XmlDocument xDoc;
			if (channel != ViasatChannel.Sport)
			{
				xDoc = GetWebData<XmlDocument>("http://viastream.viasat.tv/siteMapData/se/" + ((int)channel).ToString() + "se/" + id);
			}
			else
			{
				xDoc = GetWebData<XmlDocument>("http://viastream.player.mtgnewmedia.se/xml/xmltoplayer.php?type=siteMapData&channel=" + ((int)channel).ToString() + "se&country=se&category=" + id);
			}
			parentCategory.SubCategories = new List<Category>();
			foreach (XmlElement e in xDoc.SelectNodes("//siteMapNode"))
			{
				RssLink subCat = new RssLink() { Name = e.Attributes["title"].Value, Url = e.Attributes["id"].Value, ParentCategory = parentCategory };
				if (e.Attributes["children"].Value == "true") subCat.HasSubCategories = true;
				parentCategory.SubCategories.Add(subCat);
			}
			parentCategory.SubCategoriesDiscovered = true;
			return parentCategory.SubCategories.Count;
		}


        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            {
                Category channelCategory = category;
                while (channelCategory.ParentCategory != null) channelCategory = channelCategory.ParentCategory;
                ViasatChannel channel = (ViasatChannel)Enum.Parse(typeof(ViasatChannel), channelCategory.Name);
                string doc = "";
                if (channel != ViasatChannel.Sport)
                {
                    doc = GetWebData("http://viastream.viasat.tv/Products/Category/" + ((RssLink)category).Url);
                }
                else
                {
                    doc = GetWebData("http://viastream.player.mtgnewmedia.se/xml/xmltoplayer.php?type=Products&category=" + ((RssLink)category).Url);
                }
                XmlDocument xDoc = new XmlDocument();
                xDoc.LoadXml(doc);
                foreach (XmlElement e in xDoc.SelectNodes("//Product"))
                {
                    VideoInfo video = new VideoInfo();
                    video.Title = e.SelectSingleNode("Title").InnerText;
                    video.VideoUrl = e.SelectSingleNode("ProductId").InnerText;
                    video.Other = channel.ToString();
                    videos.Add(video);
                }
            }
            return videos;
        }

		public override string getUrl(VideoInfo video)
		{
			ViasatChannel channel = (ViasatChannel)Enum.Parse(typeof(ViasatChannel), (string)video.Other);
			string doc = "";
			if (channel != ViasatChannel.Sport)
			{
				doc = GetWebData("http://viastream.viasat.tv/Products/" + video.VideoUrl);
			}
			else
			{
				doc = GetWebData("http://viastream.player.mtgnewmedia.se/xml/xmltoplayer.php?type=Products&clipid=" + video.VideoUrl);
			}

			doc = System.Text.RegularExpressions.Regex.Replace(doc, "&(?!amp;)", "&amp;");
			XmlDocument xDoc = new XmlDocument();
			xDoc.LoadXml(doc);

			string playstr = xDoc.SelectSingleNode("Products/Product/Videos/Video/Url").InnerText;

			XmlNode geo;
			if ((geo = xDoc.SelectSingleNode("Products/Product/Geoblock")) != null)
			{
				if (geo.InnerText == "true")
				{
					xDoc.LoadXml(GetWebData(playstr));
					if (xDoc.SelectSingleNode("GeoLock/Success").InnerText != "false")
					{
						playstr = xDoc.SelectSingleNode("GeoLock/Url").InnerText;
					}
					else
					{
						throw new OnlineVideosException(xDoc.SelectSingleNode("GeoLock/Msg").InnerText);
					}
				}
			}

			if (playstr.ToLower().StartsWith("rtmp"))
			{
				int mp4Index = playstr.ToLower().IndexOf("mp4:flash");
				if (mp4Index > 0)
				{
					playstr = new MPUrlSourceFilter.RtmpUrl(playstr.Substring(0, mp4Index)) { PlayPath = playstr.Substring(mp4Index), SwfUrl = redirectedSwfUrl, SwfVerify = true }.ToString();
				}
				else
				{
					playstr = new MPUrlSourceFilter.RtmpUrl(playstr) { SwfUrl = redirectedSwfUrl, SwfVerify = true }.ToString();
				}
			}

			return playstr;
		}
    }
}
