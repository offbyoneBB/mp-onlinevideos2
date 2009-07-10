using System;
using System.Collections.Generic;
using System.Text;
using YahooMusicEngine;
using YahooMusicEngine.OnlineDataProvider;
using YahooMusicEngine.Entities;
using YahooMusicEngine.Services;

namespace OnlineVideos.Sites
{
    public class YahooMusicVideosUtil : SiteUtilBase, ISearch
    {
        YahooSettings settings;
        CategoryTreeService catserv;
        ServiceProvider provider;
        VideosForACategoryService videoInCatList = new VideosForACategoryService();
        SearchForVideosService videoSearchList = new SearchForVideosService();
        VideosForGivenPublishedListService videoPublishedList = new VideosForGivenPublishedListService();

        public override List<VideoInfo> getVideoList(Category category)
        {
            string id = ((RssLink)category).Url;

            List<VideoInfo> videoList = new List<VideoInfo>();

            if (id == "popular" || id == "new")
            {
                videoPublishedList.Id = id;
                provider.GetData(videoPublishedList);

                foreach (VideoResponse vi in videoPublishedList.Items)
                {
                    if (!string.IsNullOrEmpty(vi.Video.Id))
                    {
                        VideoInfo videoInfo = new VideoInfo();
                        videoInfo.Title = FormatTitle(vi);
                        videoInfo.Length = vi.Video.Duration.ToString();
                        videoInfo.VideoUrl = vi.Video.Id;
                        videoInfo.ImageUrl = vi.Image.Url;
                        videoList.Add(videoInfo);
                    }
                }
            }
            else
            {
                CategoryEntity cat = catserv.Items.Find(new Predicate<CategoryEntity>(delegate(CategoryEntity ce) { return ce.Id == id; }));

                videoInCatList.Category = id;
                videoInCatList.Count = cat.VideoCount;
                provider.GetData(videoInCatList);

                foreach (VideoResponse vi in videoInCatList.Items)
                {
                    if (!string.IsNullOrEmpty(vi.Video.Id))
                    {
                        VideoInfo videoInfo = new VideoInfo();
                        videoInfo.Title = FormatTitle(vi);
                        videoInfo.Length = vi.Video.Duration.ToString();
                        videoInfo.VideoUrl = vi.Video.Id;
                        videoInfo.ImageUrl = vi.Image.Url;
                        videoList.Add(videoInfo);
                    }
                }
            }

            return videoList;
        }

        public override int DiscoverDynamicCategories(SiteSettings site)
        {
            if (settings == null)
            {
                settings = new YahooSettings();
                settings.Load();
            }

            if (provider == null)
            {
                provider = new ServiceProvider();
                provider.AppId = "DeUZup_IkY7d17O2DzAMPoyxmc55_hTasA--";
                provider.SharedSecret = "d80b9a5766788713e1fadd73e752c7eb";
                provider.Token = settings.Token;
                provider.SetLocale(settings.Locale);
                provider.Init();
            }
            else if (provider.Error)
            {
                provider.Error = false;
                provider.Init();
            }

            if (provider.Error)
            {
                MediaPortal.Dialogs.GUIDialogNotify dlg = (MediaPortal.Dialogs.GUIDialogNotify)MediaPortal.GUI.Library.GUIWindowManager.GetWindow((int)MediaPortal.GUI.Library.GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                dlg.SetHeading("ERROR");
                dlg.SetText("Yahoo Authentication Token invalid. Please check Configuration.");
                dlg.DoModal(MediaPortal.GUI.Library.GUIWindowManager.ActiveWindow);
                return 0;
            }

            if (catserv == null)
            {
                catserv = new CategoryTreeService();
                catserv.Type = CategoryTreeTypes.genre;
                provider.GetData(catserv);                
            }

            site.Categories.Clear();

            RssLink popularItem = new RssLink();
            popularItem.Name = "Popular";
            popularItem.Url = "popular";
            site.Categories.Add(popularItem.Name, popularItem);

            RssLink newItem = new RssLink();
            newItem.Name = "New";
            newItem.Url = "new";
            site.Categories.Add(newItem.Name, newItem);

            foreach (CategoryEntity cat in catserv.Items)
            {
                RssLink loRssItem = new RssLink();
                loRssItem.Name = cat.Name;
                loRssItem.Url = cat.Id;
                site.Categories.Add(loRssItem.Name, loRssItem);
            }

            site.DynamicCategoriesDiscovered = true;
            return site.Categories.Count;
        }

        public override String getUrl(VideoInfo video, SiteSettings foSite)
        {
            RTMP_LIB.Link link = YahooRTMPLinkCatcher(video.VideoUrl);
            string resultUrl = string.Format("http://localhost:20004/stream.flv?app={0}&tcUrl={1}&hostname={2}&port={3}&playpath={4}",
                System.Web.HttpUtility.UrlEncode(link.app),
                System.Web.HttpUtility.UrlEncode(link.tcUrl),
                System.Web.HttpUtility.UrlEncode(link.hostname),
                link.port,
                System.Web.HttpUtility.UrlEncode(link.playpath));
            return resultUrl;

            /*
            VideoPlayer player = new VideoPlayer(video.VideoUrl);
            player.AutoStart = true;
            if (settings.Bandwith != 0) player.Bandwidth = settings.Bandwith;
            player.Ympsc = provider.Locale.Ympsc;
            player.EID = provider.Locale.EID;
            player.VideoIds.Clear();            
            return player.VideoPlayerUrl;
            */
        }

        static RTMP_LIB.Link YahooRTMPLinkCatcher(string videoId)
        {
            RTMP_LIB.Link link = new RTMP_LIB.Link();

            string url = "http://video.music.yahoo.com/ver/268.0/process/getPlaylistFOP.php?node_id=v" + videoId + "&tech=flash&bitrate=5000&eventid=1301797";
            System.Xml.XmlDocument data = new System.Xml.XmlDocument();
            data.Load(System.Xml.XmlReader.Create(url));

            System.Xml.XmlElement stream = (System.Xml.XmlElement)data.SelectSingleNode("//SEQUENCE-ITEM[@TYPE='S_STREAM']/STREAM");
            if (stream != null)
            {
                link.tcUrl = stream.Attributes["APP"].Value;
                link.hostname = stream.Attributes["SERVER"].Value;
                link.port = 1935;
                string queryString = stream.Attributes["QUERYSTRING"].Value;
                string path = stream.Attributes["PATH"].Value.Substring(1);
                link.app = link.tcUrl.Substring(link.tcUrl.LastIndexOf('/') + 1);
                link.playpath = path.Substring(0, path.LastIndexOf('.')) + '?' + queryString;
            }

            return link;
        }

        string FormatTitle(VideoResponse vid)
        {
            if (string.IsNullOrEmpty(settings.Format_Title))
                return vid.Artist.Name + " - " + vid.Video.Title;
            string s = settings.Format_Title;
            s = s.Replace("%title%", vid.Video.Title);
            s = s.Replace("%artist%", vid.Artist.Name);
            s = s.Replace("%year%", vid.Video.CopyrightYear.ToString());
            s = s.Replace("%rating%", vid.Video.Rating.ToString());
            s = System.Web.HttpUtility.HtmlDecode(s);
            return s;
        }


        #region ISearch Member

        public Dictionary<string, string> GetSearchableCategories(Category[] configuredCategories)
        {
            Dictionary<string, string> loRssItems = new Dictionary<string, string>();
            if (catserv != null)
            {
                foreach (CategoryEntity cat in catserv.Items)
                {
                    loRssItems.Add(cat.Name, cat.Id);
                }
            }
            return loRssItems;
        }

        public List<VideoInfo> Search(string searchUrl, string query)
        {
            videoSearchList.Category = "";
            videoSearchList.Keyword = query;            
            provider.GetData(videoSearchList);

            List<VideoInfo> loRssItemList = new List<VideoInfo>();

            foreach (VideoResponse vi in videoSearchList.Items)
            {
                if (!string.IsNullOrEmpty(vi.Video.Id))
                {
                    VideoInfo loRssItem = new VideoInfo();
                    loRssItem.Title = FormatTitle(vi);
                    loRssItem.Length = vi.Video.Duration.ToString();
                    loRssItem.VideoUrl = vi.Video.Id;
                    loRssItem.ImageUrl = vi.Image.Url;
                    loRssItemList.Add(loRssItem);
                }
            }

            return loRssItemList;            
        }

        public List<VideoInfo> Search(string searchUrl, string query, string category)
        {
            videoSearchList.Category = category;
            videoSearchList.Keyword = query;
            provider.GetData(videoSearchList);

            List<VideoInfo> loRssItemList = new List<VideoInfo>();

            foreach (VideoResponse vi in videoSearchList.Items)
            {
                if (!string.IsNullOrEmpty(vi.Video.Id))
                {
                    VideoInfo loRssItem = new VideoInfo();
                    loRssItem.Title = FormatTitle(vi);
                    loRssItem.Length = vi.Video.Duration.ToString();
                    loRssItem.VideoUrl = vi.Video.Id;
                    loRssItem.ImageUrl = vi.Image.Url;
                    loRssItemList.Add(loRssItem);
                }
            }

            return loRssItemList;            

        }

        #endregion
    }
}
