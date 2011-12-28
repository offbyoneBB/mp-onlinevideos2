
namespace OnlineVideos.Sites
{
    public class TheEscapistUtil : GenericSiteUtil
    {
        public override int DiscoverDynamicCategories()
        {
            base.DiscoverDynamicCategories();
            foreach (Category cat in Settings.Categories)
                cat.HasSubCategories = (((RssLink)cat).Url.EndsWith(@"videos/galleries"));
            return Settings.Categories.Count;
        }
    }
}
