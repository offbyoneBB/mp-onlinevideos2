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
    public class ZDFMediathekUtil : SiteUtilBase, ISearch
    {
        RestAgent agent;
        RestAgent Agent 
        { 
            get 
            {
                if (agent == null)
                {
                    agent = new RestAgent("http://www.zdf.de/ZDFmediathek/tv/mceservice/1.9") { CVer = "1.4.3" };
                }
                return agent;
            } 
        }
        Dictionary<string, string> categoriesForSearching = new Dictionary<string, string>();

        public override int DiscoverDynamicCategories(SiteSettings site)
        {
            site.Categories.Clear();
            foreach (Teaser teaser in Agent.Inhalt(Kanaltyp.Sendungen, "Alle"))
            {
                RssLink item = new RssLink();
                item.Name = convertUnicodeU(teaser.Titel);
                item.Url = teaser.ID;
                item.Thumb = teaser.Teaserbilder[0].Url;
                site.Categories.Add(item.Name, item);
                categoriesForSearching.Add(item.Name, item.Url);
            }
            site.DynamicCategoriesDiscovered = true;
            return site.Categories.Count;
        }
        
        public override String getUrl(VideoInfo video, SiteSettings foSite)        
        {
            string fsId = video.VideoUrl;
            Beitrag beitrag = Agent.Beitrag(fsId);
            string s = GetWebData(beitrag.StreamUrl);
            XmlDocument document = new XmlDocument();
            document.Load(XmlReader.Create(new StringReader(s)));
            return document.SelectSingleNode("//ASX/Entry/Ref").Attributes.GetNamedItem("href").InnerText;
        }        

        public override List<VideoInfo> getVideoList(Category category)
        {
            string fsSubUrl = (category as RssLink).Url;
            Suchergebnis suchergebnis = Agent.Suchergebnis(" ", 1, 0x1869f, SortOption.Datum, false, fsSubUrl, true, false, false, false, new DateTime(), new DateTime());
            return GetTeasersFromSuchergebnis(suchergebnis);
        }

        protected List<VideoInfo> GetTeasersFromSuchergebnis(Suchergebnis suchergebnis)
        {
            List<VideoInfo> list = new List<VideoInfo>();
            foreach (Teaser teaser in suchergebnis.Teasers)
            {
                if (teaser.Typ == Beitragstype.Video)
                {
                    VideoInfo item = new VideoInfo();
                    item.ImageUrl = teaser.Teaserbilder[0].Url;
                    item.Title = teaser.Titel + " (" + teaser.Datum + " " + teaser.Startzeit + ")";
                    item.Description = teaser.Beschreibung;
                    item.Length = teaser.Laenge;
                    item.VideoUrl = teaser.ID;
                    list.Add(item);
                }
            }
            return list;
        }

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

        #region ISearch Member

        public Dictionary<string, string> GetSearchableCategories(Category[] configuredCategories)
        {
            return categoriesForSearching;
        }

        public List<VideoInfo> Search(string searchUrl, string query)
        {
            Suchergebnis suchergebnis = Agent.Suchergebnis(query, 1, 0x1869f, SortOption.Datum, false, "Alle", true, false, false, false, new DateTime(), new DateTime());
            return GetTeasersFromSuchergebnis(suchergebnis);            
        }

        public List<VideoInfo> Search(string searchUrl, string query, string category)
        {
            return Search(searchUrl, query);
        }

        #endregion
    }
}

