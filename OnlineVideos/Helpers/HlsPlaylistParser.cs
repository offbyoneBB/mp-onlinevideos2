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

        /// <summary>
        /// Parse a m3u8 playlist into a dictionary with streamname and streamurl
        /// Sorting is done from high to low bandwidth, and streamname is like "1024x768 (200 Kbps)"
        /// </summary>
        /// <param name="playlist">the m3u8 data</param>
        /// <param name="url">url to use if the m3u8 data contains relative urls</param>
        /// <returns>Dictionary with the streamnames and urls</returns>
        public static Dictionary<string, string> GetPlaybackOptions(string playlist, string url)
        {
            return GetPlaybackOptions(playlist, url, HlsStreamInfoFormatter.VideoDimensionAndBitrate);
        }

        /// <summary>
        /// Parse a m3u8 playlist into a dictionary with streamname and streamurl
        /// Sorting is done from high to low bandwidth, and streamname is customizable
        /// </summary>
        /// <param name="playlist">the m3u8 data</param>
        /// <param name="url">url to use if the m3u8 data contains relative urls</param>
        /// <param name="formatter">delegate returning the formatted string for a given HlsStreamInfo</param>
        /// <returns>Dictionary with the streamnames and urls</returns>
        public static Dictionary<string, string> GetPlaybackOptions(string playlist, string url, HlsStreamInfoFormatter formatter)
        {
            return GetPlaybackOptions(playlist, url, HlsStreamInfoComparer.BandwidthHighLow, formatter);
        }

        /// <summary>
        /// Parse a m3u8 playlist into a dictionary with streamname and streamurl
        /// Sorting and streamname are customizable
        /// </summary>
        /// <param name="playlist">the m3u8 data</param>
        /// <param name="url">url to use if the m3u8 data contains relative urls</param>
        /// <param name="comparer">comparing two HlsStreamInfo, used in the sorting</param>
        /// <param name="formatter">formating display string for a given HlsStreamInfo</param>
        /// <returns>Dictionary with the streamnames and urls</returns>
        public static Dictionary<string, string> GetPlaybackOptions(string playlist, string url, HlsStreamInfoComparer comparer, HlsStreamInfoFormatter formatter)
        {
            Dictionary<string, string> playbackOptions = new Dictionary<string, string>();
            var tmp = new HlsPlaylistParser(playlist, url);

            foreach (var streamInfo in tmp.StreamInfos.OrderBy(info => info, comparer))
            {
                var streamName = formatter.Format(streamInfo);
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
            {
                return;
            }

            Uri baseUrl = new Uri(originalUrl);
            using (StringReader reader = new StringReader(playlist.Trim()))
            {
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
                        int p = line.IndexOf('#');
                        if (p >= 0)
                            line = line.Substring(0, p);
                        Uri streamUrl;
                        if (!Uri.TryCreate(line, UriKind.RelativeOrAbsolute, out streamUrl) || !streamUrl.IsAbsoluteUri)
                        {
                            streamUrl = new Uri(baseUrl, line);
                        }
                        Log.Debug("HlsPlaylistParser: Found stream info: Bandwidth '{0}', Resolution '{1}x{2}', Url '{3}'", bandwidth, width, height, streamUrl);
                        streamInfos.Add(new HlsStreamInfo(bandwidth, width, height, framerate, streamUrl.AbsoluteUri));
                        bandwidth = 0;
                        width = 0;
                        height = 0;
                    }
                }
            }
        }
    }

    public abstract class HlsStreamInfoFormatter
    {
        /// <summary>
        /// formats streamInfo like "1024x768 (Bitrate: 4096 Kbps)"
        /// </summary>
        public static HlsStreamInfoFormatter VideoDimensionAndBitrate { get; private set; }

        /// <summary>
        /// formats streamInfo like "1024x768"
        /// </summary>
        public static HlsStreamInfoFormatter VideoDimension { get; private set; }

        /// <summary>
        /// formats streamInfo like "Bitrate: 4096 Kbps"
        /// </summary>
        public static HlsStreamInfoFormatter Bitrate { get; private set; }

        static HlsStreamInfoFormatter()
        {
            VideoDimension = new DelegateHlsStreamInfoFormatter(streamInfo => string.Format("{0}x{1}", streamInfo.Width, streamInfo.Height));
            Bitrate = new DelegateHlsStreamInfoFormatter(streamInfo => string.Format("Bitrate: {0} Kbps", streamInfo.Bandwidth / 1000));

            VideoDimensionAndBitrate  = new DelegateHlsStreamInfoFormatter(streamInfo => string.Format("{0} ({1})", VideoDimension.Format(streamInfo), Bitrate.Format(streamInfo)));
        }

        public abstract string Format(HlsStreamInfo streamInfo);


        public class DelegateHlsStreamInfoFormatter : HlsStreamInfoFormatter
        {
            private Func<HlsStreamInfo, string> _formatterDelegate;
            public DelegateHlsStreamInfoFormatter(Func<HlsStreamInfo, string> formatterDelegate)
            {
                _formatterDelegate = formatterDelegate;
            }

            public override string Format(HlsStreamInfo streamInfo)
            {
                return _formatterDelegate(streamInfo);
            }
        }

    }
    public abstract class HlsStreamInfoComparer : IComparer<HlsStreamInfo>
    {
        public static HlsStreamInfoComparer BandwidthHighLow { get; private set; }
        public static HlsStreamInfoComparer BandwidtLowHigh { get; private set; }

        static HlsStreamInfoComparer()
        {
            BandwidthHighLow = new BandwidthComparer(false);
            BandwidtLowHigh = new BandwidthComparer(true);
        }

        public abstract int Compare(HlsStreamInfo x, HlsStreamInfo y);

        class BandwidthComparer : HlsStreamInfoComparer
        {
            private readonly bool _ascending;
            public BandwidthComparer(bool ascending)
            {
                _ascending = ascending;
            }

            public override int Compare(HlsStreamInfo x, HlsStreamInfo y)
            {
                return _ascending ? x.Bandwidth.CompareTo(y.Bandwidth) : y.Bandwidth.CompareTo(x.Bandwidth);
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
