using MediaPortal.Configuration;
using MediaPortal.Profile;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Xml;
using ZDF.BL;

namespace OnlineVideos.Sites
{
    public class ZDFMediathekUtil : SiteUtilBase
    {
        protected static bool _stopped = false;
        private static List<List<VideoInfo>> listOfAllLinks = new List<List<VideoInfo>>();
        private static List<BackgroundWorker> listOfWorkers = new List<BackgroundWorker>();
        protected eBandwidthType m_iBandwidthType = eBandwidthType.DSL2000;
        
        protected string ConvertUmlaut(string strIN)
        {
            return strIN.Replace("\x00c4", "Ae").Replace("\x00e4", "ae").Replace("\x00d6", "Oe").Replace("\x00f6", "oe").Replace("\x00dc", "Ue").Replace("\x00fc", "ue");
        }

        protected static string convertUnicodeD(string source)
        {
            int startIndex = 0;
            do
            {
                startIndex = source.IndexOf("&#", startIndex);
                if (startIndex > 0)
                {
                    int num3;
                    int index = source.IndexOf(";", startIndex);
                    int.TryParse(source.Substring(startIndex + 2, (index - startIndex) - 2), out num3);
                    source = source.Replace(source.Substring(startIndex, (index - startIndex) + 1), char.ConvertFromUtf32(num3).ToString());
                }
            }
            while (startIndex > 0);
            return source;
        }

        protected static string convertUnicodeU(string source)
        {
            int startIndex = 0;
            do
            {
                startIndex = source.IndexOf(@"\u", startIndex);
                if (startIndex > 0)
                {
                    int num2;
                    int.TryParse(source.Substring(startIndex + 2, 4), NumberStyles.HexNumber, null, out num2);
                    source = source.Replace(source.Substring(startIndex, 6), char.ConvertFromUtf32(num2).ToString());
                }
            }
            while (startIndex > 0);
            return source;
        }

        protected string getCachedHTMLData(string fsUrl)
        {
            return convertUnicodeU(convertUnicodeD(GetWebData(fsUrl)));
        }

        public override List<Category> getDynamicCategories()
        {            
            List<Category> list = new List<Category>();
            RestAgent agent = new RestAgent("http://www.zdf.de/ZDFmediathek/tv/mceservice/1.9");
            agent.CVer = "1.4.0";
            foreach (Teaser teaser in agent.Inhalt(Kanaltyp.Sendungen, "Alle"))
            {
                RssLink item = new RssLink();
                item.Name = convertUnicodeU(teaser.Titel);
                item.Url = teaser.ID;
                //item.iconUrl = teaser.Teaserbilder[0].Url;
                //item.iDepth = 19;
                list.Add(item);
            }
            return list;
        }

        public string getFileNameForRecord(VideoInfo link)
        {
            return (this.ConvertUmlaut(link.Title) + ".asf");
        }

        public override String getUrl(VideoInfo video, SiteSettings foSite)        
        {
            string fsId = video.VideoUrl;
            RestAgent agent = new RestAgent("http://www.zdf.de/ZDFmediathek/tv/mceservice/1.9");
            agent.CVer = "1.4.0";
            Beitrag beitrag = agent.Beitrag(fsId);
            string s = GetWebData(beitrag.StreamUrl);
            XmlDocument document = new XmlDocument();
            document.Load(XmlReader.Create(new StringReader(s)));
            return document.SelectSingleNode("//ASX/Entry/Ref").Attributes.GetNamedItem("href").InnerText;
        }        

        public override List<VideoInfo> getVideoList(Category category)
        {
            string fsSubUrl = (category as RssLink).Url;
            RestAgent agent = new RestAgent("http://www.zdf.de/ZDFmediathek/tv/mceservice/1.9");
            agent.CVer = "1.4.0";
            Suchergebnis suchergebnis = agent.Suchergebnis(" ", 1, 0x1869f, SortOption.Datum, false, fsSubUrl, true, false, false, false, new DateTime(), new DateTime());
            List<VideoInfo> list = new List<VideoInfo>();
            foreach (Teaser teaser in suchergebnis.Teasers)
            {
                if (teaser.Typ == Beitragstype.Video)
                {
                    VideoInfo item = new VideoInfo();
                    item.ImageUrl = teaser.Teaserbilder[0].Url;
                    item.Title = teaser.Titel + "(" + teaser.Datum + " " + teaser.Startzeit + " - " + teaser.Laenge + ")";
                    item.Description = teaser.Beschreibung;
                    item.VideoUrl = teaser.ID;
                    list.Add(item);
                }
            }
            return list;
        }      

        public eBandwidthType BandwidthType
        {
            get
            {
                return this.m_iBandwidthType;
            }
            set
            {
                this.m_iBandwidthType = value;
            }
        }
        
        public enum eBandwidthType
        {
            DSL2000,
            DSL1000,
            ISDN
        }
    }
}

