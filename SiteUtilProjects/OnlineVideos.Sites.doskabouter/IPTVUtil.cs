using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using OnlineVideos.MPUrlSourceFilter;

namespace OnlineVideos.Sites
{
    public class IPTVUtil : GenericSiteUtil
    {
        [Category("OnlineVideosUserConfiguration"), Description("Url of your m3u8")]
        protected string m3u8url = "";

        private static readonly Regex extinfReg = new Regex(@"\#EXTINF[^\s]*\stvg-id=""(?<tvgid>[^""]*)""\stvg-name=""(?<tvgname>[^""]*?)(?:(?<reso>\s(HD|FHD|FHD\+|HEVC|FHD\sHEVC|H.265)(?:\s\([^\)]*\))?))?""\stvg-logo=""(?<tvglogo>[^""]*)""\sgroup-title=""(?<grouptitle>[^""]*?)(?:(?<groupreso>\s(HD|FHD|HEVC|FHD\sHEVC|HEVC\sH.265)))?"",(?<rest>.*)", RegexOptions.IgnoreCase);

        SortedList<string, SortedList<string, SortedList<string, IPTVStream>>> groups = new SortedList<string, SortedList<string, SortedList<string, IPTVStream>>>();

        public override int DiscoverDynamicCategories()
        {
            var data = GetWebData(m3u8url);
            using (StringReader sr = new StringReader(data))
            {
                string line = sr.ReadLine();
                if (line == "#EXTM3U")
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        Match m = extinfReg.Match(line);
                        if (m.Success)
                        {
                            IPTVStream stream = new IPTVStream()
                            {
                                tvgid = m.Groups["tvgid"].Value,
                                tvgname = m.Groups["tvgname"].Value.Replace(" H.265",""),
                                grouptitle = m.Groups["grouptitle"].Value.Replace(" Terugkijken + Overig", "").Replace(" KANALEN","").Replace(" HEVC H.265",""),
                                reso = m.Groups["reso"].Value,
                                logo = m.Groups["tvglogo"].Value
                            };
                            if (String.IsNullOrEmpty(stream.tvgid))
                                stream.tvgid = "None";
                            stream.url = sr.ReadLine();
                            if (!groups.ContainsKey(stream.grouptitle))
                                groups.Add(stream.grouptitle, new SortedList<string, SortedList<string, IPTVStream>>(StringComparer.CurrentCultureIgnoreCase));
                            var group = groups[stream.grouptitle];
                            if (!group.ContainsKey(stream.tvgname))
                                group.Add(stream.tvgname, new SortedList<string, IPTVStream>());
                            var channel = group[stream.tvgname];
                            if (!channel.ContainsKey(stream.reso))
                                channel.Add(stream.reso, stream);
                        }

                    }
                }
            }
            if (Settings.Categories == null) Settings.Categories = new BindingList<Category>();
            foreach (var group in groups)
            {
                Category cat = new Category()
                {
                    Name = group.Key,
                    Other = group.Value
                };
                Settings.Categories.Add(cat);
            }
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            var vids = (SortedList<string, SortedList<string, IPTVStream>>)category.Other;
            List<VideoInfo> videos = new List<VideoInfo>();
            foreach (var vid in vids)
            {
                VideoInfo video = new VideoInfo()
                {
                    Title = vid.Key,
                    Other = vid.Value,
                    PlaybackOptions = new Dictionary<string, string>()
                };
                foreach (var res in vid.Value)
                {
                    HttpUrl httpUrl = new HttpUrl(res.Value.url);
                    httpUrl.UserAgent = OnlineVideoSettings.Instance.UserAgent;
                    httpUrl.LiveStream = String.IsNullOrEmpty(Path.GetExtension(res.Value.url));
                    video.PlaybackOptions.Add(res.Key, httpUrl.ToString());
                    video.Thumb = res.Value.logo;
                }
                videos.Add(video);
            }
            return videos;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            return video.GetPreferredUrl(true);
        }

    }

    class IPTVStream
    {
        public string tvgid;
        public string tvgname;
        public string grouptitle;
        public string reso;
        public string url;
        public string logo;
        public override string ToString()
        {
            return tvgname + " " + grouptitle;
        }
    }
}
