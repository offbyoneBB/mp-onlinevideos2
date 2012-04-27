using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using OnlineVideos.Sites.MpExtendedService;
using OnlineVideos.Sites.MpExtendedStreamingService;
using OnlineVideos.Sites.MpExtendedTvService;
using System.Web;

namespace OnlineVideos.Sites
{
    public class MpExtended : SiteUtilBase
    {
		[Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Username"), Description("Username for MpExtended"), DefaultValue("admin")]
        String mUsername = "admin";

		[Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Password"), Description("Password for MpExtended"), PasswordPropertyText(true), DefaultValue("admin")]
        String mPassword = "admin";

		[Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Server"), Description("MpExtended Address"), DefaultValue("localhost")]
        String mServer = "localhost";

		[Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Port"), Description("MpExtended Port"), DefaultValue(4322)]
        int mPort = 4322;

        MpExtendedService.MediaAccessService mediaAccess;
        MpExtendedTvService.TVAccessService tvAccess;
        MpExtendedStreamingService.SoapEndpoint mediaStreaming;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            mediaAccess = new MpExtendedService.MediaAccessService();
            mediaAccess.Url = String.Format("http://{0}:{1}/MPExtended/MediaAccessService/", mServer, mPort);
            mediaAccess.Credentials = new NetworkCredential(mUsername, mPassword);

            tvAccess = new MpExtendedTvService.TVAccessService();
            tvAccess.Url = String.Format("http://{0}:{1}/MPExtended/TvAccessService/", mServer, mPort);
            tvAccess.Credentials = new NetworkCredential(mUsername, mPassword);

            mediaStreaming = new MpExtendedStreamingService.SoapEndpoint();
            mediaStreaming.Url = String.Format("http://{0}:{1}/MPExtended/StreamingService/soap", mServer, mPort);
            mediaStreaming.Credentials = new NetworkCredential(mUsername, mPassword);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            WebMediaServiceDescription desc = mediaAccess.GetServiceDescription();

            foreach (WebBackendProvider p in desc.AvailableMovieLibraries)
            {
                RssLink catLive = new RssLink();
                catLive.Name = p.Name;
                catLive.Url = "movies";
                catLive.HasSubCategories = false;
                catLive.Other = p;
                Settings.Categories.Add(catLive);
            }

            foreach (WebBackendProvider p in desc.AvailableTvShowLibraries)
            {
                RssLink catLive = new RssLink();
                catLive.Name = p.Name;
                catLive.Url = "series";
                catLive.HasSubCategories = true;
                catLive.Other = p;
                Settings.Categories.Add(catLive);
            }

            WebTVServiceDescription tvDesc = tvAccess.GetServiceDescription();

            if (tvDesc.HasConnectionToTVServerSpecified)
            {
                RssLink catTV = new RssLink();
                catTV.Name = "TV";
                catTV.Url = "tv";
                catTV.HasSubCategories = true;
                Settings.Categories.Add(catTV);
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        /// <summary>
        /// Dynamically discover sub-categories for a given parent category
        /// </summary>
        /// <param name="parentCategory">parent category</param>
        /// <returns>Count of sub-categories</returns>
        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            String url = ((RssLink)parentCategory).Url;
            if (url.Equals("series"))
            {
                WebBackendProvider provider = (WebBackendProvider)parentCategory.Other;
                if (((RssLink)parentCategory).Url.Equals("series"))
                {
                    GetSeries(parentCategory, provider);
                }
            }
            else if (url.Equals("tvshow"))
            {
                WebTVShowBasic series = (WebTVShowBasic)parentCategory.Other;
                GetSeasons(parentCategory, series);
            }
            else if (url.Equals("tv"))
            {
                GetTvGroups(parentCategory);
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        private void GetTvGroups(Category parentCategory)
        {
            WebChannelGroup[] groups = tvAccess.GetGroups(MpExtendedTvService.SortField.User, true, MpExtendedTvService.SortOrder.Asc, true);

            foreach (WebChannelGroup g in groups)
            {
                RssLink cat = new RssLink();
                cat.Name = g.GroupName;
                cat.Url = "tvgroup";
                cat.HasSubCategories = false;
                cat.Other = g;
                parentCategory.SubCategories.Add(cat);
                cat.ParentCategory = parentCategory;
            }
        }

        private void GetSeasons(Category parentCategory, WebTVShowBasic series)
        {
            WebTVSeasonDetailed[] seasons = mediaAccess.GetTVSeasonsDetailedForTVShow(series.PID, true, series.Id, SortBy.TVSeasonNumber, true, OrderBy.Asc, true);

            foreach (WebTVSeasonDetailed s in seasons)
            {
                RssLink cat = new RssLink();
                cat.Name = "Season " + s.SeasonNumber;
				cat.Description = s.YearSpecified && s.Year > 0 ? string.Format("{0}: {1}", Translation.Instance.DateOfRelease, s.Year) : "";
				cat.EstimatedVideoCount = s.EpisodeCountSpecified ? (uint?)s.EpisodeCount : null;
				if (s.Artwork.Any(a => a.Type == WebFileType.Banner)) cat.Thumb = String.Format("http://{0}:{1}/MPExtended/StreamingService/stream/GetArtworkResized?id={2}&provider={3}&artworktype=2&offset=0&mediatype=6&maxWidth=160&maxHeight=160", mServer, mPort, s.Id, s.PID);
                cat.Url = "tvseason";
                cat.HasSubCategories = false;
                cat.Other = s;
                parentCategory.SubCategories.Add(cat);
                cat.ParentCategory = parentCategory;
            }
        }

        private void GetSeries(Category parentCategory, WebBackendProvider provider)
        {
            WebTVShowBasic[] allSeries = mediaAccess.GetAllTVShowsBasic(provider.Id, true, null, null, SortBy.Title, true, OrderBy.Asc, true);

            foreach (WebTVShowBasic s in allSeries)
            {
                RssLink cat = new RssLink();
                cat.Name = s.Title;
				cat.Description =
					(s.YearSpecified && s.Year > 0 ? string.Format("{0}: {1}", Translation.Instance.DateOfRelease, s.Year) : "") +
					(s.Genres != null && s.Genres.Length > 0 ? string.Format("\n{0}: {1}", Translation.Instance.Genre, string.Join(", ", s.Genres)) : "") +
					(s.Actors != null && s.Actors.Length > 0 ? string.Format("\n{0}: {1}", Translation.Instance.Actors, string.Join(", ", s.Actors.Take(4).Select(a => a.Name).ToArray())) : "");
				cat.EstimatedVideoCount = s.EpisodeCountSpecified ? (uint?)s.EpisodeCount : null;
				if (s.Artwork.Any(a => a.Type == WebFileType.Poster)) cat.Thumb = String.Format("http://{0}:{1}/MPExtended/StreamingService/stream/GetArtworkResized?id={2}&provider={3}&artworktype=3&offset=0&mediatype=5&maxWidth=160&maxHeight=160", mServer, mPort, s.Id, s.PID);
                cat.Url = "tvshow";
                cat.HasSubCategories = true;
                cat.Other = s;
                parentCategory.SubCategories.Add(cat);
                cat.ParentCategory = parentCategory;
            }
        }

        public override string getUrl(VideoInfo video)
        {
            WebStreamMediaType type = WebStreamMediaType.File;
            int providerId = 0;
            String itemId = null;
            String profile = null;
            bool live = false;
            
            if (video.Other.GetType().Equals(typeof(WebMovieBasic)))
            {
                WebMovieBasic movie = video.Other as WebMovieBasic;
                type = WebStreamMediaType.Movie;
                providerId = movie.PID;
                itemId = movie.Id;
                profile = "Flash pseudostreaming test";
            }
            else if (video.Other.GetType().Equals(typeof(WebTVEpisodeBasic)))
            {
                WebTVEpisodeBasic ep = video.Other as WebTVEpisodeBasic;
                type = WebStreamMediaType.TVEpisode;
                providerId = ep.PID;
                itemId = ep.Id;
                profile = "Flash pseudostreaming test";
            }
            else if (video.Other.GetType().Equals(typeof(WebChannelBasic)))
            {
                WebChannelBasic channel = video.Other as WebChannelBasic;
                type = WebStreamMediaType.TV;
                providerId = 0;
                itemId = channel.Id.ToString();
				profile = "Flash HQ";
                live = true;
            }
            //mediaStreaming.InitStream(type, true, movie.PID, true, movie.Id, "MediaPortal client", identifier, out result, out result);
            //String start = mediaStreaming.StartStream(identifier, "Flash HQ", 0, true);
            //String url2 = mediaStreaming.GetStreamingUrl(type, true, movie.PID, true, movie.Id, "MediaPortal client", "Flash pseudostreaming test", 0, true);
            // String ip = new MpExtendedStreamingService.SoapEndpoint1().Url.Replace("soapstream", "stream");
            bool result;
            mediaStreaming.AuthorizeStreaming(out result, out result);
			string url = string.Format("http://{0}:{1}/MPExtended/StreamingService/stream/DoStream?type={2}&provider={3}&itemId={4}&clientDescription={5}&profileName={6}&startPosition={7}", mServer, mPort, type, providerId, HttpUtility.UrlEncode(itemId), HttpUtility.UrlEncode("OnlineVideos client"), HttpUtility.UrlEncode(profile), 0);
            return url;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            RssLink cat = category as RssLink;
            List<VideoInfo> returnList = new List<VideoInfo>();
            if (cat.Url.Equals("movies"))
            {
                WebBackendProvider provider = (WebBackendProvider)cat.Other;

                if (provider != null)
                {
                    WebMovieBasic[] movies = mediaAccess.GetAllMoviesBasic(provider.Id, true, null, null, SortBy.Title, true, OrderBy.Asc, true);

                    foreach (WebMovieBasic m in movies)
                    {
                        VideoInfo info = new VideoInfo();
                        info.VideoUrl = m.Id;
						if (m.Artwork.Any(a => a.Type == WebFileType.Cover)) info.ImageUrl = String.Format("http://{0}:{1}/MPExtended/StreamingService/stream/GetArtworkResized?id={2}&provider={3}&artworktype=4&offset=0&mediatype=0&maxWidth=160&maxHeight=160", mServer, mPort, m.Id, m.PID);
                        info.Other = m;
                        info.Title = m.Title;
						info.Length = new DateTime(TimeSpan.FromMinutes(m.Runtime).Ticks).ToString("HH:mm:ss");
                        info.Airdate = m.Year.ToString();
						info.Description =
							(m.Genres != null && m.Genres.Length > 0 ? string.Format("\n{0}: {1}", Translation.Instance.Genre, string.Join(", ", m.Genres)) : "") +
							(m.Actors != null && m.Actors.Length > 0 ? string.Format("\n{0}: {1}", Translation.Instance.Actors, string.Join(", ", m.Actors.Take(4).Select(a => a.Name).ToArray())) : "");
                        returnList.Add(info);
                    }
                }
            }
            else if (cat.Url.Equals("tvseason"))
            {
                WebTVSeasonDetailed season = (WebTVSeasonDetailed)cat.Other;

                if (season != null)
                {
                    WebTVEpisodeBasic[] episodes = mediaAccess.GetTVEpisodesBasicForSeason(season.PID, true, season.Id, SortBy.TVEpisodeNumber, true, OrderBy.Asc, true);

                    foreach (WebTVEpisodeBasic e in episodes)
                    {
                        VideoInfo info = new VideoInfo();
                        info.VideoUrl = e.Id;
						if (e.Artwork.Any(a => a.Type == WebFileType.Banner)) info.ImageUrl = String.Format("http://{0}:{1}/MPExtended/StreamingService/stream/GetArtworkResized?id={2}&provider={3}&artworktype=2&offset=0&mediatype=3&maxWidth=160&maxHeight=160", mServer, mPort, e.Id, e.PID);
						if (e.RatingSpecified && e.Rating > 0.0f) info.Description = string.Format("Rating: {0}", e.Rating);
                        info.Other = e;
                        info.Title = string.Format("s{0:D2}e{1:D2} - {2}", e.SeasonNumber, e.EpisodeNumber, e.Title);
						info.Airdate = e.FirstAired.ToString("d", OnlineVideoSettings.Instance.Locale);

                        returnList.Add(info);
                    }
                }
            }
            else if (cat.Url.Equals("tvgroup"))
            {
                WebChannelGroup group = (WebChannelGroup)cat.Other;

                if (group != null)
                {
                    WebChannelBasic[] channels = tvAccess.GetChannelsBasic(group.Id, true, MpExtendedTvService.SortField.Channel, true, MpExtendedTvService.SortOrder.Asc, true);

                    foreach (WebChannelBasic c in channels)
                    {
                        VideoInfo info = new VideoInfo();
                        info.VideoUrl = c.Id.ToString();
                        info.Other = c;
                        info.Title = c.DisplayName;
                        returnList.Add(info);
                    }
                }
            }
            return returnList;
        }
    }
}
