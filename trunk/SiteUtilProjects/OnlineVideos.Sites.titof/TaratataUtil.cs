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

namespace OnlineVideos.Sites
{
    public class TaratataUtil : SiteUtilBase
    {

        public override int DiscoverDynamicCategories()
        {           
            RssLink cat = new RssLink();
            cat.Url = "http://www.mytaratata.com/Pages/EMISSIONS_accueil.aspx";
            cat.Name = "Emissions";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://www.mytaratata.com/Pages/VIDEO_accueil.aspx";
            cat.Name = "Vidéos";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://www.mytaratata.com/Pages/ARTISTES_accueil.aspx";
            cat.Name = "Artistes";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://www.mytaratata.com/Pages/JEUNES_TALENTS_accueil.aspx";
            cat.Name = "Jeunes talents";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);
        
            Settings.DynamicCategoriesDiscovered = true;

            return 4;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
                
            if (parentCategory.Name.Equals("Emissions"))
            {
                RssLink cat = new RssLink();
                cat.Url = "http://www.mytaratata.com/Pages/EMISSIONS_accueil.aspx";
                cat.Name = "Les dernières émissions";
                cat.ParentCategory = parentCategory;
                parentCategory.SubCategories.Add(cat);

                List<RssLink> listDates = new List<RssLink>();
                string webData = GetWebData((parentCategory as RssLink).Url);

                Regex r = new Regex(@"<a\sid=""ctl00_ContentPlaceHolder1_DataList2_ctl[^_]*_HyperLinkEmissionsParDates""\shref=""(?<url>[^""]*)"">(?<title>[^<]*)</a>\s*",
                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);

                Match m = r.Match(webData);
                while (m.Success)
                {
                    RssLink date = new RssLink();
                    date.Url =  m.Groups["url"].Value;
                    date.Name =  m.Groups["title"].Value;
                    date.ParentCategory = parentCategory;
                    listDates.Add(date);
                    m = m.NextMatch();
                }
            
                IComparer<RssLink> comp = new dateComparer();
                listDates.Sort(comp);               
                parentCategory.SubCategories.AddRange(listDates.ToArray());

                return listDates.Count + 1;
       
            }

            if (parentCategory.Name.Equals("Vidéos"))
            {

                RssLink cat = new RssLink();
                cat.Url = "http://www.mytaratata.com/Pages/VIDEO_all.aspx?w=genre&amp;g=0&amp;s=0";
                cat.Name = "Les vidéos les plus vues";
                cat.ParentCategory = parentCategory;
                parentCategory.SubCategories.Add(cat);

                cat = new RssLink();
                cat.Url = "http://www.mytaratata.com/Pages/VIDEO_all.aspx?w=last&amp;s=0";
                cat.Name = "Les dernières vidéos ajoutées";
                cat.ParentCategory = parentCategory;
                parentCategory.SubCategories.Add(cat);

                cat = new RssLink();
                cat.Url = "http://www.mytaratata.com/Pages/VIDEO_all.aspx?w=random&amp;s=0";
                cat.Name = "Vidéos aléatoires";
                cat.ParentCategory = parentCategory;
                parentCategory.SubCategories.Add(cat);

                cat = new RssLink();
                cat.Url = "http://www.mytaratata.com/Pages/PLAYLISTS_all.aspx?w=random";
                cat.Name = "Playlists aléatoires";
                cat.ParentCategory = parentCategory;
                parentCategory.SubCategories.Add(cat);

                return 4;
            }
            return 0;

        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            if (((RssLink)category).Url.EndsWith("EMISSIONS_accueil.aspx"))
            {                
                List<VideoInfo> listVideos = new List<VideoInfo>();
                string webData = GetWebData((category as RssLink).Url);

                Regex r = new Regex(@"<div\sclass=""UneEmission"">\s*<a\sid=""[^""]*""\sclass=""LienContourNoir""\shref=""(?<url>[^""]*)""><img\sid=""[^""]*""\ssrc=""[^""]*""\sstyle=""border-width:1px;border-style:Solid;height:120px;width:120px;""\s/><br\s/><br\s/>\s*<span\sid=""[^""]*""\sstyle=""font-weight:bold;"">(?<title>[^<]*)</span><br\s/>\s*",
                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);

                Match m = r.Match(webData);
                while (m.Success)
                {
                    VideoInfo video = new VideoInfo();
                    video.VideoUrl = m.Groups["url"].Value;
                    video.Title = m.Groups["title"].Value;
                    
                    listVideos.Add(video);
                    m = m.NextMatch();
                }
                return listVideos;
            
            }
            if (((RssLink)category).Url.Contains("?date="))
            {
                List<VideoInfo> listVideos = new List<VideoInfo>();
                string webData = GetWebData("http://www.mytaratata.com/Pages/" + (category as RssLink).Url);

                Regex r = new Regex(@"<a\sid=""[^""]*""\sclass=""BoutonVoir""\shref=""(?<VideoUrl>[^""]*)""></a>\s*</td>\s*<td>\s*<div\sclass=""LabelEmission"">\s*<span\sid=""[^""]*"">(?<Title>[^<]*)</span>\s*</div>\s*</td>\s*</tr>\s*<tr>\s*<td>\s*<div\sclass=""LabelInvité"">\s*<span\sid=""[^""]*"">(?<Description>[^<]*)</span>\s*",
                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);

                Match m = r.Match(webData);
                while (m.Success)
                {
                    VideoInfo video = new VideoInfo();
                    video.VideoUrl = m.Groups["VideoUrl"].Value;
                    video.Title = m.Groups["Title"].Value;
                    video.Description = m.Groups["Description"].Value;
                    
                    listVideos.Add(video);
                    m = m.NextMatch();
                }
                return listVideos;
            }

           
            if (((RssLink)category).Url.Contains("VIDEO_all.aspx") || ((RssLink)category).Url.Contains("PLAYLISTS_all.aspx"))
            {
                List<VideoInfo> listVideos = new List<VideoInfo>();
                string webData = GetWebData((category as RssLink).Url);

                Regex r = new Regex(@"class=""[^""]*""\shref=""VIDEO_page_video\.aspx\?sig=(?<url>[^""]*)""\sstyle=""display:inline-block;width:200px;"">(?<title>.*?)</a></td><td><a\sclass=""VideoAllLienArtiste""\shref='[^']*'>(?<description>[^<]*)</a>",
                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);

                Match m = r.Match(webData);
                while (m.Success)
                {
                    VideoInfo video = new VideoInfo();
                    video.VideoUrl = m.Groups["url"].Value;
                    string title = m.Groups["title"].Value;
                    string title2 = title + @".  -  ." + m.Groups["description"].Value;
                    video.Title =  title2;
                    video.Other = "VIDEO";
                    listVideos.Add(video);
                    m = m.NextMatch();
                }
                return listVideos;
            }
            return null;
        }

        public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<string> listUrls = new List<string>();
            string sig = "";
            string playerkey = "919173dd863e";
            string token = GetWebData("http://api.kewego.com/app/getPlayerAppToken/?playerKey=" + playerkey);
            XmlDocument doc1 = new XmlDocument();
            doc1.LoadXml(token);
            token = doc1.DocumentElement.SelectSingleNode("/kewego_response/message/player_app_token").InnerText;
           
            if (video.Other != null && video.Other.Equals("VIDEO"))
            {
                sig = video.VideoUrl;
                listUrls.Add("http://api.kewego.com/video/getStream/?format=hd&sig=" + sig + "&appToken=" + token + "&mode=external&v=7556528");
            }
            else
            {
                sig = Regex.Match(GetWebData("http://www.mytaratata.com/Pages/" + video.VideoUrl), @"csig=(?<m0>[^""]*)""").Groups["m0"].Value;
                string data = GetWebData("http://api.kewego.com/channel/getVideos/?&csig=" + sig + "&appToken=" + token);

                if (!string.IsNullOrEmpty(data))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(data);
                    XmlNode root = doc.DocumentElement.SelectSingleNode("/kewego_response");
                    XmlNodeList list = root.SelectNodes("message/video");
                    int i = 0;
                    foreach (XmlNode node in list)
                    {
                        i++;
                        listUrls.Add("http://api.kewego.com//video/getStreamInChannel/?format=hd&csig=" + sig + "&appToken="+token + "&mode=external&pos=" + i + "&v=4814887");

                    }
                }
            }

            
            return listUrls;
        }

       


        private class dateComparer : IComparer<RssLink> 
        {
            public int Compare(RssLink x, RssLink y) 
            {
                if (int.Parse(x.Name) < int.Parse(y.Name)) 
                    return 1;

                if (int.Parse(x.Name) > int.Parse(y.Name))
                    return -1;

                return 0;
            }
        }
      
    }
}
