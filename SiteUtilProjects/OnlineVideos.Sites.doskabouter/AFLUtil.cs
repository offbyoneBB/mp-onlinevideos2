using System;
using System.Collections.Generic;
using System.Xml;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class AFLUtil : GenericSiteUtil
    {
        private enum Levels { Competition, Season, RoundVideos, VideoListTeam, Match };
        RegexOptions defaultRegexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture;
        Regex allVideosSubcats;

        Dictionary<string, string> squads = new Dictionary<string, string>();

        public override int DiscoverDynamicCategories()
        {
            allVideosSubcats = new Regex(@"<li\sclass=""[^""]*""><a\shref=""(?<url>[^""]*)""\sonclick=""_VideoSearch\.LoadMedia[^""]*""><span>(?<title>[^<]*)</span></a></li>", defaultRegexOptions);

            base.DiscoverDynamicCategories();

            XmlDocument doc = new XmlDocument();
            doc.Load(@"http://xml.afl.com.au/Squad.aspx");
            foreach (XmlNode squadNode in doc.SelectNodes(@"//squads/squad"))
                squads.Add(squadNode.Attributes["id"].Value, squadNode.Attributes["name"].Value);

            RssLink aflVideosCat = new RssLink()
            {
                Name = "AFL Videos",
                SubCategories = new List<Category>(),
                HasSubCategories = true,
                SubCategoriesDiscovered = true
            };
            Settings.Categories.Add(aflVideosCat);
            RssLink allCat = new RssLink()
            {
                Name = "All",
                HasSubCategories = true,
                ParentCategory = aflVideosCat,
                SubCategoriesDiscovered = true,
                SubCategories = new List<Category>()
            };
            aflVideosCat.SubCategories.Add(allCat);

            string data = GetWebData(baseUrl);
            Match m = allVideosSubcats.Match(data);
            while (m.Success)
            {  //Highlights..Club video
                RssLink cat = new RssLink();
                cat.Url = m.Groups["url"].Value;
                cat.Name = m.Groups["title"].Value;
                cat.ParentCategory = allCat;
                allCat.SubCategories.Add(cat);
                m = m.NextMatch();
            }

            m = Regex.Match(data, @"<option\svalue=""(?<videoTabId>[^0][^""]*)"">(?<title>[^<]*)</option>", defaultRegexOptions);
            while (m.Success)
            {  //Adelaide Crows..Western Bulldogs
                RssLink cat = new RssLink();
                cat.Url = String.Format(@"http://www.afl.com.au/ajax.aspx?feed=VideoSearch&videoTabId={0}&videoSubTabId=0&page=1&mid=131673",
                    m.Groups["videoTabId"].Value);
                cat.Name = m.Groups["title"].Value;
                cat.ParentCategory = aflVideosCat;
                cat.HasSubCategories = true;
                cat.Other = new Level() { level = Levels.VideoListTeam };
                aflVideosCat.SubCategories.Add(cat);
                m = m.NextMatch();
            }

            return Settings.Categories.Count;
        }


        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            string url = ((RssLink)parentCategory).Url;
            string data = GetWebData(url);

            if (parentCategory.Other == null) // Competition
            {
                string[] parts = url.Split('/');
                string competitionid = parts[parts.Length - 2];

                Match m = regEx_dynamicSubCategories.Match(data);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    string seasonid = m.Groups["seasonid"].Value;
                    cat.Url = String.Format(@"http://www.afl.com.au/ajax.aspx?feed=RoundFixtureList&seasonID={0}&methodName=RoundLoad&competitionID={1}&mid=131672",
                        seasonid, competitionid);

                    cat.Name = m.Groups["title"].Value;
                    cat.ParentCategory = parentCategory;
                    cat.HasSubCategories = true;
                    cat.Other = new Level() { level = Levels.Season, seasonId = seasonid, competitionId = competitionid };
                    parentCategory.SubCategories.Add(cat);
                    m = m.NextMatch();
                }

                parentCategory.SubCategoriesDiscovered = true;
                return parentCategory.SubCategories.Count;

            }
            Level level = (Level)parentCategory.Other;
            switch (level.level)
            {
                /*case Levels.Round:
                    {
                        Match m = Regex.Match(data, @"<div\sclass=""roundinfo""\sstyle=""height:28px;"">\s*<div\sclass=""teams"">\s*<h6\sclass=""home"">(?<Home>[^\s]*)\s</h6>\s*<em>v</em>\s*<h6\sclass=""away"">(?<Away>[^\s]*)\s</h6>\s*</div>\s*<div\sclass=""links"">(?<VideoList>.*?)</div>\s*</div>", defaultRegexOptions);
                        while (m.Success)
                        {  //vs
                            RssLink cat = new RssLink();
                            cat.Name = m.Groups["Home"].Value + " vs " + m.Groups["Away"].Value;
                            List<VideoInfo> videoList = new List<VideoInfo>();
                            Match m2 = Regex.Match(m.Groups["VideoList"].Value, @"<a\shref=""(?<VideoUrl>[^""]*)""\sonclick=""[^>]*>(?<Title>[^<]*)</a>", defaultRegexOptions);
                            while (m2.Success)
                            {
                                VideoInfo video = new VideoInfo();
                                video.Title = m2.Groups["Title"].Value;
                                video.VideoUrl = m2.Groups["VideoUrl"].Value;
                                videoList.Add(video);
                                m2 = m2.NextMatch();
                            }
                            cat.Other = videoList;
                            cat.ParentCategory = parentCategory;
                            parentCategory.SubCategories.Add(cat);
                            m = m.NextMatch();
                        }
                        parentCategory.SubCategoriesDiscovered = true;
                        return parentCategory.SubCategories.Count;
                    }*/
                case Levels.Season:
                    {
                        Match m = Regex.Match(data, @"(?<!<ul\sclass=""rounds"">.*)<option\svalue=""(?<roundid>[^""]*)""[^>]*>(?<title>[^<]*)</option>", defaultRegexOptions);
                        while (m.Success)
                        {  //Rounds
                            RssLink cat = new RssLink();
                            cat.Url = String.Format(@"http://xml.afl.com.au/RoundVideo.aspx?roundid={0}",
                                m.Groups["roundid"].Value);
                            cat.Name = m.Groups["title"].Value;
                            cat.Other = new Level() { level = Levels.RoundVideos };
                            cat.ParentCategory = parentCategory;
                            parentCategory.SubCategories.Add(cat);
                            m = m.NextMatch();
                        }
                        parentCategory.SubCategoriesDiscovered = true;
                        return parentCategory.SubCategories.Count;
                    }
                case Levels.VideoListTeam:
                    {
                        Match m = allVideosSubcats.Match(data);
                        while (m.Success)
                        {  //News...Community
                            RssLink cat = new RssLink();
                            cat.Url = m.Groups["url"].Value;
                            string[] urlparts = cat.Url.Split('/');
                            string videoTabId = urlparts[8];
                            string subTabId = urlparts[10];
                            //http://www.afl.com.au/tabid/164/feed/videosearch/videotabid/651/videosubtabid/654/default.aspx?feed=videosearch&videotabid=651&videosubtabid=0&page=1&mid=131673
                            //->
                            //http://www.afl.com.au/ajax.aspx?feed=VideoSearch&videoTabId=651&videoSubTabId=654&page=1&mid=131673

                            cat.Url = String.Format(@"http://www.afl.com.au/ajax.aspx?feed=VideoSearch&videoTabId={0}&videoSubTabId={1}&page=1&mid=131673",
                                videoTabId, subTabId);
                            cat.Name = m.Groups["title"].Value;
                            cat.ParentCategory = parentCategory;
                            cat.Other = String.Format(@"http://www.afl.com.au/ajax.aspx?feed=VideoSearch&videoTabId={0}&videoSubTabId={1}&page={{0}}&mid=131673",
                                videoTabId, subTabId);
                            parentCategory.SubCategories.Add(cat);
                            m = m.NextMatch();
                        }
                        parentCategory.SubCategoriesDiscovered = true;
                        return parentCategory.SubCategories.Count;
                    };
            }
            return 0;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            nextPageAvailable = false;
            nextPageUrl = String.Empty;
            string url = ((RssLink)category).Url;
            if (category.Other is Level)
            {
                Level level = (Level)category.Other;
                switch (level.level)
                {
                    case Levels.RoundVideos:
                        {
                            List<VideoInfo> videoList = new List<VideoInfo>();
                            XmlDocument doc = new XmlDocument();
                            doc.Load(url);
                            foreach (XmlNode vidNode in doc.SelectNodes(@"//round/matches/match/qualities/quality/periods/period"))
                            {
                                XmlNode matchNode = vidNode.ParentNode.ParentNode.ParentNode.ParentNode;
                                string nm = squads[matchNode.Attributes["homeSquadId"].Value] + " vs " +
                                    squads[matchNode.Attributes["awaySquadId"].Value] + ' ' + vidNode.Attributes["name"].Value;
                                VideoInfo video = videoList.Find(item => item.Title.Equals(nm, StringComparison.InvariantCultureIgnoreCase));
                                if (video == null)
                                {
                                    video = new VideoInfo();
                                    video.Title = nm;
                                    video.PlaybackOptions = new Dictionary<string, string>();
                                    video.Other = "livestream";//bypass genericsiteutil.geturl
                                    videoList.Add(video);
                                }
                                video.VideoUrl = vidNode.Attributes["url"].Value;
                                video.Airdate = matchNode.Attributes["dateTime"].Value;
                                video.PlaybackOptions.Add(vidNode.ParentNode.ParentNode.Attributes["name"].Value, vidNode.Attributes["url"].Value);
                            }
                            return videoList;
                        }
                }
            }

            if (category.Other is string)
            {
                //VideoListTeam
                nextPageRegExUrlFormatString = (string)category.Other;
                List<VideoInfo> res = base.getVideoList(category);

                return res;
            }
            return null;
        }

        public override Dictionary<string, string> GetPlaybackOptions(string playlistUrl)
        {
            Dictionary<string, string> res = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> kv in base.GetPlaybackOptions(playlistUrl))
                res.Add(kv.Key, kv.Value.Replace(@"\/", @"/"));
            return res;
        }

        private class Level
        {
            public string competitionId;
            public string seasonId;
            public Levels level;
        }
    }
}
