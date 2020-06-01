# How to contribute

As websites change constantly, keeping it all working requires quite some work, and we can't do this without the help of the community.

There are three basic parts of this plugin, the core, the MediaPortal specific parts and the parts for the sites itself.

## The core and the MediaPortal Specific parts

Contribution for this is done through pull-requests which will be reviewed by one of the core developers, and acted upon accordingly.

## Sites

### Fixing existing sites
This is also done through pull-requests.

### Adding a new site.

It depends a bit on the complexity of the site, if the structure more or less fits max. 3 levels of hierarchy (Categories, Subcategories, Videos) you start with [SiteParser](https://github.com/offbyoneBB/mp-onlinevideos2/wiki/SiteParser) and see how far you get.
If more work is needed (some special hoops to jump through to get the playable url) You will need to create a small siteutil and override the GetVideoUrl
```
namespace OnlineVideos.Sites
{
    public class MyUtil : GenericSiteUtil
    {
        public override string GetVideoUrl(VideoInfo video)
        {
            var data = GetWebData(video.VideoUrl);
            //Do magic to fill finalUrl
            return finalUrl;
        }
    }
}
```

If the site is more complex, it ususally is easier to just code it all (probably using some small stuff like baseUrl and one or two regexes from the GenericSiteUtil, build with the SiteParser)

Skeleton for that would be something like:
```
namespace OnlineVideos.Sites
{
    public class MyUtil : GenericSiteUtil
    {
        public override int DiscoverDynamicCategories()
        {
            //your code here to fill Settings.Categories
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            //Your code here to fill parentCategory.SubCategories
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            //Your code here to fill the resulting List<VideoInfo>
        }

    }
}
```

After you have tested it and approved the new website, you can publish it in the MediaPortal Configuration tool.
Select Plugins, OnlineVideos, Config, Sites. Then select your site, press Publish to Web ![Publish to Web](https://raw.githubusercontent.com/offbyoneBB/mp-onlinevideos2/master/OnlineVideos.MediaPortal1/Resources/PublishToWeb.png) and follow the instructions there.
