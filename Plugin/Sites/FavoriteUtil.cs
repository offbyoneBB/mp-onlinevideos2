using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using System.ComponentModel;
using System.Threading;
using OnlineVideos.Database;
using MediaPortal.GUI.Library;
using MediaPortal.GUI.View;
using MediaPortal.Dialogs;
using MediaPortal.Util;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Description of FavoriteUtil.
    /// </summary>
    public class FavoriteUtil : SiteUtilBase
    {
        public override string getUrl(VideoInfo video, SiteSettings foSite)
        {
            SiteSettings loSite = OnlineVideoSettings.getInstance().moSiteList[video.SiteID];
            return SiteUtilFactory.GetByName(video.SiteID).getUrl(video, loSite); ;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string fsUrl = ((RssLink)category).Url.Substring(4);
            FavoritesDatabase db = FavoritesDatabase.getInstance();
            List<VideoInfo> loVideoList;
            if (String.IsNullOrEmpty(fsUrl))
            {
                loVideoList = db.getAllFavoriteVideos();
            }
            else if (fsUrl.StartsWith("%"))
            {
                fsUrl = fsUrl.Substring(1);
                loVideoList = db.searchFavoriteVideos(fsUrl);
            }
            else
            {
                loVideoList = db.getSiteFavoriteVideos(fsUrl);
            }
            return loVideoList;
        }

        public override List<Category> getDynamicCategories()
        {
            RssLink all = new RssLink();
            all.Name = "All";
            all.Url = "fav:";
            List<Category> cats = new List<Category>();
            cats.Add(all);

            FavoritesDatabase db = FavoritesDatabase.getInstance();
            string[] lsSiteIds = db.getSiteIDs();
            Dictionary<String, SiteSettings> loSiteList = OnlineVideoSettings.getInstance().moSiteList;
            SiteSettings loSite;
            RssLink cat;
            foreach (string lsSiteId in lsSiteIds)
            {
                loSite = loSiteList[lsSiteId];
                cat = new RssLink();
                cat.Name = loSite.Name + "-Favorites";
                cat.Url = "fav:" + loSite.Name;
                cats.Add(cat);

            }
            cat = new RssLink();
            cat.Name = "Search-Favorites";
            cat.Url = "fav:%{0}";
            cats.Add(cat);

            return cats;
        }
    }
}
