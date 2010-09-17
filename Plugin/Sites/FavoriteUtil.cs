using System;
using System.Collections.Generic;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Description of FavoriteUtil.
    /// </summary>
    public class FavoriteUtil : SiteUtilBase
    {
        public override List<string> getMultipleVideoUrls(VideoInfo video)
        {
            SiteUtilBase util = OnlineVideoSettings.Instance.SiteUtilsList[video.SiteName];
            return util.getMultipleVideoUrls(video);
        }

        public override List<VideoInfo> getVideoList(Category category)
        {            
            return OnlineVideoSettings.Instance.FavDB.getFavoriteVideos(((RssLink)category).Url, null);
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
                all.Url = string.Empty;
                cachedCategories.Add(all.Name, all);
            }
            Settings.Categories.Add(all);

            string[] lsSiteIds = OnlineVideoSettings.Instance.FavDB.getSiteIDs();            
            foreach (string lsSiteId in lsSiteIds)
            {
                SiteUtilBase util = null;
                if (OnlineVideoSettings.Instance.SiteUtilsList.TryGetValue(lsSiteId, out util))
                {
                    SiteSettings aSite = util.Settings;
                    if (aSite.IsEnabled &&
                       (!aSite.ConfirmAge || !OnlineVideoSettings.Instance.UseAgeConfirmation || OnlineVideoSettings.Instance.AgeConfirmed))
                    {
                        RssLink cat = null;
                        if (!cachedCategories.TryGetValue(aSite.Name + " - " + Translation.Favourites, out cat))
                        {
                            cat = new RssLink();
                            cat.Name = aSite.Name + " - " + Translation.Favourites;
                            cat.Description = aSite.Description;
                            cat.Url = aSite.Name;
                            cat.Thumb = System.IO.Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, @"Icons\" + aSite.Name + ".png");
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
            return OnlineVideoSettings.Instance.FavDB.getFavoriteVideos(null, query);
        }

        #endregion

        public override List<string> GetContextMenuEntries(Category selectedCategory, VideoInfo selectedItem)
        {
            return new List<string>() { Translation.DeleteAll };
        }

        public override bool ExecuteContextMenuEntry(Category selectedCategory, VideoInfo selectedItem, string choice)
        {
            return OnlineVideoSettings.Instance.FavDB.removeAllFavoriteVideos(((RssLink)selectedCategory).Url.Substring(4));
        }
    }
}
