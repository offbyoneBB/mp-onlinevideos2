using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites.Utils.NaviX;
using System.ComponentModel;

namespace OnlineVideos.Sites
{
    public class NaviXUtil : SiteUtilBase
    {
        Dictionary<string, string> searchableCats = null;

        
        DateTime lastUpdate = DateTime.MinValue;
        public override int DiscoverDynamicCategories()
        {
            if (DateTime.Now.Subtract(lastUpdate).TotalMinutes > 15)
            {
                foreach (Category cat in Settings.Categories)
                    if (cat is RssLink)
                    {
                        cat.HasSubCategories = true;
                        cat.SubCategoriesDiscovered = false;
                    }
                lastUpdate = DateTime.Now;
            }
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            NaviXMediaItem naviXItem = parentCategory.Other as NaviXMediaItem;
            if (naviXItem != null)
            {
                if (naviXItem.Type == "search")
                    throw new OnlineVideosException("To search specify search category and use OnlineVideos search feature");
            }

            string plUrl = (parentCategory as RssLink).Url;
            searchableCats = new Dictionary<string, string>();
            parentCategory.SubCategories = new List<Category>();

            bool isSort = false;
            if (parentCategory.ParentCategory != null)
            {
                NaviXMediaItem parentItem = parentCategory.ParentCategory.Other as NaviXMediaItem;
                isSort = parentItem != null && parentItem.Type == "sort";
            }

            List<Category> subCats = getCats(plUrl, parentCategory, isSort);
            foreach (Category subCat in subCats)
            {
                NaviXMediaItem subcatItem = subCat.Other as NaviXMediaItem;
                if (subcatItem.Type == "search")
                    searchableCats[subCat.Name] = subcatItem.URL;
                parentCategory.SubCategories.Add(subCat);
            }
            if (parentCategory.SubCategories.Count > 0)
                parentCategory.SubCategoriesDiscovered = true;

            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> vids = new List<VideoInfo>();
            VideoInfo vid = new VideoInfo();
            vid.Title = category.Name;
            vid.Description = category.Description;
            vid.ImageUrl = category.Thumb;
            vid.Other = category.Other;
            vids.Add(vid);
            return vids;
        }

        public override string getUrl(VideoInfo video)
        {
            NaviXMediaItem item = video.Other as NaviXMediaItem;
            string urlStr;
            if (item.Type == "video" && !string.IsNullOrEmpty(item.Processor))
            {
                NaviXProcessor proc = new NaviXProcessor(item);
                if (proc.ReturnCode == 0)
                {
                    urlStr = proc.Data;
                }
                else
                    return "";
            }
            else
                urlStr = item.URL;
            
            if (urlStr.ToLower().StartsWith("rtmp"))
            {
                MPUrlSourceFilter.RtmpUrl url = NaviXRTMP.GetRTMPUrl(urlStr);
                return url.ToString();
            }

            if (item.Player == "default")
                Settings.Player = PlayerType.Internal;
            else
                Settings.Player = PlayerType.Auto;
            return urlStr;
            //return new MPUrlSourceFilter.HttpUrl(urlStr) { UserAgent = OnlineVideoSettings.Instance.UserAgent }.ToString();
        }

        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<ISearchResultItem> DoSearch(string query, string category)
        {
            List<ISearchResultItem> results = new List<ISearchResultItem>();
            foreach (Category cat in getCats(category + query))
                results.Add(cat);
            return results;
        }

        public override Dictionary<string, string> GetSearchableCategories()
        {
            return searchableCats;
        } 

        List<Category> getCats(string playlistUrl, Category parentCategory = null, bool isSort = false)
        {
            List<Category> cats = new List<Category>();
            NaviXPlaylist pl = new NaviXPlaylist(playlistUrl);
            if (pl.Ready)
            {
                foreach (NaviXMediaItem item in pl.Items)
                {
                    if (string.IsNullOrEmpty(item.Type))
                        continue;
                    RssLink cat = new RssLink();
                    cat.Name = System.Text.RegularExpressions.Regex.Replace(item.Name, @"\[/?COLOR[^\]]*\]", "");
                    cat.Description = item.Description;
                    cat.Url = item.URL;
                    cat.Thumb = item.Thumb != null ? item.Thumb : item.Icon;
                    cat.Other = item;
                    cat.HasSubCategories = item.Type == "playlist" || item.Type == "search";
                    if (new Uri(item.URL).AbsolutePath == new Uri(playlistUrl).AbsolutePath)
                        item.Type = "sort";

                    if (isSort && parentCategory != null && parentCategory.ParentCategory != null)
                        cat.ParentCategory = parentCategory.ParentCategory.ParentCategory; //skip all sort categories on back
                    else
                        cat.ParentCategory = parentCategory;

                    cats.Add(cat);
                }
            }
            return cats;
        }
    }
}
