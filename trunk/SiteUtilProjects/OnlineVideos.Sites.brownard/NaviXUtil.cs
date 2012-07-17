using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites.Utils.NaviX;
using System.ComponentModel;
using System.Text.RegularExpressions;

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
            parentCategory.SubCategories.AddRange(getCats(plUrl, parentCategory));
            if (parentCategory.SubCategories.Count > 0)
                parentCategory.SubCategoriesDiscovered = true;

            return parentCategory.SubCategories.Count;
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            category.ParentCategory.SubCategories.Remove(category);
            category.ParentCategory.SubCategories.AddRange(getCats((category as RssLink).Url, category.ParentCategory));
            return category.ParentCategory.SubCategories.Count;
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
            if (item == null)
                return video.VideoUrl;
            string urlStr;
            if (item.Type == "video" && !string.IsNullOrEmpty(item.Processor))
            {
                NaviXProcessor proc = new NaviXProcessor(item);
                if (proc.Process())
                    urlStr = proc.Data;
                else
                    return "";
            }
            else
                urlStr = item.URL;
            
            if (urlStr.ToLower().StartsWith("rtmp"))
            {
                MPUrlSourceFilter.RtmpUrl url = getRTMPUrl(urlStr);
                return url.ToString();
            }

            if (item.Player == "default")
                Settings.Player = PlayerType.Internal;
            else
                Settings.Player = PlayerType.Auto;
            return urlStr;
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

        List<Category> getCats(string playlistUrl, Category parentCategory = null)
        {
            List<Category> cats = new List<Category>();
            NaviXPlaylist pl = new NaviXPlaylist(playlistUrl);
            if (pl.Ready)
            {
                foreach (NaviXMediaItem item in pl.Items)
                {
                    if (string.IsNullOrEmpty(item.Type))
                        continue;
                    RssLink cat;
                    if (System.Text.RegularExpressions.Regex.IsMatch(item.URL, @"[?&]page=\d+"))
                        cat = new NextPageCategory();
                    else
                        cat = new RssLink();
                    cat.Name = System.Text.RegularExpressions.Regex.Replace(item.Name, @"\[/?COLOR[^\]]*\]", "");
                    if (!string.IsNullOrEmpty(item.InfoTag))
                        cat.Name += string.Format(" ({0})", item.InfoTag);
                    cat.Description = string.IsNullOrEmpty(item.Description) ? pl.Description : item.Description;
                    cat.Url = item.URL;
                    if (!string.IsNullOrEmpty(item.Thumb))
                        cat.Thumb = item.Thumb;
                    else if (!string.IsNullOrEmpty(item.Icon))
                        cat.Thumb = item.Icon;
                    else
                        cat.Thumb = pl.Logo;
                    cat.Other = item;
                    cat.HasSubCategories = item.Type == "playlist" || item.Type == "search";
                    cat.ParentCategory = parentCategory;
                    cats.Add(cat);
                    if (item.Type == "search" && searchableCats != null)
                        searchableCats[cat.Name] = cat.Url;
                }
            }
            return cats;
        }

        MPUrlSourceFilter.RtmpUrl getRTMPUrl(string naviXRTMPUrl)
        {
            MatchCollection matches = new Regex(@"\s+(tcUrl|app|playpath|swfUrl|pageUrl|swfVfy|live)\s*=\s*([^\s]*)").Matches(naviXRTMPUrl);
            if (matches.Count < 1)
                return new MPUrlSourceFilter.RtmpUrl(naviXRTMPUrl);

            MPUrlSourceFilter.RtmpUrl url = new MPUrlSourceFilter.RtmpUrl(naviXRTMPUrl.Substring(0, matches[0].Index));
            foreach (Match m in matches)
            {
                string val = m.Groups[2].Value;
                switch (m.Groups[1].Value)
                {
                    case "tcUrl":
                        url.TcUrl = val;
                        break;
                    case "app":
                        url.App = val;
                        break;
                    case "playpath":
                        url.PlayPath = val;
                        break;
                    case "swfUrl":
                        url.SwfUrl = val;
                        break;
                    case "pageUrl":
                        url.PageUrl = val;
                        break;
                    case "swfVfy":
                        url.SwfUrl = val;
                        break;
                    case "live":
                        if (val == "1" || val.ToLower() == "true")
                            url.Live = true;
                        break;
                }
            }
            return url;
        }
    }
}
