using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using OnlineVideos.MPUrlSourceFilter;

namespace OnlineVideos.Sites
{
    public class GofastIPTVUtil : GenericSiteUtil
    {
        [Category("OnlineVideosUserConfiguration"), Description("Username of your gofastiptv account")]
        private string username = null;

        [Category("OnlineVideosUserConfiguration"), Description("Password of your gofastiptv account"), PasswordPropertyText(true)]
        private string password = null;


        private static readonly Regex extinfReg = new Regex(@"\#EXTINF[^\s]*\stvg-id=""(?<tvgid>[^""]+)""\stvg-name=""(?<tvgname>[^""]*?)(?:(?<reso>\s(HD|FHD|HEVC|FHD\sHEVC)(?:\s\([^\)]*\))?))?""\stvg-logo=""(?<tvglogo>[^""]*)""\sgroup-title=""(?<grouptitle>[^""]*?)(?:(?<groupreso>\s(HD|FHD|HEVC|FHD\sHEVC)))?"",(?<rest>.*)", RegexOptions.IgnoreCase);

        SortedList<string, SortedList<string, SortedList<string, GoFastStream>>> groups = new SortedList<string, SortedList<string, SortedList<string, GoFastStream>>>();

        public override int DiscoverDynamicCategories()
        {
            var data = GetWebData(string.Format(baseUrl, username, password));
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
                            GoFastStream stream = new GoFastStream()
                            {
                                tvgid = m.Groups["tvgid"].Value,
                                tvgname = m.Groups["tvgname"].Value,
                                grouptitle = m.Groups["grouptitle"].Value,
                                reso = m.Groups["reso"].Value,
                                logo = m.Groups["tvglogo"].Value
                            };
                            if (String.IsNullOrEmpty(stream.tvgid))
                                stream.tvgid = "None";
                            stream.url = sr.ReadLine();
                            if (!groups.ContainsKey(stream.grouptitle))
                                groups.Add(stream.grouptitle, new SortedList<string, SortedList<string, GoFastStream>>());
                            var group = groups[stream.grouptitle];
                            if (!group.ContainsKey(stream.tvgid))
                                group.Add(stream.tvgid, new SortedList<string, GoFastStream>());
                            var channel = group[stream.tvgid];
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
            var vids = (SortedList<string, SortedList<string, GoFastStream>>)category.Other;
            List<VideoInfo> videos = new List<VideoInfo>();
            foreach (var vid in vids)
            {
                VideoInfo video = new GoFastVideoInfo()
                {
                    Title = vid.Key,
                    Other = vid.Value,
                    PlaybackOptions = new Dictionary<string, string>()
                };
                foreach (var res in vid.Value)
                {
                    video.PlaybackOptions.Add(res.Key, res.Value.url);
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

    public class GoFastVideoInfo : VideoInfo
    {
        public override string GetPlaybackOptionUrl(string url)
        {
            var result = WebCache.Instance.GetRedirectedUrl(PlaybackOptions[url]);
            HttpUrl res = new HttpUrl(result) { LiveStream = true };
            return res.ToString();
        }
    }


    class GoFastStream
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
