using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using OnlineVideos.Helpers;


namespace OnlineVideos.Sites.Ard
{
    public class ArdConstants
    {

        public static Uri API_URL { get; } = new Uri("https://api.ardmediathek.de");
        public static string ITEM_URL { get; } = API_URL + "/page-gateway/pages/ard/item/";
        public static int DAY_PAGE_SIZE { get; } = 100;
    }

    public class ArdMediathekUtil : SiteUtilBase
    {
        public static readonly string PLACEHOLDER_IMAGE_WIDTH = "{width}";
        public static readonly string IMAGE_WIDTH = "1024";

        static ArdMediathekUtil()
        {
            //TODO Workaround
            //ServicePointManager.Expect100Continue = false;
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.DefaultConnectionLimit = 10;
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            Settings.Categories.Add(ArdPageFactory.CreateCategory<ArdLiveStreamsDeserializer>());

            Settings.Categories.Add(ArdPageFactory.CreateCategory<ArdTopicsPageDeserializer>());
            Settings.Categories.Add(ArdPageFactory.CreateCategory<ArdDayPageDeserializer>());

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category selectedCategory)
        {
            Log.Debug(nameof(DiscoverSubCategories));
            if (selectedCategory is RssLink { Other: Context context } rssCategory)
            {
                Log.Debug($"Category: {selectedCategory.Name} (HasSubCat={selectedCategory.HasSubCategories}, SubCategoriesDiscovered={selectedCategory.SubCategoriesDiscovered})");
                var subCategories = new List<Category>();
                var page = context.Page;
                var result = page.GetCategories(rssCategory.Url, context.Token);
                foreach (var item in result.Value)
                {
                    var subCategory = item.AsRssLink(selectedCategory, page, result.ContinuationToken);
                    subCategories.Add(subCategory);
                }
                selectedCategory.SubCategories = subCategories;
                selectedCategory.SubCategoriesDiscovered = true;
                Log.Debug($"SubCategories Discovered: {string.Join(",", selectedCategory.SubCategories.Select(c => c.Name))}");
            }

            return selectedCategory.SubCategories.Count;
        }

        public override List<VideoInfo> GetVideos(Category selectedCategory)
        {
            Log.Debug(nameof(GetVideos));
            Log.Debug($"Category: {selectedCategory.Name} (HasSubCat={selectedCategory.HasSubCategories}, SubCategoriesDiscovered={selectedCategory.SubCategoriesDiscovered})");

            var list = new List<VideoInfo>();

            if (selectedCategory is RssLink { Other: Context context } rssCategory)
            {
                var page = context.Page;
                var result = page.GetVideos(rssCategory.Url, context.Token);
                foreach (var filmInfoDto in result.Value)
                {
                    list.Add(filmInfoDto.AsVideoInfo(page, result.ContinuationToken, skipPlaybackOptionsDialog: _skipPlayackOptionsDialog));
                }
            }
            return list;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            Log.Debug(nameof(GetVideoUrl));

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


        private bool _skipPlayackOptionsDialog = false;
        /// <inheritdoc />
        public override List<ContextMenuEntry> GetContextMenuEntries(Category selectedCategory, VideoInfo selectedItem)
        {
            var ctxMenu = new ContextMenuEntry()
                          {
                              DisplayText = _skipPlayackOptionsDialog ? "Manual select Playback Stream" : "Directly select Playback Stream",
                              Action = ContextMenuEntry.UIAction.Execute,

                          };
            return new List<ContextMenuEntry>() { ctxMenu };
        }


        /// <inheritdoc />
        public override ContextMenuExecutionResult ExecuteContextMenuEntry(Category selectedCategory, VideoInfo selectedItem, ContextMenuEntry choice)
        {
            _skipPlayackOptionsDialog = !_skipPlayackOptionsDialog;
            Log.Debug($"_skipPlayackOptionsDialog={_skipPlayackOptionsDialog}");
            return null;
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


        public static VideoInfo AsVideoInfo(this ArdVideoInfoDto item, PageDeserializerBase page, ContinuationToken token, bool skipPlaybackOptionsDialog = false)
        {
            return AsVideoInfo(item, new Context(page, token), skipPlaybackOptionsDialog);
        }


        public static VideoInfo AsVideoInfo(this ArdVideoInfoDto item, Context context, bool skipPlaybackOptionsDialog = false)
        {
            return new VideoInfo
                   {
                       Title = item.Title,
                       Description = item.Description ?? string.Empty,
                       Length = item.FormatDuration(),
                       Thumb = item.ImageUrl,
                       Airdate = item.AirDate?.ToLocalTime().ToString("g", OnlineVideoSettings.Instance.Locale) ?? string.Empty,
                       VideoUrl = item.TargetUrl,
                       HasDetails = !skipPlaybackOptionsDialog,
                       Other = context
                   };
        }


        private static string FormatDuration(this ArdVideoInfoDto item)
        {
            var duration = TimeSpan.FromSeconds(item.Duration ?? 0);
            return duration.TotalMinutes <= 0
                       ? string.Empty
                       : $"{duration.Minutes} Min.";
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
