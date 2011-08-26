using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class TubePlusUtil : GenericSiteUtil
    {
        [Category("OnlineVideosConfiguration"), Description("")]
        protected string aZSubCategoriesRegEx;

        [Category("OnlineVideosConfiguration"), Description("")]
        protected string seriesSubCategoriesRegEx;

        [Category("OnlineVideosConfiguration"), Description("")]
        protected string seasonCategoriesRegEx;

        [Category("OnlineVideosConfiguration"), Description("")]
        protected string showVideoListRegEx;

        [Category("OnlineVideosConfiguration"), Description("")]
        protected string moviesVideoListRegEx;

        private RegexOptions defaultRegexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture;
        private Regex regex_AZSubCategories;
        private Regex regex_SeriesSubCategories;
        private Regex regex_SeasonCategories;
        private Regex regex_ShowVideoList;
        private Regex regex_MoviesVideoList;

        private enum Mode { Show, AZ, Series, Season, Movies, MoviesAz };

        private string userAgent = @"Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.0; Trident/4.0; SLCC1; .NET CLR 2.0.50727; Media Center PC 5.0; .NET CLR 3.5.30729; .NET CLR 3.0.30729; .NET4.0C; .NET4.0E)";

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regex_AZSubCategories = new Regex(aZSubCategoriesRegEx, defaultRegexOptions);
            regex_SeriesSubCategories = new Regex(seriesSubCategoriesRegEx, defaultRegexOptions);
            regex_SeasonCategories = new Regex(seasonCategoriesRegEx, defaultRegexOptions);
            regex_ShowVideoList = new Regex(showVideoListRegEx, defaultRegexOptions);
            regex_MoviesVideoList = new Regex(moviesVideoListRegEx, defaultRegexOptions);
        }

        public override int DiscoverDynamicCategories()
        {

            int res = base.DiscoverDynamicCategories();
            foreach (Category cat in Settings.Categories)
            {
                cat.HasSubCategories = true;
                if (cat.Name.ToLower().Contains("movies"))
                    cat.Other = Mode.Movies;
                else
                    cat.Other = Mode.Show;
            }
            return res;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            Regex re = null;
            switch ((Mode)parentCategory.Other)
            {
                case Mode.Show:
                case Mode.Movies: re = regex_AZSubCategories; break;
                case Mode.AZ:
                case Mode.MoviesAz: re = regex_SeriesSubCategories; break;
                case Mode.Series: re = regex_SeasonCategories; break;
            }

            string data = GetWebData((parentCategory as RssLink).Url, userAgent: userAgent);
            if (!string.IsNullOrEmpty(data))
            {
                parentCategory.SubCategories = new List<Category>();
                Match m = re.Match(data);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Url = m.Groups["url"].Value;
                    if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(baseUrl), cat.Url).AbsoluteUri;
                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim().Replace('\n', ' '));
                    cat.Thumb = m.Groups["thumb"].Value;
                    if (!String.IsNullOrEmpty(cat.Thumb) && !Uri.IsWellFormedUriString(cat.Thumb, System.UriKind.Absolute)) cat.Thumb = new Uri(new Uri(baseUrl), cat.Thumb).AbsoluteUri;
                    cat.Description = m.Groups["description"].Value;

                    cat.Other = (Mode)((int)parentCategory.Other + 1);
                    if ((Mode)parentCategory.Other == Mode.Series)
                    {
                        regEx_VideoList = regex_ShowVideoList;
                        string[] airdates = m.Groups["airdates"].Value.Replace(",", "||").Split(new[] { @"||" }, StringSplitOptions.RemoveEmptyEntries);

                        List<VideoInfo> videoList = Parse(null, m.Groups["urls"].Value);

                        for (int i = 0; i < videoList.Count && i + 1 < airdates.Length; i++)
                        {
                            string airdate = airdates[i + 1].Trim('"');
                            int p = airdate.LastIndexOf('_');
                            if (p < 0) p = 0;
                            airdate = airdate.Substring(p + 1);
                            if (!String.IsNullOrEmpty(airdate))
                                videoList[i].Length = '|' + Translation.Airdate + ": " + airdate;
                        }
                        cat.Other = videoList;
                    }
                    else
                        if ((Mode)parentCategory.Other != Mode.Movies)
                            cat.HasSubCategories = true;
                    cat.ParentCategory = parentCategory;
                    parentCategory.SubCategories.Add(cat);
                    m = m.NextMatch();
                }
                return parentCategory.SubCategories.Count;
            }
            return 0;
        }

        public override VideoInfo CreateVideoInfo()
        {
            return new TubePlusVideoInfo();
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            if (Mode.MoviesAz.Equals(category.Other))
            {
                regEx_VideoList = regex_MoviesVideoList;
                return base.getVideoList(category);
            }
            else
                return (List<VideoInfo>)category.Other;
        }

        public class TubePlusVideoInfo : VideoInfo
        {
            public override string GetPlaybackOptionUrl(string url)
            {
                return GetVideoUrl(base.PlaybackOptions[url]);
            }
        }

    }
}
