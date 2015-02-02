using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace OnlineVideos.Sites
{
  public class DumpertUtil : GenericSiteUtil
  {

    public override string GetVideoUrl(VideoInfo video)
    {
        //string encodedHTML = getUrl(video);
        string resultUrl = getFormattedVideoUrl(video);
        string playListUrl = getPlaylistUrl(resultUrl);
        if (String.IsNullOrEmpty(playListUrl))
            return String.Empty; // if no match, return empty url -> error

        // 3.b find a match in the retrieved data for the final playback url
        if (regEx_FileUrl != null)
        {
            video.PlaybackOptions = GetPlaybackOptions(playListUrl);
            if (video.PlaybackOptions.Count == 0) return ""; // if no match, return empty url -> error
            else
            {
                // return first found url as default
                var enumer = video.PlaybackOptions.GetEnumerator();
                enumer.MoveNext();
                resultUrl = enumer.Current.Value; 
            }
            if (video.PlaybackOptions.Count == 1) video.PlaybackOptions = null; // only one url found, PlaybackOptions not needed
        }

        string encodedHTML = resultUrl; //is a base64 encoded piece of javascript which contains the videos

        Log.Info("encoded HTML: " + encodedHTML);
        Dictionary<String, String> videos = getVidList(encodedHTML);

        if (videos != null)
        {
            video.PlaybackOptions = new System.Collections.Generic.Dictionary<string, string>();

            //sort from high to low quality: 720p, tablet, flv, mobile
            if (videos.ContainsKey("720p"))
            {
                video.PlaybackOptions.Add("720p", videos["720p"]);
            }
            if (videos.ContainsKey("tablet"))
            {
                video.PlaybackOptions.Add("High", videos["tablet"]);
            }
            if (videos.ContainsKey("flv"))
            {
                video.PlaybackOptions.Add("Medium", videos["flv"]);
            }
            if (videos.ContainsKey("mobile"))
            {
                video.PlaybackOptions.Add("Low", videos["mobile"]);
            }

            return video.PlaybackOptions.First().Value;
        }
        else
            return String.Empty;
    }

    private Dictionary<String, String> getVidList(string encodedHTML)
    {
        Dictionary<String, String> videos = new Dictionary<String, String>();

        Log.Debug("DumpertUtil: base64encoded videoURLs: " + encodedHTML);
        byte[] data = Convert.FromBase64String(encodedHTML);
        string decodedHTML = Encoding.UTF8.GetString(data);

        //construct url from {"flv":"http:\/\/media.dumpert.nl\/flv\/85bba761_YTDL_1.mp4.flv"
        Regex getVideos = new Regex("\"(?<type>[^\"]*)\":\"(?<url>[^\"]*)\"");
        MatchCollection matches = getVideos.Matches(decodedHTML);

        if (matches.Count > 0)
        {
            foreach (Match videosMatch in matches)
            {
                GroupCollection videosGroups = videosMatch.Groups;

                string videoUrl = videosGroups["url"].Value.Replace("\\/", "/");
                string videoType = videosGroups["type"].Value;
                Log.Info("DumpertUtil: " + videoType  + " - " + videoUrl);
                videos.Add(videoType, videoUrl);
            }
            videos.Remove("still"); //remove the still image from the videos list
            return videos;
        }
        else
        {
            return null;
        }
    }


    //unused, now it returns an 403 Forbidden error
    private string getOriginalDumpertUrl(string flvUrl) 
    {
        //try to get original url
        MatchCollection origVideoMatches = Regex.Matches(flvUrl, @"flv/(?<code>.*)\.flv");
        Match origVideoMatch = origVideoMatches[0];
        GroupCollection origVideoGroups = origVideoMatch.Groups;

        string origVideoFile = origVideoGroups["code"].Value;

        return "http://media.dumpert.nl/original/" + origVideoFile;
    }

  }
}
