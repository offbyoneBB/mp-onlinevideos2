using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Xml;
using System.Web;
using OnlineVideos.MPUrlSourceFilter;

namespace OnlineVideos.Sites
{
    public class LeagueOfLegendsUtil : GenericSiteUtil
    {
        protected override List<VideoInfo> Parse(string url, string data)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();
            if (string.IsNullOrEmpty(data)) data = GetWebData(url);
            if (data.Length > 0)
            {
                Match m = regEx_VideoList.Match(data);
                while (m.Success)
                {
                    VideoInfo videoInfo = CreateVideoInfo();
                    videoInfo.Title = HttpUtility.HtmlDecode(m.Groups["Title"].Value);
                    string hoster = m.Groups["hoster"].Value;
                    videoInfo.Other = hoster;
                    switch (hoster)
                    {
                        case "own3d":
                            {
                                //for own3d:
                                videoInfo.VideoUrl = string.Format(@"http://www.own3d.tv/livecfg/{0}?autoPlay=true", m.Groups["VideoUrl"].Value);
                                videoInfo.Thumb = string.Format(@"http://img.live.own3d.tv/live/live_tn_{1}_.jpg?t={0}",
                                    m.Groups["ImageUrl"].Value, m.Groups["VideoUrl"].Value);
                                break;
                            }
                        case "justin":
                            {
                                //for justin/twitch:
                                videoInfo.Thumb = string.Format(@"http://static-cdn.justin.tv/previews/live_user_{0}-320x240.jpg", m.Groups["VideoUrl"].Value);
                                videoInfo.VideoUrl = string.Format(@"http://usher.justin.tv/find/{0}.xml?type=any", m.Groups["VideoUrl"].Value);
                                break;
                            }
                    }
                    videoInfo.Description = m.Groups["Description"].Value + " " + m.Groups["viewers"].Value + " viewers, country:" + m.Groups["country"].Value;
                    videoList.Add(videoInfo);
                    m = m.NextMatch();
                }
            }
            return videoList;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string webdata = GetWebData(video.VideoUrl);
            XmlDocument doc = new XmlDocument();
            video.PlaybackOptions = new Dictionary<string, string>();

            string hoster = video.Other as string;
            switch (hoster)
            {
                case "own3d":
                    {
                        doc.LoadXml(webdata);
                        XmlNodeList streams = doc.SelectNodes("//config/channels/channel/clip/item[starts-with(@base, 'rtmp')]/stream");
                        foreach (XmlNode stream in streams)
                        {
                            string label = stream.Attributes["label"].Value;
                            RtmpUrl theUrl = new RtmpUrl(stream.ParentNode.Attributes["base"].Value)
                                {
                                    PlayPath = stream.Attributes["name"].Value,
                                    Live = true
                                };
                            if (!video.PlaybackOptions.ContainsKey(label))
                                video.PlaybackOptions.Add(label, theUrl.ToString());
                        }; break;
                    }
                case "justin":
                    {
                        string s2 = Regex.Replace(webdata, @"<((?:/)?)(\d)", "<$1a$2");
                        // fix illegal <number items
                        doc.LoadXml(s2);
                        SortedDictionary<int, string> urls = new SortedDictionary<int, string>();
                        foreach (XmlNode stream in doc.SelectSingleNode("nodes").ChildNodes)
                        {
                            string node = stream.SelectSingleNode("node").InnerText;
                            string play = stream.SelectSingleNode("play").InnerText;
                            string bitrate = stream.SelectSingleNode("bitrate").InnerText;
                            int br = Convert.ToInt32(double.Parse(bitrate));
                            string token = stream.SelectSingleNode("token").InnerText;
                            string connect = stream.SelectSingleNode("connect").InnerText;
                            RtmpUrl theUrl = new RtmpUrl(connect)
                            {
                                Jtv = token,
                                Live = true,
                                PlayPath = play,
                                SwfUrl = @"http://www-cdn.jtvnw.net/widgets/live_embed_player.r9c27c302ba389b0ff3a9f34a7a0cb495dfc3e424.swf"
                            };
                            if (!urls.ContainsKey(br))
                                urls.Add(br, theUrl.ToString());
                        }
                        video.PlaybackOptions = urls.ToDictionary(u => u.Key.ToString() + "b/s", u => u.Value);
                    }; break;
            }

            string resultUrl;
            if (video.PlaybackOptions.Count == 0) return String.Empty;
            else
                resultUrl = video.PlaybackOptions.Last().Value;
            if (video.PlaybackOptions.Count == 1) video.PlaybackOptions = null;
            return resultUrl;

        }
    }
}
