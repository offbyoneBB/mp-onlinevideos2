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
    public class FavoriteUtil : SiteUtilBase, ISearch
    {
        public override string getUrl(VideoInfo video, SiteSettings foSite)
        {
            SiteSettings loSite = OnlineVideoSettings.getInstance().moSiteList[video.SiteName];
            return loSite.Util.getUrl(video, loSite);
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

        public override int DiscoverDynamicCategories(SiteSettings site)
        {
            site.Categories.Clear();

            RssLink all = new RssLink();
            all.Name = "All";
            all.Url = "fav:";
            site.Categories.Add(all);

            FavoritesDatabase db = FavoritesDatabase.getInstance();
            string[] lsSiteIds = db.getSiteIDs();
            Dictionary<String, SiteSettings> loSiteList = OnlineVideoSettings.getInstance().moSiteList;                        
            foreach (string lsSiteId in lsSiteIds)
            {
                SiteSettings aSite = loSiteList[lsSiteId];

                if (aSite.IsEnabled &&
                   (!aSite.ConfirmAge || !OnlineVideoSettings.getInstance().useAgeConfirmation || OnlineVideoSettings.getInstance().ageHasBeenConfirmed))
                {
                    RssLink cat = new RssLink();
                    cat.Name = aSite.Name + " - Favorites";
                    cat.Url = "fav:" + aSite.Name;
                    site.Categories.Add(cat);
                }
            }
            /*
            RssLink cat = new RssLink();
            cat.Name = "Search-Favorites";
            cat.Url = "fav:%{0}";
            site.Categories.Add(cat);*/

            // need to always get the categories, because when adding new fav video from a new site, a removing the last one for a site, the categories must be refreshed 
            site.DynamicCategoriesDiscovered = false; 
            return site.Categories.Count;
        }

        public override bool RemoveFavorite(VideoInfo foVideo)
        {
            bool result = base.RemoveFavorite(foVideo);
            if (result) OnlineVideoSettings.getInstance().moSiteList["Favorites"].DynamicCategoriesDiscovered = false;
            return result;
        }

        #region ISearch Member

        public Dictionary<string, string> GetSearchableCategories(IList<Category> configuredCategories)
        {
            return new Dictionary<string, string>();
        }

        public List<VideoInfo> Search(string searchUrl, string query)
        {
            FavoritesDatabase db = FavoritesDatabase.getInstance();
            return db.searchFavoriteVideos(query);
        }

        public List<VideoInfo> Search(string searchUrl, string query, string category)
        {
            return Search(searchUrl, query);
        }

        #endregion
    }
}
