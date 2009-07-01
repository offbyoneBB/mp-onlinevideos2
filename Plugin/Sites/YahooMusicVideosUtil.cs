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

        public override List<Category> getDynamicCategories()
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

            if (catserv == null)
            {
                catserv = new CategoryTreeService();
                catserv.Type = CategoryTreeTypes.genre;
                provider.GetData(catserv);                
            }

            List<Category> loRssItems = new List<Category>();

            RssLink popularItem = new RssLink();
            popularItem.Name = "Popular";
            popularItem.Url = "popular";
            loRssItems.Add(popularItem);

            RssLink newItem = new RssLink();
            newItem.Name = "New";
            newItem.Url = "new";
            loRssItems.Add(newItem);

            foreach (CategoryEntity cat in catserv.Items)
            {
                RssLink loRssItem = new RssLink();
                loRssItem.Name = cat.Name;
                loRssItem.Url = cat.Id;                
                loRssItems.Add(loRssItem);
            }            
            
            return loRssItems;
        }

        public override String getUrl(VideoInfo video, SiteSettings foSite)
        {            
            VideoPlayer player = new VideoPlayer(video.VideoUrl);
            player.AutoStart = true;
            if (settings.Bandwith != 0) player.Bandwidth = settings.Bandwith;
            player.Ympsc = provider.Locale.Ympsc;
            player.EID = provider.Locale.EID;
            player.VideoIds.Clear();            
            return player.VideoPlayerUrl;
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
            return s;
        }


        #region ISearch Member

        public Dictionary<string, string> GetSearchableCategories(Category[] configuredCategories)
        {
            Dictionary<string, string> loRssItems = new Dictionary<string, string>();

            foreach (CategoryEntity cat in catserv.Items)
            {
                loRssItems.Add(cat.Name, cat.Id);
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
