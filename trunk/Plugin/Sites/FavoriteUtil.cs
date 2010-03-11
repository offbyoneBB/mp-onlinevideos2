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

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Description of FavoriteUtil.
    /// </summary>
    public class FavoriteUtil : SiteUtilBase
    {
        public override string getUrl(VideoInfo video)
        {
            SiteUtilBase util = OnlineVideoSettings.getInstance().SiteList[video.SiteName];
            return util.getUrl(video);
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

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            RssLink all = new RssLink();
            all.Name = "All";
            all.Url = "fav:";
            Settings.Categories.Add(all);

            FavoritesDatabase db = FavoritesDatabase.getInstance();
            string[] lsSiteIds = db.getSiteIDs();            
            foreach (string lsSiteId in lsSiteIds)
            {
                SiteSettings aSite = OnlineVideoSettings.getInstance().SiteList[lsSiteId].Settings;

                if (aSite.IsEnabled &&
                   (!aSite.ConfirmAge || !OnlineVideoSettings.getInstance().useAgeConfirmation || OnlineVideoSettings.getInstance().ageHasBeenConfirmed))
                {
                    RssLink cat = new RssLink();
                    cat.Name = aSite.Name + " - " + Translation.Favourites;
                    cat.Url = "fav:" + aSite.Name;
                    cat.Thumb = OnlineVideoSettings.getInstance().BannerIconsDir + @"Icons\" + aSite.Name + ".png";
                    Settings.Categories.Add(cat);
                }
            }

            // need to always get the categories, because when adding new fav video from a new site, a removing the last one for a site, the categories must be refreshed 
            Settings.DynamicCategoriesDiscovered = false;
            return Settings.Categories.Count;
        }        

        #region Search

        public override bool CanSearch { get { return true; } }

        public override List<VideoInfo> Search(string query)
        {
            FavoritesDatabase db = FavoritesDatabase.getInstance();
            return db.searchFavoriteVideos(query);
        }

        #endregion
    }
}
