using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class FiveMinUtil : GenericSiteUtil
    {
        public override Dictionary<string, string> GetPlaybackOptions(string playlistUrl)
        {
            string dataPage;
            if (String.IsNullOrEmpty(fileUrlPostString))
                dataPage = GetWebData(playlistUrl, GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
            else
                dataPage = GetWebDataFromPost(playlistUrl, fileUrlPostString, GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);

            Dictionary<string, string> playbackOptions = new Dictionary<string, string>();
            Match matchFileUrl = regEx_FileUrl.Match(dataPage);
            while (matchFileUrl.Success)
            {
                // apply some formatting to the url
                List<string> groupValues = new List<string>();
                List<string> groupNameValues = new List<string>();
                for (int i = 0; i < matchFileUrl.Groups.Count; i++)
                {
                    groupValues.Add(ApplyUrlDecoding(matchFileUrl.Groups["m" + i.ToString()].Value, fileUrlDecoding));
                    groupNameValues.Add(ApplyUrlDecoding(matchFileUrl.Groups["n" + i.ToString()].Value, fileUrlNameDecoding));
                }
                string foundUrl = string.Format(groupValues[1] == "" ? fileUrlFormatString.Replace("_", "") : fileUrlFormatString, groupValues.ToArray());
                // try to JSON deserialize
                if (foundUrl.StartsWith("\"") && foundUrl.EndsWith("\""))
                {
                    try
                    {
                        string deJSONified = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(foundUrl);
                        if (!string.IsNullOrEmpty(deJSONified)) foundUrl = deJSONified;
                    }
                    catch { }
                }
                if (!playbackOptions.ContainsValue(foundUrl))
                {
                    if (groupNameValues.Count == 0) groupNameValues.Add(playbackOptions.Count.ToString()); // if no groups to build a name, use numbering
                    string urlNameToAdd = string.Format(fileUrlNameFormatString, groupNameValues.ToArray());
                    if (playbackOptions.ContainsKey(urlNameToAdd))
                        urlNameToAdd += playbackOptions.Count.ToString();
                    playbackOptions.Add(urlNameToAdd, foundUrl);
                }
                matchFileUrl = matchFileUrl.NextMatch();
            }
            if (playbackOptions.Count == 0 && Regex.IsMatch(dataPage, "messageError=ErrorVideoNoLongerAvailable&")) throw new OnlineVideosException("Video no longer available!");
            return playbackOptions;
        }
    }
}
