using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
  public class FernsehkritikTvUtil : GenericSiteUtil
  {
    private readonly Regex regexVideoBaseURL = new Regex(@"var[\s]*base[\s=']*(?<baseURL>[^']*)");
    private readonly Regex regexVideoEpisode = new Regex(@"var[\s]*ep[\s=]*(?<ep>[^;]*)");
    private readonly Regex regexVideoPlaylist = new Regex(@"url:[^']*'(?<part>[^}]*)'");

    #region Overrides of SiteUtilBase

    /// <summary>
    /// This function will be called to get the urls for playback of a video.<br/>
    /// By default: returns a list with the result from <see cref="getUrl"/>.
    /// </summary>
    /// <param name="video">The <see cref="VideoInfo"/> object, for which to get a list of urls.</param>
    /// <returns></returns>
    public override List<String> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
    {
      video.VideoUrl = video.VideoUrl + "Start/";
      Log.Info("FernsehkritikTvUtil: Get multiple urls: " + video.VideoUrl);

      string data = GetWebData(video.VideoUrl);
      if (!string.IsNullOrEmpty(data))
      {
        Match urlMatch = regexVideoBaseURL.Match(data);
        Match episodeMatch = regexVideoEpisode.Match(data);
        MatchCollection playlistMatches = regexVideoPlaylist.Matches(data);

        if (urlMatch.Success && episodeMatch.Success && playlistMatches.Count > 0)
        {
          List<string> urlList = new List<string>();
          string baseURL = urlMatch.Groups["baseURL"].Value;
          string episode = episodeMatch.Groups["ep"].Value;

          foreach (Match match in playlistMatches)
          {
            string part = match.Groups["part"].Value;
            if (!part.StartsWith(episode))
              part = episode + part;

            urlList.Add(baseURL + part);
          }

          Log.Info("FernsehkritikTvUtil: Found {0} video urls.", urlList.Count);
          foreach (string url in urlList)
            Log.Debug(url);

          return urlList;
        }
        else
        {
          Log.Info("FernsehkritikTvUtil: Problems while matching video urls...");
          Log.Debug("Matches: urlMatch.Success={0}|episodeMatch.Success={1}|playlistMatches.Count={2}",
            urlMatch.Success, episodeMatch.Success, playlistMatches.Count);
          Log.Debug(data);
        }
      }

      return null;
    }

    #endregion
  }
}
