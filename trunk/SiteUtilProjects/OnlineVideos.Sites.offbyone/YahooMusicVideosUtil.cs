using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using YahooMusicEngine;
using YahooMusicEngine.OnlineDataProvider;
using YahooMusicEngine.Entities;
using YahooMusicEngine.Services;
using System.Drawing.Design;
using System.Windows.Forms.Design;
using System.Windows.Forms;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Api Documentation at : http://www.yahooapis.com/music/
    /// </summary>
    public class YahooMusicVideosUtil : SiteUtilBase
    {
        public class UITokenEditor : UITypeEditor
        {
            IWindowsFormsEditorService editorService;

            public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
            {
                return UITypeEditorEditStyle.Modal;
            }

            public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
            {
                if (provider != null)
                {
                    editorService =
                        provider.GetService(
                        typeof(IWindowsFormsEditorService))
                        as IWindowsFormsEditorService;
                }

                if (editorService != null)
                {
                    OnlineVideos.BrowserForm form = new BrowserForm();
                    form.webBrowser1.DocumentCompleted += WebBrowserDocumentCompleted;
                    Yahoo.Authentication auth = new Yahoo.Authentication(YahooMusicVideosUtil.AppId, YahooMusicVideosUtil.SharedSecret);
                    form.webBrowser1.Url = auth.GetUserLogOnAddress();
                    editorService.ShowDialog(form);
                    value = Token;
                }

                return value;
            }

            string Token;

            private void WebBrowserDocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
            {
                Token = string.Empty;
                if (e.Url.DnsSafeHost.Contains("extra.hu"))
                {
                    try
                    {
                        Token = System.Web.HttpUtility.ParseQueryString(e.Url.Query)["token"];
                        ((sender as WebBrowser).Parent as Form).Close();
                    }
                    catch {}
                }
            }
        }

        public enum Locale 
        { 
            [Description("United States")]us, 
            [Description("United States (Español)")]e1, 
            [Description("Canada")]ca, 
            [Description("Mexico")]mx, 
            [Description("Australia")]au, 
            [Description("New Zealand")]nz 
        };

        const string AppId = "DeUZup_IkY7d17O2DzAMPoyxmc55_hTasA--";
        const string SharedSecret = "d80b9a5766788713e1fadd73e752c7eb";

        [Editor("OnlineVideos.Sites.YahooMusicVideosUtil+UITokenEditor", typeof(UITypeEditor)),
        Category("OnlineVideosUserConfiguration"), 
        Description("You can, but don't have to sign in with your Yahoo account to access user specific features of this service.")]
        string token = "";

        [Category("OnlineVideosUserConfiguration"), Description("Language (Country) to use when accessing this service.")]
        Locale locale = Locale.us;
        [Category("OnlineVideosUserConfiguration"), Description("How each video title is displayed. These tags will be replaced: %artist% %title% %year% %rating%.")]
        string format_Title = "%artist% - %title%";
        [Category("OnlineVideosUserConfiguration"), Description("Defines number of videos to display per page.")]
        int pageSize = 27;
        
        CategoryTreeService catserv;
        ServiceProvider provider;
        VideosForACategoryService videoInCatList = new VideosForACategoryService();
        SearchForVideosService videoSearchList = new SearchForVideosService();
        VideosForGivenPublishedListService videoPublishedList = new VideosForGivenPublishedListService();

        List<VideoInfo> GetVideoForCurrentCategory()        
        {
            string id = ((RssLink)currentCategory).Url;

            List<VideoInfo> videoList = new List<VideoInfo>();

            if (id == "popular" || id == "new")
            {
                videoPublishedList.Id = id;
                videoPublishedList.Start = currentStart;
                videoPublishedList.Count = pageSize;
                provider.GetData(videoPublishedList);
                ((RssLink)currentCategory).EstimatedVideoCount = (uint)videoPublishedList.Total;

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
                //CategoryEntity cat = catserv.Find(id);
                videoInCatList.Category = id;
                videoInCatList.Start = currentStart;
                videoInCatList.Count = pageSize;
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

        public override List<VideoInfo> getVideoList(Category category)
        {
            currentCategory = category as RssLink;
            currentStart = 1;
            return GetVideoForCurrentCategory();
        }

        public override int DiscoverDynamicCategories()
        {
            if (provider == null)
            {
                provider = new ServiceProvider();
                provider.AppId = AppId;
                provider.SharedSecret = SharedSecret;
                provider.Token = token;
                provider.SetLocale(locale.ToString());
                provider.Init();
            }
            else if (provider.Error)
            {
                provider.Error = false;
                provider.Init();
            }
            if (provider.Error)
            {
                throw new OnlineVideosException("Yahoo Authentication Token invalid. Please check Configuration.");
            }
            if (catserv == null)
            {
                catserv = new CategoryTreeService();
                catserv.Type = CategoryTreeTypes.genre;
                provider.GetData(catserv);                
            }

            Settings.Categories.Clear();

            RssLink popularItem = new RssLink();
            popularItem.Name = "Popular";
            popularItem.Url = "popular";
            Settings.Categories.Add(popularItem);

            RssLink newItem = new RssLink();
            newItem.Name = "New";
            newItem.Url = "new";
            Settings.Categories.Add(newItem);

            foreach (CategoryEntity cat in catserv.Items)
            {
                RssLink loRssItem = new RssLink();
                loRssItem.Name = cat.Name;                
                loRssItem.Url = cat.Id;
                loRssItem.EstimatedVideoCount = (uint)cat.VideoCount;
                Settings.Categories.Add(loRssItem);

                // create sub cats tree
                if (cat.Childs != null && cat.Childs.Count > 0)
                {
                    loRssItem.HasSubCategories = true;
                    loRssItem.SubCategoriesDiscovered = true;
                    loRssItem.SubCategories = new List<Category>();

                    RssLink general = new RssLink();
                    general.Name = cat.Name + " (Uncategorized)";
                    general.Url = cat.Id;
                    general.EstimatedVideoCount = (uint)cat.VideoCount;
                    loRssItem.SubCategories.Add(general);
                    general.ParentCategory = loRssItem;

                    foreach(CategoryEntity subcat in cat.Childs)
                    {
                        RssLink subitem = new RssLink();
                        subitem.Name = subcat.Name;
                        subitem.Url = subcat.Id;
                        subitem.EstimatedVideoCount = (uint)subcat.VideoCount;
                        loRssItem.SubCategories.Add(subitem);
                        subitem.ParentCategory = loRssItem;
                    }
                }
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override String getUrl(VideoInfo video)
        {
            RTMP_LIB.Link link = YahooRTMPLinkCatcher(video.VideoUrl);
            string resultUrl = string.Format("http://127.0.0.1:{5}/stream.flv?app={0}&tcUrl={1}&hostname={2}&port={3}&playpath={4}",
                System.Web.HttpUtility.UrlEncode(link.app),
                System.Web.HttpUtility.UrlEncode(link.tcUrl),
                System.Web.HttpUtility.UrlEncode(link.hostname),
                link.port,
                System.Web.HttpUtility.UrlEncode(link.playpath),
                OnlineVideoSettings.RTMP_PROXY_PORT);
            return resultUrl;
        }

        public override string GetFileNameForDownload(VideoInfo video, string url)
        {
            string safeName = ImageDownloader.GetSaveFilename(video.Title);
            return safeName + ".flv";
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
            if (string.IsNullOrEmpty(format_Title))
                return vid.Artist.Name + " - " + vid.Video.Title;
            string s = format_Title;
            s = s.Replace("%title%", vid.Video.Title);
            s = s.Replace("%artist%", vid.Artist.Name);
            s = s.Replace("%year%", vid.Video.CopyrightYear.ToString());
            s = s.Replace("%rating%", vid.Video.Rating.ToString());
            s = System.Web.HttpUtility.HtmlDecode(s);
            return s;
        }

        #region Next/Previous Page

        RssLink currentCategory;
        int currentStart = 1;

        public override bool HasNextPage
        {
            get { return currentCategory != null && currentCategory.EstimatedVideoCount > currentStart; }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            currentStart += pageSize;
            return GetVideoForCurrentCategory();
        }

        public override bool HasPreviousPage
        {
            get { return currentCategory != null && currentStart > 1; }
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            currentStart -= pageSize;
            if (currentStart < 1) currentStart = 1;
            return GetVideoForCurrentCategory();
        }

        #endregion

        #region Search

        public override bool CanSearch { get { return true; } }

        public override Dictionary<string, string> GetSearchableCategories()
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

        public override List<VideoInfo> Search(string query)
        {
            return Search(query, "");    
        }

        public override List<VideoInfo> Search(string query, string category)
        {            
            videoSearchList.Category = category;
            videoSearchList.Keyword = query;
            videoSearchList.Count = 100;
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

            currentCategory = null;

            return loRssItemList;            

        }

        #endregion
    }
}
