using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites.Utils.NaviX;
using System.ComponentModel;
using System.Text.RegularExpressions;
using OnlineVideos.Sites.Utils;
using System.Xml;

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
                        cat.Other = null;
                    }
                lastUpdate = DateTime.Now;
            }
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            SubCatHolder holder = parentCategory.Other as SubCatHolder;
            if (holder != null)
            {
                if (holder.SubCategories != null && holder.SubCategories.Count > 0)
                {
                    parentCategory.SubCategories = holder.SubCategories;
                    searchableCats = holder.SearchableCategories;
                    return holder.SubCategories.Count;
                }
                NaviXMediaItem naviXItem = holder.Other as NaviXMediaItem;
                if (naviXItem != null)
                {
                    if (naviXItem.Type == "search")
                        throw new OnlineVideosException("To search specify search category and use OnlineVideos search feature");
                }
            }
            string plUrl = (parentCategory as RssLink).Url;
            holder = getCats(plUrl, parentCategory);
            parentCategory.SubCategories = holder.SubCategories;
            searchableCats = holder.SearchableCategories;
            parentCategory.Other = holder;
            return holder.SubCategories.Count;
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            SubCatHolder holder = category.ParentCategory.Other as SubCatHolder;
            if (holder == null)
                return category.ParentCategory.SubCategories.Count;

            holder.SubCategories.Remove(category);
            holder.SubCategories.AddRange(getCats((category as RssLink).Url, category.ParentCategory).SubCategories);
            category.ParentCategory.SubCategories = holder.SubCategories;
            return holder.SubCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> vids = new List<VideoInfo>();
            NaviXMediaItem item = category.Other as NaviXMediaItem;
            if (item == null)
                return vids;
            if (item.Type == "video")
            {
                VideoInfo vid = new VideoInfo();
                vid.Title = category.Name;
                vid.Description = category.Description;
                vid.ImageUrl = category.Thumb;
                vid.Airdate = item.Date;
                vid.Other = item;
                vids.Add(vid);
            }
            else if (item.Type.StartsWith("rss"))
            {
                string url = item.URL;
                if (url.StartsWith("rss://"))
                    url = "http://" + url.Substring(6);
                RssToolkit.Rss.RssDocument doc = GetWebData<RssToolkit.Rss.RssDocument>(url);
                if (doc != null)
                {
                    foreach (RssToolkit.Rss.RssItem rssItem in doc.Channel.Items)
                    {
                        VideoInfo vid = VideoInfo.FromRssItem(rssItem, true, new Predicate<string>(isPossibleVideo));
                        if (vid != null)
                            vids.Add(vid);
                    }
                }
            }
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
            foreach (Category cat in getCats(category + query).SubCategories)
                results.Add(cat);
            return results;
        }

        public override Dictionary<string, string> GetSearchableCategories()
        {
            if (searchableCats == null)
                searchableCats = new Dictionary<string, string>();
            return searchableCats;
        }

        SubCatHolder getCats(string playlistUrl, Category parentCategory = null)
        {
            SubCatHolder holder = new SubCatHolder();
            holder.SubCategories = new List<Category>();
            holder.SearchableCategories = new Dictionary<string, string>();
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
                    cat.HasSubCategories = item.Type == "playlist" || item.Type == "search";
                    cat.ParentCategory = parentCategory;
                    cat.Other = item;
                    holder.SubCategories.Add(cat);
                    if (item.Type == "search" && searchableCats != null)
                        holder.SearchableCategories[cat.Name] = cat.Url;
                }
            }
            return holder;
        }

        MPUrlSourceFilter.RtmpUrl getRTMPUrl(string naviXRTMPUrl)
        {
            MatchCollection matches = new Regex(@"\s+(tcUrl|app|playpath|swfUrl|pageUrl|swfVfy|live|timeout)\s*=\s*([^\s]*)", RegexOptions.IgnoreCase).Matches(naviXRTMPUrl);
            if (matches.Count < 1)
                return new MPUrlSourceFilter.RtmpUrl(naviXRTMPUrl);

            MPUrlSourceFilter.RtmpUrl url = new MPUrlSourceFilter.RtmpUrl(naviXRTMPUrl.Substring(0, matches[0].Index));
            foreach (Match m in matches)
            {
                string val = m.Groups[2].Value;
                switch (m.Groups[1].Value.ToLower())
                {
                    case "tcurl":
                        url.TcUrl = val;
                        break;
                    case "app":
                        url.App = val;
                        break;
                    case "playpath":
                        url.PlayPath = val;
                        break;
                    case "swfurl":
                        url.SwfUrl = val;
                        break;
                    case "pageurl":
                        url.PageUrl = val;
                        break;
                    case "swfvfy":
                        if (val == "1" || val.ToLower() == "true")
                            url.SwfVerify = true;
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
