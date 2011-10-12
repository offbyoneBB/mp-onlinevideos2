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
    /// API Documentation at : http://www.yahooapis.com/music/
    /// </summary>
    public class YahooMusicVideosUtil : SiteUtilBase
    {
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
        
        [Category("OnlineVideosUserConfiguration"), Description("Language (Country) to use when accessing this service.")]
        Locale locale = Locale.us;
        [Category("OnlineVideosUserConfiguration"), Description("How each video title is displayed. These tags will be replaced: %artist% %title% %year% %rating%.")]
        string format_Title = "%artist% - %title% (%year%)";
        [Category("OnlineVideosUserConfiguration"), Description("Defines number of videos to display per page.")]
        int pageSize = 26;

        [Category("OnlineVideosConfiguration"), Description("Format string applied to the video id to create an url for getting the playlist file.")]
        protected string videoUrlFormatString = "http://cosmos.bcst.yahoo.com/up/yep/process/getPlaylistFOP.php?node_id=v{0}&tech=flash&mode=playlist&lg=R0xx6idZnW2zlrKP8xxAIR&bitrate=700&eventid=1301797";
                
        CategoryTreeService catserv;
        ServiceProvider provider;
        VideosForACategoryService videoInCatList = new VideosForACategoryService();
        SearchForVideosService videoSearchList = new SearchForVideosService();
        VideosForGivenPublishedListService videoPublishedList = new VideosForGivenPublishedListService();
        SimilarVideosService similarVideosServ = new SimilarVideosService();

        List<VideoInfo> GetVideoForCurrentCategory()        
        {
            currentVideosTitle = null;

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
                        videoList.Add(GetVideoInfoFromVideoResponse(vi));
                    }
                }
            }
            else if (currentCategory.Name == "similar")
            {
                similarVideosServ.Id = id;
                similarVideosServ.Start = currentStart;
                similarVideosServ.Count = pageSize;
                provider.GetData(similarVideosServ);
                foreach (VideoResponse vi in similarVideosServ.Items)
                {
                    if (!string.IsNullOrEmpty(vi.Video.Id))
                    {
                        videoList.Add(GetVideoInfoFromVideoResponse(vi));
                    }
                }
            }
            else
            {
                videoInCatList.Category = id;
                videoInCatList.Start = currentStart;
                videoInCatList.Count = pageSize;
                provider.GetData(videoInCatList);

                foreach (VideoResponse vi in videoInCatList.Items)
                {
                    if (!string.IsNullOrEmpty(vi.Video.Id))
                    {
                        videoList.Add(GetVideoInfoFromVideoResponse(vi));
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
                throw new OnlineVideosException(provider.ErrorMessage);
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

            Dictionary<string, RssLink> ids = new Dictionary<string,RssLink>();

            foreach (CategoryEntity cat in catserv.Items)
            {
                RssLink loRssItem = new RssLink();
                loRssItem.Name = cat.Name;                
                loRssItem.Url = cat.Id;
                loRssItem.EstimatedVideoCount = (uint)cat.VideoCount;
                Settings.Categories.Add(loRssItem);

                ids.Add(cat.Id, loRssItem);

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

                        ids.Add(subcat.Id, subitem);
                    }
                }
            }

            string[] idsOnly = new string[ids.Count];
            ids.Keys.CopyTo(idsOnly, 0);
            CategoryByIdService catIdServ = new CategoryByIdService();
            catIdServ.ID = string.Join(",", idsOnly);
            catIdServ.Params.Add("response", "shortdesc");
            provider.GetData(catIdServ);
            foreach (CategoryEntity catEnt in catIdServ.Items)
            {
                ids[catEnt.Id].Description = catEnt.ShortDescription;
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override String getUrl(VideoInfo video)
        {
            RTMP_LIB.Link link = YahooRTMPLinkCatcher(video.VideoUrl);
            string resultUrl = ReverseProxy.Instance.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance, 
                string.Format("http://127.0.0.1/stream.flv?app={0}&tcUrl={1}&hostname={2}&port={3}&playpath={4}",
                    System.Web.HttpUtility.UrlEncode(link.app),
                    System.Web.HttpUtility.UrlEncode(link.tcUrl),
                    System.Web.HttpUtility.UrlEncode(link.hostname),
                    link.port,
                    System.Web.HttpUtility.UrlEncode(link.playpath)));
            return resultUrl;
        }
        
        RTMP_LIB.Link YahooRTMPLinkCatcher(string videoId)
        {
            RTMP_LIB.Link link = new RTMP_LIB.Link();

            string url = string.Format(videoUrlFormatString, videoId);
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
                    loRssItemList.Add(GetVideoInfoFromVideoResponse(vi));
                }
            }

            currentCategory = null;

            return loRssItemList;            

        }

        #endregion

        #region Related

        string currentVideosTitle = null;
        public override string getCurrentVideosTitle()
        {
            return currentVideosTitle;
        }

        public override List<string> GetContextMenuEntries(Category selectedCategory, VideoInfo selectedItem)
        {
            List<string> result = new List<string>();
            if (selectedItem != null)
            {
				result.Add(Translation.Instance.RelatedVideos);
            }
            return result;
        }

        public override bool ExecuteContextMenuEntry(Category selectedCategory, VideoInfo selectedItem, string choice, out List<ISearchResultItem> newVideos)
        {
            newVideos = null;
			if (choice == Translation.Instance.RelatedVideos)
            {
                RssLink rememberedCategory = currentCategory;

                currentCategory = new RssLink() { Url = selectedItem.VideoUrl, Name = "similar" };
                currentStart = 1;
                newVideos = GetVideoForCurrentCategory().ConvertAll<ISearchResultItem>(v => v as ISearchResultItem);

                if (newVideos.Count == 0)
                {
                    currentCategory = rememberedCategory;
					throw new OnlineVideosException(Translation.Instance.NoVideoFound);
                }
                else
                {
					currentVideosTitle = Translation.Instance.RelatedVideos + " [" + selectedItem.Title + "]";
                }
            }
            return false;
        }

        #endregion

        VideoInfo GetVideoInfoFromVideoResponse(VideoResponse vi)
        {
            VideoInfo videoInfo = new VideoInfo();
            videoInfo.Title = FormatTitle(vi);
            videoInfo.Length = vi.Video.Duration.ToString();
            videoInfo.Airdate = vi.Video.CopyrightYear.ToString();
            videoInfo.VideoUrl = vi.Video.Id;
            videoInfo.ImageUrl = new Uri(vi.Image.Url).GetLeftPart(UriPartial.Path);
            List<string> albums = new List<string>();
            foreach (var album in vi.Albums) albums.Add(string.Format("{0} ({1})", album.Release.Title, album.Release.Year));
            videoInfo.Description = string.Join("\n", albums.ToArray());
            return videoInfo;
        }
    }    
}