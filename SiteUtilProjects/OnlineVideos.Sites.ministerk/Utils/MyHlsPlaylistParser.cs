using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites.Utils
{
    /// <summary>
    /// Parses a given HLS playlist and checks for any substreams
    /// </summary>
    class MyHlsPlaylistParser
    {
        public const string APPLE_USER_AGENT = "AppleCoreMedia/1.0.0.8F455 (AppleTV; U; CPU OS 4_3 like Mac OS X; en_en)";

        const string STREAM_INFO_TAG = "#EXT-X-STREAM-INF:";
        static readonly Regex bandwidthReg = new Regex(@"BANDWIDTH=(\d+)", RegexOptions.IgnoreCase);
        static readonly Regex resolutionReg = new Regex(@"RESOLUTION=(\d+)x(\d+)", RegexOptions.IgnoreCase);
        
        List<MyHlsStreamInfo> streamInfos = new List<MyHlsStreamInfo>();

        public MyHlsPlaylistParser(string playlist, string originalUrl)
        {
            PopulateStreamInfo(playlist, originalUrl);
        }

        public List<MyHlsStreamInfo> StreamInfos
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
                }
                else if (line != string.Empty && !line.StartsWith("#"))
                {
                    Uri streamUrl;
                    if (!Uri.TryCreate(line, UriKind.RelativeOrAbsolute, out streamUrl) || !streamUrl.IsAbsoluteUri)
                        streamUrl = new Uri(baseUrl, line);
                    Log.Debug("HlsPlaylistParser: Found stream info: Bandwidth '{0}', Resolution '{1}x{2}', Url '{3}'", bandwidth, width, height, streamUrl);
                    streamInfos.Add(new MyHlsStreamInfo(bandwidth, width, height, streamUrl.ToString()));
                    bandwidth = 0;
                    width = 0;
                    height = 0;
                }
            }

            streamInfos.Sort((x, y) => y.Bandwidth.CompareTo(x.Bandwidth));
        }
    }

    public class MyHlsStreamInfo
    {
        public MyHlsStreamInfo(int bandwidth, int width, int height, string url)
        {
            Bandwidth = bandwidth;
            Width = width;
            Height = height;
            Url = url;
        }

        public int Bandwidth { get; protected set; }
        public int Width { get; protected set; }
        public int Height { get; protected set; }
        public string Url { get; protected set; }
    }
}
