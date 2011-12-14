using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{    
    public class DasErsteMediathekUtil : GenericSiteUtil
    {        
        public enum VideoQuality { Low, High, Max };

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Video Quality", TranslationFieldName="VideoQuality"), Description("Choose your preferred quality for the videos according to bandwidth.")]
        VideoQuality videoQuality = VideoQuality.High;

        public override String getUrl(VideoInfo video)
        {
            if (video.PlaybackOptions == null || video.PlaybackOptions.Count == 0)
            {
                string dataPage = GetWebData(video.VideoUrl);
                video.PlaybackOptions = new Dictionary<string, string>();
                Match match = regEx_FileUrl.Match(dataPage);
                List<string[]> options = new List<string[]>();
                while (match.Success)
                {
                    string[] infos = match.Groups["Info"].Value.Split(',');
                    for (int i = 0; i < infos.Length; i++) infos[i] = infos[i].Trim(new char[] { '"', ' ' });
                    options.Add(infos);
                    match = match.NextMatch();
                }
                options.Sort(new Comparison<string[]>(delegate(string[] a, string[] b)
                    {
                        return int.Parse(a[1]).CompareTo(int.Parse(b[1]));
                    }));
                foreach(string[] infos in options)
                {
                    int type = int.Parse(infos[0]);
                    VideoQuality quality = (VideoQuality)int.Parse(infos[1]);
                    string resultUrl = "";
                    if (infos[infos.Length - 2].ToLower().StartsWith("rtmp"))
                    {
                        Uri uri = new Uri(infos[infos.Length - 2]);
                        if (uri.Host == "gffstream.fcod.llnwd.net")
                        {
                            resultUrl = uri.OriginalString.Trim('/') + "/" + infos[infos.Length - 1].Trim(new char[] { '"', ' ' });
                        }
                        else
                        {
							resultUrl = new MPUrlSourceFilter.RtmpUrl(infos[infos.Length - 2]) { PlayPath = infos[infos.Length - 1].Trim(new char[] { '"', ' ' }) }.ToString();
                        }
                        video.PlaybackOptions.Add(string.Format("{0} | rtmp:// | {1}", quality.ToString().PadRight(4, ' '), infos[infos.Length - 1].ToLower().Contains("mp4:") ? ".mp4" : ".flv"), resultUrl);
                    }
                    else
                    {
                        resultUrl = infos[infos.Length - 1].Trim(new char[] { '"', ' ' });                        
                        if (!resultUrl.EndsWith(".mp3"))
                        {
                            try
                            {
                                Uri uri = new Uri(resultUrl);
                                video.PlaybackOptions.Add(string.Format("{0} | {1}:// | {2}", quality.ToString().PadRight(4, ' '), uri.Scheme, System.IO.Path.GetExtension(resultUrl)), uri.AbsoluteUri);
                                if (resultUrl.EndsWith(".asx"))
                                {
                                    resultUrl = ParseASX(resultUrl)[0];
                                    uri = new Uri(resultUrl);
                                    video.PlaybackOptions.Add(string.Format("{0} | {1}:// | {2}", quality.ToString().PadRight(4, ' '), uri.Scheme, System.IO.Path.GetExtension(resultUrl)), uri.AbsoluteUri);
                                }                            
                            }
                            catch { }
                        }
                    }
                }
            }

            if (video.PlaybackOptions == null || video.PlaybackOptions.Count == 0)
            {
				// no url to play available
                return "";
            }
            else if (video.PlaybackOptions.Count == 1 || videoQuality == VideoQuality.Low)
            {
                //user wants low quality or only one playback option -> use first
                return video.PlaybackOptions.First().Value;
            }
            else if (videoQuality == VideoQuality.Max)
            {
                // take highest available quality
				return video.PlaybackOptions.Last().Value;
            }
            else
            {
				// choose a high quality from options (first below Max)
				return video.PlaybackOptions.Last(v => !v.Key.StartsWith(VideoQuality.Max.ToString())).Value;
            }
        }

    }
}

