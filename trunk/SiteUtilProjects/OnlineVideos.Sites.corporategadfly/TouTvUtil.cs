using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class TouTvUtil : GenericSiteUtil
    {
        private static Regex rtmpUrlRegex = new Regex(@"^(?<host>rtmp.*?)(\{break\}|\<break\>)(?<playPath>.*?)$",
            RegexOptions.Compiled);
        private static Regex singleVideoCategoryRegex = new Regex(@"<h1\sclass=""emission"">(?<Title>[^<]*)</h1>\s*<span\sclass=""clear""></span>\s*<p\sid=""MainContent_ctl00_PAnneeLabel""\sclass=""saison"">[^<]*</p>\s*<span\sclass=""clear""></span>\s*<p\sitemprop=""description""><br\s/>(?<Description>[^<]*)</p>\s*<br\s/>\s*<div\sclass=""specs"">\s*<p\sid=""MainContent_ctl00_PDateEpisode""><small>Date\sde\sdiffusion\s:</small>\s<strong>(?<Airdate>[^<]*)</strong></p>",
            RegexOptions.Compiled);

        protected override List<VideoInfo> Parse(string url, string data)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();
            if (string.IsNullOrEmpty(data)) data = GetWebData(url, GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
            if (data.Length > 0)
            {
                if (regEx_VideoList != null)
                {
                    Match m = regEx_VideoList.Match(data);
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
                        videoInfo.Length = Utils.PlainTextFromHtml(m.Groups["Duration"].Value);
                        videoInfo.Airdate = Utils.PlainTextFromHtml(m.Groups["Airdate"].Value);
                        videoInfo.Description = m.Groups["Description"].Value;
                        videoList.Add(videoInfo);
                        m = m.NextMatch();
                    }
                }
                
                // no videos found could mean that we are on a category that only has a single video,
                // so create a category with a single video (use a separate regular expression to find info)
                if (videoList.Count == 0)
                {
                    Log.Debug("No videos found, attempting to find single video");
                
                    VideoInfo videoInfo = CreateVideoInfo();
                    videoInfo.VideoUrl = url;
                    Match m = singleVideoCategoryRegex.Match(data);
                    if (m.Success)
                    {
                        videoInfo.Title = HttpUtility.HtmlDecode(m.Groups["Title"].Value);
                        videoInfo.Airdate = Utils.PlainTextFromHtml(m.Groups["Airdate"].Value);
                        videoInfo.Description = m.Groups["Description"].Value;
                    }
                    videoList.Add(videoInfo);
                }
            }
            
            return videoList;
        }
        
        public override string getUrl(VideoInfo video)
        {
            string playListUrl = getPlaylistUrl(video.VideoUrl);
            if (String.IsNullOrEmpty(playListUrl))
                return String.Empty; // if no match, return empty url -> error
            
            Log.Debug(@"video: {0}", video.Title);
            string result = string.Empty;

            video.PlaybackOptions = new Dictionary<string, string>();
            // keep track of bitrates and URLs
            Dictionary<int, string> urlsDictionary = new Dictionary<int, string>();

            XmlDocument xml = GetWebData<XmlDocument>(playListUrl);

            Log.Debug(@"SMIL loaded");

            XmlNamespaceManager nsmRequest = new XmlNamespaceManager(xml.NameTable);
            nsmRequest.AddNamespace("a", @"http://www.w3.org/2001/SMIL20/Language");

            XmlNode metaBase = xml.SelectSingleNode(@"//a:meta", nsmRequest);
            string metaBaseValue = metaBase.Attributes["base"].Value;

            // base URL may be stored in the base attribute of <meta> tag
            string baseRtmp = metaBaseValue.StartsWith("rtmp") ? metaBaseValue : String.Empty;

            foreach (XmlNode node in xml.SelectNodes("//a:body/a:switch/a:video", nsmRequest))
            {
                int bitrate = int.Parse(node.Attributes["system-bitrate"].Value);
                // do not bother unless bitrate is non-zero
                if (bitrate == 0) continue;

                string url = node.Attributes["src"].Value;
                if (!string.IsNullOrEmpty(baseRtmp))
                {
                    // prefix url with base (from <meta> tag) and artifical <break>
                    url = baseRtmp + @"<break>" + url;
                }
                Log.Debug(@"bitrate: {0}, url: {1}", bitrate / 1000, url);

                if (url.StartsWith("rtmp"))
                {
                    Match rtmpUrlMatch = rtmpUrlRegex.Match(url);

                    if (rtmpUrlMatch.Success && !urlsDictionary.ContainsKey(bitrate / 1000))
                    {
                        string host = rtmpUrlMatch.Groups["host"].Value;
                        string playPath = rtmpUrlMatch.Groups["playPath"].Value;
                        if (playPath.EndsWith(@".mp4") && !playPath.StartsWith(@"mp4:"))
                        {
                            // prepend with mp4:
                            playPath = @"mp4:" + playPath;
                        }
                        else if (playPath.EndsWith(@".flv"))
                        {
                            // strip extension
                            playPath = playPath.Substring(0, playPath.Length - 4);
                        }
                        Log.Debug(@"Host: {0}, PlayPath: {1}", host, playPath);
                        MPUrlSourceFilter.RtmpUrl rtmpUrl = new MPUrlSourceFilter.RtmpUrl(host) { PlayPath = playPath };
                        urlsDictionary.Add(bitrate / 1000, rtmpUrl.ToString());
                    }
                }
            }

            // sort the URLs ascending by bitrate
            foreach (var item in urlsDictionary.OrderBy(u => u.Key))
            {
                video.PlaybackOptions.Add(string.Format("{0} kbps", item.Key), item.Value);
                // return last URL as the default (will be the highest bitrate)
                result = item.Value;
            }
            return result;
        }

    }
}
