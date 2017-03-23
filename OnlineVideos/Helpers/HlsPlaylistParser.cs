using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Helpers
{
    /// <summary>
    /// Parses a given HLS playlist and checks for any substreams
    /// </summary>
    public class HlsPlaylistParser
    {
        public const string APPLE_USER_AGENT = "AppleCoreMedia/1.0.0.8F455 (AppleTV; U; CPU OS 4_3 like Mac OS X; en_en)";

        private const string STREAM_INFO_TAG = "#EXT-X-STREAM-INF:";
        private static readonly Regex bandwidthReg = new Regex(@"BANDWIDTH=(\d+)", RegexOptions.IgnoreCase);
        private static readonly Regex resolutionReg = new Regex(@"RESOLUTION=(\d+)x(\d+)", RegexOptions.IgnoreCase);
        private static readonly Regex framerateReg = new Regex(@"FRAME-RATE=(\d+)", RegexOptions.IgnoreCase);

        private List<HlsStreamInfo> streamInfos = new List<HlsStreamInfo>();

        private HlsPlaylistParser(string playlist, string originalUrl)
        {
            PopulateStreamInfo(playlist, originalUrl);
        }

        public static Dictionary<string, string> GetPlaybackOptions(string url, string playlist)
        {
            return GetPlaybackOptions(url, playlist, (x, y) => x.Bandwidth.CompareTo(y.Bandwidth), (x) => x.Width + "x" + x.Height + " (" + x.Bandwidth / 1000 + " Kbps)");
        }

        public static Dictionary<string, string> GetPlaybackOptions(string url, string playlist, Comparison<HlsStreamInfo> sortcompare, Func<HlsStreamInfo, string> formatter)
        {
            Dictionary<string, string> playbackOptions = new Dictionary<string, string>();
            var tmp = new HlsPlaylistParser(playlist, url);
            tmp.streamInfos.Sort(sortcompare);
            foreach (var streamInfo in tmp.StreamInfos)
            {
                var streamName = formatter(streamInfo);
                if (!playbackOptions.ContainsKey(streamName))
                    playbackOptions.Add(streamName, streamInfo.Url);
            }
            return playbackOptions;
        }

        private List<HlsStreamInfo> StreamInfos
        {
            get { return streamInfos; }
        }

        protected void PopulateStreamInfo(string playlist, string originalUrl)
        {
            if (string.IsNullOrEmpty(playlist))
                return;

            Uri baseUrl = new Uri(originalUrl);
            StringReader reader = new StringReader(playlist.Trim());
            string line = reader.ReadLine();
            if (!line.StartsWith("#EXTM3U", StringComparison.InvariantCultureIgnoreCase))
            {
                Log.Warn("HlsPlaylistParser: Not a valid m3u8 file");
                return;
            }

            int bandwidth = 0;
            int width = 0;
            int height = 0;
            int framerate = 0;
            Match m;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith(STREAM_INFO_TAG))
                {
                    if ((m = bandwidthReg.Match(line)).Success)
                    {
                        bandwidth = int.Parse(m.Groups[1].Value);
                    }
                    if ((m = resolutionReg.Match(line)).Success)
                    {
                        width = int.Parse(m.Groups[1].Value);
                        height = int.Parse(m.Groups[2].Value);
                    }
                    if ((m = framerateReg.Match(line)).Success)
                    {
                        framerate = int.Parse(m.Groups[1].Value);
                    }
                }
                else if (line != string.Empty && !line.StartsWith("#"))
                {
                    Uri streamUrl;
                    if (!Uri.TryCreate(line, UriKind.RelativeOrAbsolute, out streamUrl) || !streamUrl.IsAbsoluteUri)
                        streamUrl = new Uri(baseUrl, line);
                    Log.Debug("HlsPlaylistParser: Found stream info: Bandwidth '{0}', Resolution '{1}x{2}', Url '{3}'", bandwidth, width, height, streamUrl);
                    streamInfos.Add(new HlsStreamInfo(bandwidth, width, height, framerate, streamUrl.ToString()));
                    bandwidth = 0;
                    width = 0;
                    height = 0;
                }
            }

        }
    }

    public class HlsStreamInfo
    {
        public HlsStreamInfo(int bandwidth, int width, int height, int frameRate, string url)
        {
            Bandwidth = bandwidth;
            Width = width;
            Height = height;
            FrameRate = frameRate;
            Url = url;
        }

        public override string ToString()
        {
            return Width + "x" + Height + ' ' + Bandwidth + ' ' + FrameRate;
        }

        public int Bandwidth { get; protected set; }
        public int Width { get; protected set; }
        public int Height { get; protected set; }
        public int FrameRate { get; protected set; }
        public string Url { get; protected set; }
    }
}
