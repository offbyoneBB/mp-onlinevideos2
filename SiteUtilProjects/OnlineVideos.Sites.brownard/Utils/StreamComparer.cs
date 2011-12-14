using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites.brownardPRIVATE
{
    class StreamComparer : IComparer<string>
    {
        public static string GetBestPlaybackUrl(Dictionary<string, string> playbackOptions, int qualityPreference = 5, bool autoStart = false, int maxBitrate = int.MaxValue, string regExPattern = @"(?<quality>\d+)\s*kbps")
        {
            if (playbackOptions == null || playbackOptions.Count < 1)
                return "";

            List<string> streams = new List<string>();
            foreach (KeyValuePair<string, string> key in playbackOptions)
                streams.Add(key.Key); //create list of all PlaybackOptions keys

            List<KeyValuePair<string, int>> qualities = new List<KeyValuePair<string, int>>();

            if (string.IsNullOrEmpty(regExPattern))
                regExPattern = @"(?<quality>\d+)\s*kbps";
            Regex reg = new Regex(regExPattern);

            Match m; System.Text.RegularExpressions.Group g;
            foreach (string stream in streams) //loop through each stream and try to determine quality
            {
                if ((m = reg.Match(stream)).Success && (g = m.Groups["quality"]).Success)
                {
                    int quality;
                    if (int.TryParse(g.Value, out quality) && quality <= maxBitrate) //quality successfully parsed and it's less than the max rate
                        qualities.Add(new KeyValuePair<string, int>(stream, quality));
                }
            }

            if (qualities.Count < 1)
            {
                //error getting qualities
                return playbackOptions[streams[streams.Count - 1]];
            }

            qualities.Sort(delegate(KeyValuePair<string, int> first, KeyValuePair<string, int> other) //sort streams by stream quality
            {
                return first.Value.CompareTo(other.Value);
            });

            string selectedKey;
            if (qualities.Count == 1 || qualityPreference < 2) //only 1 stream or target quality is 1
                selectedKey = qualities[0].Key; //select lowest quality (1st) stream
            else
            {
                int min = qualities[0].Value;
                int max = qualities[qualities.Count - 1].Value;
                int range = max - min; //get difference between lowest and highest quality
                selectedKey = qualities[0].Key; //select 1st stream
                if (range > 0)
                {
                    //determine target stream bitrate relative to lowest and highest available
                    //i.e qualityPrefs 2, 3 and 4 are equally spaced between lowest and highest bitrates
                    int target = max;
                    if (qualityPreference < 5)
                        target = min + (range / 4) * (qualityPreference - 1);

                    //select stream closest to target bitrate
                    //start with lowest
                    int diff = target - min;
                    int closestBitrate = min;
                    for (int i = 1; i < qualities.Count; i++)
                    {
                        int newBitrate = qualities[i].Value;
                        //if bitrates are the same, prefer list order
                        if (newBitrate == closestBitrate)
                            continue;

                        //get difference between current stream quality and target quality
                        int newDiff = Math.Abs(newBitrate - target);
                        //if we're further away then we must already have best stream, 
                        //or if we're the same distance and qualityPreference is 2 prefer lower quality
                        if (newDiff > diff || (newDiff == diff && qualityPreference < 3))
                            break;

                        diff = newDiff; //set new closest
                        selectedKey = qualities[i].Key; //select stream
                    }
                }
            }

            string url = playbackOptions[selectedKey];
            if (autoStart)
            {
                //if we want to auto start stream, remove all but selected url from PlaybackOptions
                playbackOptions.Clear();
                playbackOptions.Add(selectedKey, url);
            }

            return url;
        }

        public int Compare(string x, string y)
        {
            int x_kbps = 0;
            if (!int.TryParse(System.Text.RegularExpressions.Regex.Match(x, @"(\d+) kbps").Groups[1].Value, out x_kbps)) return 1;
            int y_kbps = 0;
            if (!int.TryParse(System.Text.RegularExpressions.Regex.Match(y, @"(\d+) kbps").Groups[1].Value, out y_kbps)) return -1;
            int compare = x_kbps.CompareTo(y_kbps);
            if (compare != 0)
                return compare;
            return x.CompareTo(y);
        }
    }
}
