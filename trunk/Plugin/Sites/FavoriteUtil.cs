using System;
using System.Collections.Generic;
using OnlineVideos.Database;
using MediaPortal.Configuration;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Description of FavoriteUtil.
    /// </summary>
    public class FavoriteUtil : SiteUtilBase
    {
        public override string getUrl(VideoInfo video)
        {
            SiteUtilBase util = OnlineVideoSettings.Instance.SiteList[video.SiteName];
            return util.getUrl(video);
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            return FavoritesDatabase.Instance.getFavoriteVideos(((RssLink)category).Url.Substring(4), null);
        }

        // keep a reference of all Categories ever created and reuse them, to get them selected when returning to the category view
        Dictionary<string, RssLink> cachedCategories = new Dictionary<string, RssLink>();

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            RssLink all = null;
            if (!cachedCategories.TryGetValue(Translation.All, out all))
            {
                all = new RssLink();
                all.Name = Translation.All;
                all.Url = "fav:";
                cachedCategories.Add(all.Name, all);
            }
            Settings.Categories.Add(all);

            string[] lsSiteIds = FavoritesDatabase.Instance.getSiteIDs();            
            foreach (string lsSiteId in lsSiteIds)
            {
                SiteUtilBase util = null;
                if (OnlineVideoSettings.Instance.SiteList.TryGetValue(lsSiteId, out util))
                {
                    SiteSettings aSite = util.Settings;
                    if (aSite.IsEnabled &&
                       (!aSite.ConfirmAge || !OnlineVideoSettings.Instance.useAgeConfirmation || OnlineVideoSettings.Instance.ageHasBeenConfirmed))
                    {
                        RssLink cat = null;
                        if (!cachedCategories.TryGetValue(aSite.Name + " - " + Translation.Favourites, out cat))
                        {
                            cat = new RssLink();
                            cat.Name = aSite.Name + " - " + Translation.Favourites;
                            cat.Url = "fav:" + aSite.Name;
                            cat.Thumb = Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\Icons\" + aSite.Name + ".png";
                            cachedCategories.Add(cat.Name, cat);
                        }
                        Settings.Categories.Add(cat);
                    }
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
            return FavoritesDatabase.Instance.getFavoriteVideos(null, query);
        }

        #endregion
    }
}
