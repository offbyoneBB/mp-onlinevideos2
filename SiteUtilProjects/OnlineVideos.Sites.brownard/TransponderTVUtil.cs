using HtmlAgilityPack;
using OnlineVideos.Sites.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;

namespace OnlineVideos.Sites
{
    public class TransponderTVUtil : SiteUtilBase
    {
        const string BASE_URL = "https://www.transponder.tv";
        const string IMAGE_URL_FORMAT = BASE_URL + "/images/channelicons/{0}.png"; // Channel logo url
        const string LOGIN_URL = "/login"; // Login url

        [Category("OnlineVideosUserConfiguration"), Description("Username")]
        protected string Username = null;
        [Category("OnlineVideosUserConfiguration"), Description("Password")]
        protected string Password = null;

        public override int DiscoverDynamicCategories()
        {
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            video.PlaybackOptions = new Dictionary<string, string>();

            HtmlDocument watchDocument = Get(video.VideoUrl, Cookies);
            HtmlNode videoNode = watchDocument.DocumentNode.SelectSingleNode("//video/source");
            string playlistUrl = videoNode.GetAttributeValue("src", string.Empty);
            HlsPlaylistParser hlsPlaylist = new HlsPlaylistParser(GetWebData(playlistUrl, cache: false), playlistUrl);

            string lastVideoUrl = null;
            foreach (var stream in hlsPlaylist.StreamInfos)
            {
                lastVideoUrl = stream.Url;
                video.PlaybackOptions.Add(stream.Bandwidth.ToString(), lastVideoUrl);
            }
            return lastVideoUrl;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            // Epg page for all channels in the specified category
            HtmlDocument channelListingsDocument = Get((category as RssLink).Url, Cookies);
            if (channelListingsDocument != null)
            {
                // Select each channel and convert it to a video with now and next programme info
                var tvChannelListingNodes = channelListingsDocument.DocumentNode.SelectNodes("//div[contains(@class, 'tvguideChannelListings')]");
                if (tvChannelListingNodes != null)
                    videos.AddRange(tvChannelListingNodes.Select(n => GetChannelVideoFromEPG(n)).Where(v => v != null));
                else
                    Log.Debug("TransponderTV: Unable to find TV guide channel node");
            }
            return videos;
        }

        VideoInfo GetChannelVideoFromEPG(HtmlNode tvChannelListingNode)
        {
            VideoInfo video = new VideoInfo();

            // The first programme node contains the channel name
            var channelNode = tvChannelListingNode.SelectSingleNode("./div[contains(@class, 'programme')]/h3");
            if (channelNode != null)
                video.Title = channelNode.InnerText.Trim();

            StringBuilder descriptionBuilder = new StringBuilder(string.Empty);

            // Get the currently playing programme
            var currentProgrammeNode = tvChannelListingNode.SelectSingleNode("./div[contains(@class, 'programmeCurrent')]");
            if (currentProgrammeNode != null)
            {
                var linkNode = currentProgrammeNode.SelectSingleNode("./h3/a");
                // If the channel isn't currently on air the link won't be present so ignore this channel
                if (linkNode == null)
                {
                    Log.Debug("TransponderTV: Unable to find current programme for channel {0}", video);
                    return null;
                }

                // Link to the video page
                string url = linkNode.GetAttributeValue("href", string.Empty).Trim();
                video.VideoUrl = url;

                // The name of the channel's logo should be the same as the last segment in the video url
                int channelNameIndex = url.LastIndexOf('/') + 1;
                video.Thumb = string.Format(IMAGE_URL_FORMAT, url.Substring(channelNameIndex).Trim());

                // Add the current programme's title and time to the description
                descriptionBuilder.AppendLine(linkNode.InnerText.Trim());
                var timeNode = currentProgrammeNode.SelectSingleNode("./p");
                if (timeNode != null)
                    descriptionBuilder.AppendLine(timeNode.InnerText.Trim());
            }

            // Get the programme playing after the current
            var nextProgrammeNode = tvChannelListingNode.SelectSingleNode("./div[contains(@class, 'programmeCurrent')]/following-sibling::div[contains(@class, 'programme')]");
            if (nextProgrammeNode != null)
            {
                var anchorNode = nextProgrammeNode.SelectSingleNode("./h3/a");
                if (anchorNode != null)
                {
                    // Add the next programme's title and time to the description
                    descriptionBuilder.AppendLine(anchorNode.InnerText.Trim());
                    var timeNode = nextProgrammeNode.SelectSingleNode("./p");
                    if (timeNode != null)
                        descriptionBuilder.AppendLine(timeNode.InnerText.Trim());
                }
            }
            // Add the now/next description to the video
            video.Description = descriptionBuilder.ToString();

            Log.Debug("TransponderTV: Created video from channel node - {0}", video);
            return video;
        }

        #region Login

        CookieContainer cookies = null;

        protected CookieContainer Cookies
        {
            get
            {
                CookieContainer cc = cookies;
                if (IsLoggedIn(cc))
                    return cc;
                cookies = cc = Login(Username, Password);
                return cc;
            }
        }

        protected bool IsLoggedIn(CookieContainer cc)
        {
            if (cc == null)
                return false;
            foreach (Cookie c in cc.GetCookies(new Uri(BASE_URL)))
                if (c.Expires < DateTime.Now)
                    return false;
            return true;
        }

        /// <summary>
        /// Logs in to Transponder.tv using the specified username and password.
        /// </summary>
        /// <returns>Session cookies that should be used for subsequent requests.</returns>
        CookieContainer Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                throw new OnlineVideosException("Please enter a username and password.");

            CookieContainer cookies = new CookieContainer();

            // Initial request to the login page, needed to set some cookies and get the token used with the login POST request.
            HtmlDocument loginDocument = Get(LOGIN_URL, cookies);

            HtmlNode formNode = loginDocument.DocumentNode.SelectSingleNode("//div[@id='loginGridForm']/form");
            if (formNode == null)
            {
                Log.Error("TransponderTV: Unable to find login form.");
                throw new OnlineVideosException("Error logging in to TransponderTV");
            }            

            // Get the token
            HtmlNode tokenNode = formNode.SelectSingleNode(".//input[@name='_token']");
            string token = tokenNode != null ? tokenNode.GetAttributeValue("value", string.Empty) : string.Empty;

            // Post the token and credentials back to the login page.
            string postData = "_token=" + token + "&email=" + Uri.EscapeDataString(username) + "&password=" + password;
            HtmlDocument response = Get(LOGIN_URL, cookies, postData);
            // If we are still on the login page then login failed.
            if (response.DocumentNode.SelectSingleNode("//div[@id='loginGridForm']") != null)
                throw new OnlineVideosException("Invalid username or password");

            // Session cookies should now be set and can be used for subsequent requests
            return cookies;
        }

        #endregion

        /// <summary>
        /// Utility method for getting a html document from Transponder.tv without caching, and
        /// additionally using and setting any cookies in the specified <see cref="CookieContainer"/>.
        /// </summary>
        /// <param name="url">Relative or absolute url of the html document to get.</param>
        /// <param name="cookies">Cookies to use for the request.</param>
        /// <param name="postData">Optional post data.</param>
        /// <returns>The requested <see cref="HtmlDocument"/>.</returns>
        HtmlDocument Get(string url, CookieContainer cookies, string postData = null)
        {
            return WebCache.Instance.GetWebData<HtmlDocument>(BuildUri(url), postData, cookies: cookies, cache: false);
        }

        /// <summary>
        /// Returns the absolute url of a Transponder.tv document. 
        /// </summary>
        /// <param name="possibleRelativeUrl">Relative or absolute Transponder.tv url.</param>
        /// <returns>Absolute url of the Transpnder.tv document.</returns>
        static string BuildUri(string possibleRelativeUrl)
        {
            Uri uri = new Uri(possibleRelativeUrl, UriKind.RelativeOrAbsolute);
            return (uri.IsAbsoluteUri ? uri : new Uri(new Uri(BASE_URL), uri)).ToString();
        }
    }
}
