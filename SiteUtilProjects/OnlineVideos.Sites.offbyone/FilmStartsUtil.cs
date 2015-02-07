using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class FilmStartsUtil : GenericSiteUtil
    {
        public enum Quality { Low, Mid, High };

        [Category("OnlineVideosConfiguration"), Description("Alternative Regex used for the result of a search.")]
        string SearchResultVideoListRegEx = @"<a\s+href='(?<VideoUrl>[^']+)'>\s*<img\s+src='(?<ImageUrl>[^']+)'\s+alt='(?<Title>[^']+)'\s*/>\s*</a>";
        [Category("OnlineVideosConfiguration"), Description("Regex to find an already present paging value in the query of an url for replacing with next page value.")]
        string PagingRegEx = @"([\?&](p|page))=\d+";
        [Category("OnlineVideosUserConfiguration"), Description("Chose the default Quality that will be preselected.")]
        Quality DefaultQuality = Quality.High;

        public override string GetVideoUrl(VideoInfo video)
        {
            var result = base.GetVideoUrl(video);
            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 1)
            {
                var quality = video.PlaybackOptions.FirstOrDefault(po => po.Key == DefaultQuality.ToString());
                if (quality.Value != null) return quality.Value;
            }
            return result;
        }

        public override string getPlaylistUrl(string resultUrl)
        {
            // 3.a extra step to get a playlist file if needed
            if (regEx_PlaylistUrl != null)
            {
                string dataPage = GetWebData(resultUrl, cookies: GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
                Match matchPlaylistUrl = regEx_PlaylistUrl.Match(dataPage);
                if (matchPlaylistUrl.Success)
                {
                    List<object> groupValues = new List<object>();
                    foreach(string groupName in regEx_PlaylistUrl.GetGroupNames().Where(g => g.StartsWith("m")).OrderBy(g => int.Parse(g.Substring(1))))
                    {
                        groupValues.Add(matchPlaylistUrl.Groups[groupName].Value);
                    }
                    string formattedUrl = string.Format(playlistUrlFormatString, groupValues.ToArray());
                    if (!Uri.IsWellFormedUriString(formattedUrl, System.UriKind.Absolute)) formattedUrl = new Uri(new Uri(baseUrl), formattedUrl).AbsoluteUri;
                    return formattedUrl;
                }
                else return String.Empty; // if no match, return empty url -> error
            }
            else
                return resultUrl;
        }

        public override Dictionary<string, string> GetPlaybackOptions(string playlistUrl)
        {
            var playbackOptions = base.GetPlaybackOptions(playlistUrl);
            var youTubeOption = playbackOptions.FirstOrDefault(po => po.Value.ToLower().Contains("youtube:"));
            if (youTubeOption.Value != null)
            {
                string youtubeId = youTubeOption.Value.Substring(youTubeOption.Value.ToLower().IndexOf("youtube:") + 8);
                return Hoster.HosterFactory.GetHoster("youtube").GetPlaybackOptions(youtubeId);
            }
            return playbackOptions.ToDictionary(po => ReadableQuality(po.Key), p => p.Value);
        }

        string ReadableQuality(string letter)
        {
            switch (letter)
            {
                case "l":return Quality.Low.ToString();
                case "m": return Quality.Mid.ToString();
                case "h": return Quality.High.ToString();
            }
            return letter;
        }

        protected override List<VideoInfo> Parse(string url, string data)
        {
            bool isSearch = url != null && (new Uri(url).GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.Unescaped) == new Uri(searchUrl).GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.Unescaped));
            List<VideoInfo> result;
            if (isSearch)
            {
                result = new List<VideoInfo>();
                if (string.IsNullOrEmpty(data)) data = GetWebData(url, cookies: GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
                Match m = Regex.Match(data, SearchResultVideoListRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
                while (m.Success)
                {
                    VideoInfo videoInfo = CreateVideoInfo();
                    videoInfo.Title = HttpUtility.HtmlDecode(m.Groups["Title"].Value);
                    // get, format and if needed absolutify the video url
                    videoInfo.VideoUrl = m.Groups["VideoUrl"].Value;
                    if (!string.IsNullOrEmpty(videoListRegExFormatString)) videoInfo.VideoUrl = string.Format(videoListRegExFormatString, videoInfo.VideoUrl);
                    videoInfo.VideoUrl = ApplyUrlDecoding(videoInfo.VideoUrl, videoListUrlDecoding);
                    if (!Uri.IsWellFormedUriString(videoInfo.VideoUrl, System.UriKind.Absolute)) videoInfo.VideoUrl = new Uri(new Uri(baseUrl), videoInfo.VideoUrl).AbsoluteUri;
                    // get, format and if needed absolutify the thumb url
                    videoInfo.ImageUrl = m.Groups["ImageUrl"].Value;
                    if (!string.IsNullOrEmpty(videoThumbFormatString)) videoInfo.ImageUrl = string.Format(videoThumbFormatString, videoInfo.ImageUrl);
                    if (!string.IsNullOrEmpty(videoInfo.ImageUrl) && !Uri.IsWellFormedUriString(videoInfo.ImageUrl, System.UriKind.Absolute)) videoInfo.ImageUrl = new Uri(new Uri(baseUrl), videoInfo.ImageUrl).AbsoluteUri;
                    videoInfo.Description = m.Groups["Description"].Value;
                    result.Add(videoInfo);
                    m = m.NextMatch();
                }
            }
            else
            {
                result = base.Parse(url, data);
                result.ForEach(v => v.Title = v.Title.Replace("<br/>", " -").Replace("</strong>", " -"));
            }

            if (data == null) data = GetWebData(url);

            nextPageAvailable = false;
            nextPageUrl = "";
            Match mNext = regEx_NextPage.Match(data);
            if (mNext.Success)
            {
                int currentPage = 0;
                int maxPages = 0;
                int.TryParse(mNext.Groups["curentPage"].Value, out currentPage);
                int.TryParse(mNext.Groups["maxPages"].Value, out maxPages);
                if (currentPage + 1 <= maxPages)
                {
                    nextPageAvailable = true;
                    if (Regex.IsMatch(url, PagingRegEx))
                    {
                        nextPageUrl = Regex.Replace(url, @"([\?&](p|page))=\d+", "$1=" + (currentPage + 1).ToString());
                    }
                    else
                    {
                        nextPageUrl = url + 
                                    (string.IsNullOrEmpty(new Uri(url).Query) ? "?" : "&") + 
                                    (isSearch ? "p=" + (currentPage + 1).ToString() : "page=" + (currentPage + 1).ToString());
                    }
                }
            }

            return result;
        }
    }
}
