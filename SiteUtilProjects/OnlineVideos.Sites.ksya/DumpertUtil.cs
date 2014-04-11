using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
  public class DumpertUtil : GenericSiteUtil
  {

    #region Overrides of GenericSiteUtil

    /// <summary>
    /// This function will be called to get the urls for playback of a video.<br/>
    /// By default: returns a list with the result from <see cref="getUrl"/>.
    /// </summary>
    /// <param name="video">The <see cref="VideoInfo"/> object, for which to get a list of urls.</param>
    /// <returns></returns>
    public override List<String> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
    {
        //video is a base64 encoded piece of javascript which contains the videos
        string encodedHTML = getUrl(video);
        Log.Debug("DumpertUtil: base64encoded videoURLs: " + encodedHTML);
        byte[] data = Convert.FromBase64String(encodedHTML);
        string decodedHTML = Encoding.UTF8.GetString(data);

        //construct url from {"flv":"http:\/\/media.dumpert.nl\/flv\/85bba761_YTDL_1.mp4.flv"
        Regex getVideos = new Regex("\"(?<type>[^\"]*)\":\"(?<url>[^\"]*)\"");
        MatchCollection matches = getVideos.Matches(decodedHTML);

        if (matches.Count > 0)
        {
            List<String> urls = new List<String>();
            List<String> videos = new List<String>();

            foreach (Match videosMatch in matches)
            {
                GroupCollection videosGroups = videosMatch.Groups;

                string videoUrl = videosGroups["url"].Value.Replace("\\/", "/");
                Log.Info("DumpertUtil: video: " + videoUrl);
                videos.Add(videoUrl);
            }
            videos.RemoveAt(videos.Count - 1); //remove the still image from the videos list


            //try to get original url
            MatchCollection origVideoMatches = Regex.Matches(videos[0], @"flv/(?<code>.*)\.flv");
            Match origVideoMatch = origVideoMatches[0];
            GroupCollection origVideoGroups = origVideoMatch.Groups;

            string origVideoFile = origVideoGroups["code"].Value;
            string orgVideoUrl = "http://media.dumpert.nl/original/" + origVideoFile;
            Log.Debug("DumpertUil: original video url: " + orgVideoUrl);

            urls.Add(orgVideoUrl);


            return urls;
        }else
        {
            Log.Info("DumpertUtil: no video found");
            return null;
        }
        
    }

    #endregion
  }
}
