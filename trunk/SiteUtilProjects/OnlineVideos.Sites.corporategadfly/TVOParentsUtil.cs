using System;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Site Utility for tvoparents.com.
    /// </summary>
    public class TVOParentsUtil : TVOUtil
    {
        protected override string baseUrlPrefix { get { return @"http://tvoparents.tvo.org"; } }
        
        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            Settings.Categories.Add(
                new RssLink() { Name = "School & Learning", Url = string.Format(mainCategoriesUrl, baseUrlPrefix, "9"), HasSubCategories = true }
               );
            Settings.Categories.Add(
                new RssLink() { Name = "Health & Development", Url = string.Format(mainCategoriesUrl, baseUrlPrefix, "10"), HasSubCategories = true }
               );
            Settings.Categories.Add(
                new RssLink() { Name = "Social & Emotional", Url = string.Format(mainCategoriesUrl, baseUrlPrefix, "11"), HasSubCategories = true }
               );
            Settings.Categories.Add(
                new RssLink() { Name = "Ages", Url = string.Format(mainCategoriesUrl, baseUrlPrefix, "12"), HasSubCategories = true }
               );

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }
    }
}
