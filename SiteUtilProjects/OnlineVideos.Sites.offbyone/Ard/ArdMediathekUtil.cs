using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;

using OnlineVideos.Helpers;
using OnlineVideos.Sites.Ard;


namespace OnlineVideos.Sites
{
    public class ArdConstants
    {

        public static Uri API_URL { get; } = new Uri("https://api.ardmediathek.de");
        public static string ITEM_URL { get; } = API_URL + "/page-gateway/pages/ard/item/";
        public static string DAY_PAGE_URL { get; } = API_URL + "/page-gateway/compilations/{0}/pastbroadcasts?startDateTime={1}T00:00:00.000Z&endDateTime={2}T23:59:59.000Z&pageNumber=0&pageSize={3}";

        public static int DAY_PAGE_SIZE { get; } = 100;
    }

    public class ArdMediathekUtil : SiteUtilBase
    {
        public static readonly string PLACEHOLDER_IMAGE_WIDTH = "{width}";
        public static readonly string IMAGE_WIDTH = "1024";

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            Settings.Categories.Add(ArdPageFactory.CreateCategory<ArdLiveStreamsDeserializer>());

            Settings.Categories.Add(ArdPageFactory.CreateCategory<ArdTopicsPageDeserializer>());
            Settings.Categories.Add(ArdPageFactory.CreateCategory<ArdDayPageDeserializer>());


            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public delegate ArdCategoryInfoDto GetCategories(JToken json);

        public override int DiscoverSubCategories(Category selectedCategory)
        {

            if (selectedCategory is RssLink { Other: Context context } rssCategory)
            {
                var subCategories = new List<Category>();
                var page = context.Page;
                var result = page.GetCategories(rssCategory.Url, context.Token);
                foreach (var item in result.Value)
                {
                    var subCategory = item.AsRssLink(selectedCategory, page, result.ContinuationToken);
                    subCategories.Add(subCategory);
                }
                selectedCategory.SubCategories = subCategories;
            }

            return selectedCategory.SubCategories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            var list = new List<VideoInfo>();

            if (category is RssLink rssCategory && !string.IsNullOrWhiteSpace(rssCategory.Url) && rssCategory.Other is Context context)
            {
                var page = context.Page;
                var result = page.GetVideos(rssCategory.Url, context.Token);
                foreach (var filmInfoDto in result.Value)
                {
                    list.Add(filmInfoDto.AsVideoInfo(page, result.ContinuationToken));
                }
            }

            return list;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            if (video.Other is Context context)
            {
                var page = context.Page;
                var result = page.GetStreams(video.VideoUrl, context.Token);


                var orderedPlaybackOptions = result.Value.OrderBy(i => (int)i.Quality)
                    .ToLookup(s => s.Quality)
                    .ToDictionary(i => i.Key, i => i.Select(x => x.Url));
                video.PlaybackOptions = orderedPlaybackOptions?.ToDictionary(e => e.Key.ToString(), e => e.Value.First());


                var streamUrl = video.PlaybackOptions.FirstOrDefault().Value;

                if (streamUrl?.Contains("master.m3u8") ?? false)
                {
                    var m3u8Data = GetWebData<string>(streamUrl, cache: false);
                    var m3u8PlaybackOptions = HlsPlaylistParser.GetPlaybackOptions(m3u8Data, streamUrl);
                    video.PlaybackOptions = m3u8PlaybackOptions;
                    streamUrl = video.PlaybackOptions.FirstOrDefault().Value;
                }
                return streamUrl;
            }

            return video.PlaybackOptions.FirstOrDefault().Value;
        }
    }

    class Context
    {
        public Context(PageDeserializerBase page, ContinuationToken token)
        {
            Page = page;
            Token = token;
        }

        public PageDeserializerBase Page { get; set; }
        public ContinuationToken Token { get; set; }
    }


    internal static class ArdInformationDtoExtensions
    {
        public static RssLink AsRssLink(this ArdCategoryInfoDto item, Category parentCategory, PageDeserializerBase page, ContinuationToken token)
        {
            return AsRssLink(item, parentCategory, new Context(page, token));
        }

        public static RssLink AsRssLink(ArdCategoryInfoDto item, Category parentCategory, Context context)
        {
            return new RssLink()
                   {
                       Name = item.Title,
                       Description = item.Description,
                       Thumb = item.ImageUrl,
                       Url = item.TargetUrl,
                       ParentCategory = parentCategory,
                       HasSubCategories = item.HasSubCategories,
                       SubCategories = new List<Category>(),
                       Other = context
                   };
        }


        public static VideoInfo AsVideoInfo(this ArdFilmInfoDto item, PageDeserializerBase page, ContinuationToken token)
        {
            return AsVideoInfo(item, new Context(page, token));
        }


        public static VideoInfo AsVideoInfo(this ArdFilmInfoDto item, Context context)
        {
            return new VideoInfo
                   {
                       Title = item.Title,
                       Description = item.Description ?? string.Empty,
                       //Length = length.TotalMinutes <= 60 ? length.TotalMinutes.ToString() + " min" : length.ToString("h\\h\\ m\\ \\m\\i\\n"),
                       Thumb = item.ImageUrl,
                       Airdate = item.AirDate?.ToLocalTime().ToString("g", OnlineVideoSettings.Instance.Locale) ?? string.Empty,
                       VideoUrl = item.TargetUrl,
                       Other = context
                   };
        }
    }


    internal static class ArdPageFactory
    {
        public static Category CreateCategory<T>() where T : PageDeserializerBase//, new()
        {
            //var page = new T();
            var page = Activator.CreateInstance(typeof(T), WebCache.Instance) as T;
            return page.RootCategory.AsRssLink(null, page, default);
        }
    }

}
