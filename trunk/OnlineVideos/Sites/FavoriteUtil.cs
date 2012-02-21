using System;
using System.Linq;
using System.Collections.Generic;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Description of FavoriteUtil.
    /// </summary>
    public class FavoriteUtil : SiteUtilBase
    {
        public class FavoriteCategory : Category
        {
            public SiteUtilBase FavSite { get; protected set; }
            public SiteUtilBase Site { get; protected set; }
            public Category SiteCategory { get; protected set; }

            public FavoriteCategory(RssLink favCat, SiteUtilBase util, SiteUtilBase favUtil)
            {
                this.Site = util;
                FavSite = favUtil;
                Other = favCat;
                Name = favCat.Name;
                Description = favCat.Description;
                Thumb = favCat.Thumb;
            }

            public void DiscoverSiteCategory()
            {
                string[] hierarchy = ((string)(Other as RssLink).Other).Split('|');
                for (int i = 0; i < hierarchy.Length; i++)
                {
                    if (SiteCategory != null)
                    {
                        if (!SiteCategory.SubCategoriesDiscovered) Site.DiscoverSubCategories(SiteCategory);
                        Category foundCat = SiteCategory.SubCategories.FirstOrDefault(c => c.Name == hierarchy[i]);
                        // nextpage until found or no more
                        while (foundCat == null && SiteCategory.SubCategories.Last() is NextPageCategory)
                        {
                            Site.DiscoverNextPageCategories(SiteCategory.SubCategories.Last() as NextPageCategory);
                            foundCat = SiteCategory.SubCategories.FirstOrDefault(c => c.Name == hierarchy[i]);
                        }
                        SiteCategory = foundCat;
                    }
                    else
                    {
                        if (!Site.Settings.DynamicCategoriesDiscovered) Site.DiscoverDynamicCategories();
                        Category foundCat = Site.Settings.Categories.FirstOrDefault(c => c.Name == hierarchy[i]);
                        // nextpage until found or no more
                        while (foundCat == null && Site.Settings.Categories.Last() is NextPageCategory)
                        {
                            Site.DiscoverNextPageCategories(Site.Settings.Categories.Last() as NextPageCategory);
                            foundCat = Site.Settings.Categories.FirstOrDefault(c => c.Name == hierarchy[i]);
                        }
                        SiteCategory = foundCat;
                    }
                    if (SiteCategory == null) break;
                }
            }
        }

        public override List<String> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            SiteUtilBase util = OnlineVideoSettings.Instance.SiteUtilsList[video.SiteName];
            return util.getMultipleVideoUrls(video, inPlaylist);
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            if (category is RssLink)
            {
                return OnlineVideoSettings.Instance.FavDB.getFavoriteVideos(((RssLink)category).Url, null);
            }
            return null;
        }

        // keep a reference of all Categories ever created and reuse them, to get them selected when returning to the category view
        Dictionary<string, RssLink> cachedCategories = new Dictionary<string, RssLink>();

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            List<KeyValuePair<string, uint>> lsSiteIds = OnlineVideoSettings.Instance.FavDB.getSiteIDs();

            if (lsSiteIds == null || lsSiteIds.Count == 0) return 0;

            uint sumOfAllVideos = (uint)lsSiteIds.Sum(s => s.Value);

            if (sumOfAllVideos > 0)  // only add the "ALL" category if we have single favorite videos in addition to favorites categories
            {
                RssLink all = null;
                if (!cachedCategories.TryGetValue(Translation.Instance.All, out all))
                {
                    all = new RssLink();
                    all.Name = Translation.Instance.All;
                    all.Url = string.Empty;
                    cachedCategories.Add(all.Name, all);
                }
                all.EstimatedVideoCount = sumOfAllVideos;
                Settings.Categories.Add(all);
            }

            foreach (var lsSiteId in lsSiteIds)
            {
                SiteUtilBase util = null;
                if (OnlineVideoSettings.Instance.SiteUtilsList.TryGetValue(lsSiteId.Key, out util))
                {
                    SiteSettings aSite = util.Settings;
                    if (aSite.IsEnabled &&
                       (!aSite.ConfirmAge || !OnlineVideoSettings.Instance.UseAgeConfirmation || OnlineVideoSettings.Instance.AgeConfirmed))
                    {
                        RssLink cat = null;
                        if (!cachedCategories.TryGetValue(aSite.Name + " - " + Translation.Instance.Favourites, out cat))
                        {
                            cat = new RssLink();
                            cat.Name = aSite.Name + " - " + Translation.Instance.Favourites;
                            cat.Description = aSite.Description;
                            cat.Url = aSite.Name;
                            cat.Thumb = System.IO.Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, @"Icons\" + aSite.Name + ".png");
                            cachedCategories.Add(cat.Name, cat);
                        }
                        cat.EstimatedVideoCount = lsSiteId.Value;
                        Settings.Categories.Add(cat);

                        // create subcategories if any
                        List<Category> favCats = OnlineVideoSettings.Instance.FavDB.getFavoriteCategories(aSite.Name);
                        if (favCats.Count > 0)
                        {
                            cat.HasSubCategories = true;
                            cat.SubCategoriesDiscovered = true;
                            cat.SubCategories = new List<Category>();
                            if (lsSiteId.Value > 0) // only add the "ALL" category if we have single favorite videos in addition to favorites categories
                            {
                                cat.SubCategories.Add(new RssLink() { Name = Translation.Instance.All, Url = aSite.Name, ParentCategory = cat, EstimatedVideoCount = lsSiteId.Value });
                            }
                            foreach (Category favCat in favCats)
                            {
                                FavoriteCategory fc = new FavoriteCategory(favCat as RssLink, util, this) { ParentCategory = cat };
                                if (String.IsNullOrEmpty(fc.Description))
                                {
                                    string[] parts = ((string)(fc.Other as RssLink).Other).Split('|');
                                    if (parts.Length > 1)
                                        fc.Description = String.Join(" / ", parts, 0, parts.Length - 1);
                                }

                                cat.SubCategories.Add(fc);
                            }
                        }
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

        #region Context Menu

        public override List<string> GetContextMenuEntries(Category selectedCategory, VideoInfo selectedItem)
        {
            List<string> result = new List<string>();
            if (selectedCategory is FavoriteCategory)
            {
                if (selectedItem == null) result.Add(Translation.Instance.RemoveFromFavorites);
            }
            else if (selectedItem != null)
            {
                result.Add(Translation.Instance.RemoveFromFavorites);
                result.Add(Translation.Instance.DeleteAll);
            }
            return result;
        }

        public override bool ExecuteContextMenuEntry(Category selectedCategory, VideoInfo selectedItem, string choice, out List<ISearchResultItem> newVideos)
        {
            newVideos = null;
            bool result = false;
            if (choice == Translation.Instance.DeleteAll)
            {
                result = OnlineVideoSettings.Instance.FavDB.removeAllFavoriteVideos(((RssLink)selectedCategory).Url);
                // we have to manually refresh the categories
                if (result && selectedCategory.ParentCategory != null) DiscoverDynamicCategories();
            }
            else if (choice == Translation.Instance.RemoveFromFavorites)
            {
                if (selectedCategory is FavoriteCategory)
                {
                    result = OnlineVideoSettings.Instance.FavDB.removeFavoriteCategory(((FavoriteCategory)selectedCategory).Other as Category);
                    if (result) selectedCategory.ParentCategory.SubCategories.Remove(selectedCategory);
                    return result;
                }
                else
                {
                    result = OnlineVideoSettings.Instance.FavDB.removeFavoriteVideo(selectedItem);
                }
                // we have to manually refresh the categories
                if (result && selectedCategory.ParentCategory != null) DiscoverDynamicCategories();
            }
            return result;
        }

        #endregion
    }
}
