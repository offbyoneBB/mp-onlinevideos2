using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using Newtonsoft.Json;

namespace OnlineVideos.Sites
{
  public class DumpertUtil : GenericSiteUtil
  {

    public override string GetVideoUrl(VideoInfo video)
    {
        //string encodedHTML = getUrl(video);
        string resultUrl = GetFormattedVideoUrl(video);
        string playListUrl = GetPlaylistUrl(resultUrl);
        if (String.IsNullOrEmpty(playListUrl))
            return String.Empty; // if no match, return empty url -> error

        // 3.b find a match in the retrieved data for the final playback url
        if (regEx_FileUrl != null)
        {
            video.PlaybackOptions = GetPlaybackOptions(playListUrl);
                if (video.PlaybackOptions.Count == 0)
                {
                    //poss. youtube
                    var data = GetWebData(playListUrl);
                    Match m = Regex.Match(data, @"<iframe\sclass='yt-iframe'\sstyle='[^']*'\ssrc='(?<url>[^']*)'", defaultRegexOptions);
                    if (m.Success)
                        video.PlaybackOptions = Hoster.HosterFactory.GetHoster("Youtube").GetPlaybackOptions(m.Groups["url"].Value);
                    return video.GetPreferredUrl(false);
                }
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

        Dictionary<String, String> videos = getVidList(encodedHTML);

        if (videos != null)
        {
            if (videos.ContainsKey("embed"))
            {
                //youtube video {"embed":"youtube:dap5lEuS5uM","still":"http:\/\/static.dumpert.nl\/stills\/6650394_e53155f4.jpg"}
                string youtubeId = videos["embed"].Split(':')[1];
                video.PlaybackOptions = Hoster.HosterFactory.GetHoster("Youtube").GetPlaybackOptions("https://www.youtube.com/watch?v=" + youtubeId);
                return (video.PlaybackOptions == null || video.PlaybackOptions.Count == 0) ? "" : video.PlaybackOptions.First().Value;
            }
            else
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
        }
        else
            return String.Empty;
    }

    private Dictionary<String, String> getVidList(string encodedHTML)
    {
        Dictionary<String, String> videos = new Dictionary<String, String>();

        Log.Debug("DumpertUtil: base64encoded videoURLs: " + encodedHTML);
        byte[] data = Convert.FromBase64String(encodedHTML);
        string videoJson = Encoding.UTF8.GetString(data);
        return JsonConvert.DeserializeObject<Dictionary<string, string>>(videoJson);
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
