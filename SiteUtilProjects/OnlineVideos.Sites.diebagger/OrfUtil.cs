using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Net;
using System.IO;

namespace OnlineVideos.Sites
{
    public class OrfUtil : GenericSiteUtil
    {
        [Category("OnlineVideosUserConfiguration"), Description("How should categories be shown. Site will show them as they are presented on tvthek.orf.at, flat will show a flat list.")]
        CategoryShowTypes View = CategoryShowTypes.Site;

        private const String ORF_BASE = "http://tvthek.orf.at";

        public enum MediaQuality { Medium, High };
        public enum CategoryShowTypes { Site, Flat }

        public enum CategoryType { VOD, Live }

        private bool useAlternative = false;

        string categoryRegex = @"<option value=""(?<url>[^""]+)"">(?<title>[^<]+)</option>";
        string mainCategoryRegex = @"<h4>(?<title>.*?)</h4>(?<subcategories>.*?)</ul";
        string subCategoryRegex = @"<li>.*?<a href=""(?<url>.*?)"".*?>(?<title>.*?)</a>.*?</li>";

        string videolistRegex = @"</a></div>[^<]*<h3 class=""title"">.*?<span>(?<title>[^<]+-(?<date>[^<]+))</span>";
        string videolistRegex2 = @"<li.*?><a href.*?>(?<day>[^<]+)</a>(?<items>.*?)</ul>";
        string videolistRegex3 = @"<li><a href=""(?<url>[^""]+)"" title=""(?<alt>[^""]+)"">(?<title>[^<]+)</a>";
        string videolistRegex4 = @"<li><a href=""(?<url>[^""]+)"">(?<title>[^<]+)</a>";
        string playlistRegex = @"<div id=""btn_playlist""( style=""[^""]+"")?>.*?<a href=""(?<url>[^""]+)"" id=""open_playlist""";

        string liveRegexStreamRunning = @"<hr />.*?<a href=""(?<url>.*?)"".*?src=""(?<channelimg>.*?)"".*?<h3 class=""title"">.*?<span>(?<title>.*?)</span>.*?<video.*?poster=""(?<img>.*?)""";
        string liveRegexNoStreamRunning = @"<div id=""livestreamCommingSoon.*?<h3 class=""title"">.*?<span>(?<title>.*?)</span>.*?<img src=""(?<img>.*?)"".*?class=""live_comming_soon"" />.*?Der Livestream beginnt um (?<begintime>.*?)\..*?id=""more_livestreams"">";
        string liveRegexFurther = @"<li class=""vod"">.*?<a href=""(?<url>/live.*?)"".*?title=""(?<showtitle>.*?)"">.*?<img src=""(?<img>.*?)"".*?<strong>(?<title>.*?)</strong>";
        string liveRegexUpcoming = @"<li class=""vod"">.*?<div class=""live"">.*?<img src=""(?<img>.*?)"".*?title=""(?<title>.*?)"".*?<span class=""desc"">.*?:(?<date>.*?)</span>.*?</li>";
        string liveUrlRegex = @"<param name=""URL"" value=""(?<url>.*?)"" />";

        string videoAlternativeSourcesRegex = @"{\\""id\\"":(?<id>.*?),\\""title\\"":\\""(?<title>.*?)\\"",\\""sources\\"":{(?<source>.*?)}}}";
        string videoAlternativeFormatsRegex = @"\\""(?<type>[0-9].*?)_(?<typestring>.*?)\\"":{\\""src\\"":\\""(?<src>.*?)\\"",\\""quality_string\\"":\\""(?<qualitystring>.*?)\\"",\\""quality_pattern\\"":\\""(?<qualitypattern>.*?)\\""}";

        Regex regEx_Videolist;
        Regex regEx_Videolist2;
        Regex regEx_Videolist3;
        Regex regEx_Videolist4;
        Regex regEx_Playlist;
        Regex regEx_Categories;
        Regex regEx_MainCategories;
        Regex regEx_SubCategories;
        Regex regEx_LiveStreamRunning;
        Regex regEx_LiveNoStreamRunning;
        Regex regEx_LiveFurther;
        Regex regEx_LiveUpcoming;
        Regex regEx_LiveUrl;
        Regex regEx_VideoAlternativeSources;
        Regex regEx_VideoAlternativeFormats;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_Categories = new Regex(categoryRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regEx_MainCategories = new Regex(mainCategoryRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
            regEx_SubCategories = new Regex(subCategoryRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);

            regEx_LiveFurther = new Regex(liveRegexFurther, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
            regEx_LiveUpcoming = new Regex(liveRegexUpcoming, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
            regEx_LiveStreamRunning = new Regex(liveRegexStreamRunning, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
            regEx_LiveNoStreamRunning = new Regex(liveRegexNoStreamRunning, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
            regEx_LiveUrl = new Regex(liveUrlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant);

            regEx_Videolist = new Regex(videolistRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
            regEx_Videolist2 = new Regex(videolistRegex2, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
            regEx_Videolist3 = new Regex(videolistRegex3, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regEx_Videolist4 = new Regex(videolistRegex4, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regEx_Playlist = new Regex(playlistRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
            regEx_VideoAlternativeSources = new Regex(videoAlternativeSourcesRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
            regEx_VideoAlternativeFormats = new Regex(videoAlternativeFormatsRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            string data = GetWebData(ORF_BASE);

            //Add the url for upcoming livestreams
            RssLink catLive = new RssLink();
            catLive.Name = "[Live-Streams]";
            catLive.Url = ORF_BASE + "/live";
            catLive.HasSubCategories = false;
            catLive.Other = CategoryType.Live;
            Settings.Categories.Add(catLive);

            //Parse and add the url for archived videos
            if (View == CategoryShowTypes.Site)
            {
                Match c = regEx_MainCategories.Match(data);
                while (c.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Name = c.Groups["title"].Value;
                    cat.HasSubCategories = true;
                    cat.Other = CategoryType.VOD;
                    cat.SubCategories = new List<Category>();

                    Match c2 = regEx_SubCategories.Match(c.Groups["subcategories"].Value);
                    while (c2.Success)
                    {
                        String categoryUrl = ORF_BASE + c2.Groups["url"].Value;
                        String categoryTitle = c2.Groups["title"].Value;

                        RssLink sub = new RssLink();
                        sub.Name = categoryTitle;
                        sub.Url = categoryUrl;
                        sub.HasSubCategories = false;
                        sub.Other = CategoryType.VOD;
                        sub.ParentCategory = cat;

                        cat.SubCategories.Add(sub);
                        c2 = c2.NextMatch();
                    }
                    Settings.Categories.Add(cat);
                    c = c.NextMatch();
                }
            }
            else
            {
                Match c = regEx_Categories.Match(data);
                while (c.Success)
                {
                    Match c2 = regEx_Categories.Match(c.Value);
                    while (c2.Success)
                    {
                        String categoryUrl = c2.Groups["url"].Value;
                        String categoryTitle = c2.Groups["title"].Value;

                        RssLink cat = new RssLink();
                        cat.Name = categoryTitle;
                        cat.Url = categoryUrl;
                        cat.HasSubCategories = false;
                        cat.Other = CategoryType.VOD;

                        Settings.Categories.Add(cat);

                        c2 = c2.NextMatch();
                    }

                    c = c.NextMatch();
                }
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override List<String> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            Log.Info("Get multiple urls: " + video.VideoUrl);
            if ((CategoryType)video.Other == CategoryType.VOD)
            {
                string data = GetWebData(video.VideoUrl);
                if (useAlternative)
                {
                    /*
                     * Alternative video urls. The tvthek also streams for Android and iOS devices.
                     * Unfortunately, currently OV can't play these formats (rtsp and m3u)
                     * 
                     * Android: rtsp stream (low/medium -> 3gp, high -> mp4)
                     * iOS: http streaming (m3u)
                     */
                    List<String> returnList = new List<string>();
                    Match m1 = regEx_VideoAlternativeSources.Match(data);
                    while (m1.Success)
                    {
                        Match m2 = regEx_VideoAlternativeFormats.Match(m1.Groups["source"].Value);
                        while (m2.Success)
                        {
                            String source = m2.Groups["src"].Value.Replace("\\/", "/");
                            String quality = m2.Groups["qualitystring"].Value;
                            String typeString = m2.Groups["typestring"].Value;

                            if (typeString.Equals("apple_Q4A"))
                            {
                                returnList.Add(source);
                                break;
                            }

                            m2 = m2.NextMatch();
                        }
                        m1 = m1.NextMatch();
                    }

                    return returnList;
                }
                else
                {
                    /*
                     * The default video urls (mms stream)
                     */
                    string videoUrl = "";
                    if (!string.IsNullOrEmpty(data))
                    {
                        Match m = regEx_Playlist.Match(data);
                        if (m.Success)
                        {
                            videoUrl = m.Groups["url"].Value;
                            videoUrl = ORF_BASE + videoUrl;
                            return Utils.ParseASX(GetWebData(videoUrl));
                        }
                    }
                }
            }
            else
            {
                return base.GetMultipleVideoUrls(video, inPlaylist);
            }
            return null;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            Log.Info("Get url: " + video.VideoUrl);
            if ((CategoryType)video.Other == CategoryType.Live)
            {
                if (video.VideoUrl != null && !video.VideoUrl.Equals(""))
                {
                    string data = GetWebData(video.VideoUrl);
                    if (!string.IsNullOrEmpty(data))
                    {
                        Match l = regEx_LiveNoStreamRunning.Match(data);
                        if (l.Success)
                        {
                            //a live link that already has a playback link (detail page) but the stream hasn't yet started
                            throw new OnlineVideosException("Stream beginnt um " + l.Groups["begintime"].Value);
                        }
                        else
                        {
                            //a live link that is currently running
                            Match m = regEx_LiveUrl.Match(data);
                            if (m.Success)
                            {
                                String videoUrl = m.Groups["url"].Value;
                                return videoUrl;
                            }
                            else
                            {
                                //video should be running but for some reason the url extraction failed
                                Log.Warn("Couldn't retrieve playback url from " + video.VideoUrl);
                                throw new OnlineVideosException("Fehler beim starten des streams");
                            }
                        }
                    }
                }
                else
                {
                    if (video.Airdate != null)
                    {
                        //for live links that didn't yet have a playback link attached
                        throw new OnlineVideosException("Stream beginnt: " + video.Airdate);
                    }
                    else
                    {
                        //A stream with no playback link and no air date set, this shouldn't happen
                        throw new OnlineVideosException("Stream beginnt zu einem späteren Zeitpunk");
                    }
                }
            }
            return base.GetVideoUrl(video);
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            CategoryType type = (CategoryType)category.Other;

            if (type == CategoryType.VOD)
            {
                return GetVodVideoList(category);
            }
            else if (type == CategoryType.Live)
            {
                return GetLiveVideoList(category);
            }
            else
            {
                Log.Warn("Unknown category type");
                return null;
            }
        }

        private List<VideoInfo> GetLiveVideoList(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            string data = GetWebData((category as RssLink).Url);
            if (!string.IsNullOrEmpty(data))
            {
                if (data.Contains("<div id=\"livestreamCommingSoon\""))
                {
                    Match m = regEx_LiveNoStreamRunning.Match(data);
                    if (m.Success)
                    {
                        VideoInfo video = new VideoInfo();
                        //the video that is shown on front should have the same format
                        //as the videos shown in the "Weitere Livestreams" section
                        //(e.g. "Live: Burgenland heute, 19:00 Uhr")
                        video.Title = m.Groups["title"].Value + ", " + m.Groups["begintime"].Value;

                        video.Title = video.Title.Replace("&#160;", " ");

                        video.Thumb = m.Groups["img"].Value;
                        video.VideoUrl = (category as RssLink).Url;
                        video.Other = CategoryType.Live;

                        videos.Add(video);
                    }
                }
                else
                {
                    Match m = regEx_LiveStreamRunning.Match(data);
                    if (m.Success)
                    {
                        VideoInfo video = new VideoInfo();
                        video.Title = m.Groups["title"].Value;
                        video.Title = video.Title.Replace("&#160;", " ");

                        video.Thumb = m.Groups["img"].Value;
                        video.VideoUrl = ORF_BASE + m.Groups["url"].Value;
                        video.Other = CategoryType.Live;

                        videos.Add(video);
                    }
                }

                Match m2 = regEx_LiveFurther.Match(data);
                while (m2.Success)
                {
                    VideoInfo video = new VideoInfo();
                    video.Title = m2.Groups["title"].Value;
                    video.Title = video.Title.Replace("&#160;", " ");

                    video.Title2 = m2.Groups["showtitle"].Value;
                    video.Thumb = m2.Groups["img"].Value;
                    video.VideoUrl = ORF_BASE + m2.Groups["url"].Value;
                    video.Other = CategoryType.Live;

                    videos.Add(video);

                    m2 = m2.NextMatch();
                }

                Match m3 = regEx_LiveUpcoming.Match(data);
                while (m3.Success)
                {
                    VideoInfo video = new VideoInfo();
                    String date = m3.Groups["date"].Value.Replace("&#160;", " ").Trim();
                    video.Title = "Demnächst: " + m3.Groups["title"].Value + " (" + date + ")";
                    video.Airdate = date;
                    video.Description = m3.Groups["title"].Value;
                    video.Thumb = m3.Groups["img"].Value;
                    //video.VideoUrl = ORF_BASE + m2.Groups["url"].Value;
                    video.Other = CategoryType.Live;

                    videos.Add(video);

                    m3 = m3.NextMatch();
                }
            }
            return videos;
        }

        /// <summary>
        /// Returns the archive videos for the category. 
        /// </summary>
        /// <param name="category">Selected category</param>
        /// <returns>List of videos</returns>
        private List<VideoInfo> GetVodVideoList(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            string data = GetWebData((category as RssLink).Url);
            if (!string.IsNullOrEmpty(data))
            {

                String thumb = null;

                //gets the thumb of the current show. Disabled for now, as the image format is too wide
                /*Match i = new Regex(@"<span>Weitere Folgen</span>.*?<img src=""(?<img>.*?)"".*?/>.*?<div class=""scrollbox"">", RegexOptions.Singleline).Match(data);
                if (i.Success)
                {
                    thumb = i.Groups["img"].Value;
                }*/

                //the main video that is shown on the category site (the newest one available)
                Match m = regEx_Videolist.Match(data);
                if (m.Success)
                {
                    VideoInfo video = new VideoInfo();
                    video.Title = m.Groups["title"].Value;
                    video.Airdate = m.Groups["date"].Value;
                    video.Title = video.Title.Replace("&amp;", "&");
                    video.VideoUrl = (category as RssLink).Url;
                    video.Other = CategoryType.VOD;
                    video.Thumb = thumb;
                    videos.Add(video);
                }

                string cutOut = "";
                int idx1 = data.IndexOf("<span>Weitere Folgen</span>");
                if (idx1 > -1)
                {
                    int idx2 = data.IndexOf("</div>", idx1);
                    cutOut = data.Substring(idx1, idx2 - idx1);
                    if (!string.IsNullOrEmpty(cutOut))
                    {
                        Match c = regEx_Videolist2.Match(cutOut);
                        while (c.Success)
                        {
                            String day = c.Groups["day"].Value;
                            String items = c.Groups["items"].Value;

                            Match n = regEx_Videolist3.Match(items);
                            while (n.Success)
                            {
                                VideoInfo video = new VideoInfo();
                                video.Title = n.Groups["title"].Value;
                                video.VideoUrl = n.Groups["url"].Value;
                                video.VideoUrl = ORF_BASE + video.VideoUrl;
                                video.Airdate = day;
                                video.Other = CategoryType.VOD;
                                video.Thumb = thumb;
                                videos.Add(video);

                                n = n.NextMatch();
                            }
                            string prefix = "";
                            Match o = regEx_Videolist4.Match(items);
                            while (o.Success)
                            {
                                if (o.Groups["url"].Value.StartsWith("#"))
                                {
                                    prefix = o.Groups["title"].Value;
                                }
                                else
                                {
                                    VideoInfo video = new VideoInfo();
                                    video.Title = prefix + " " + HttpUtility.HtmlDecode(o.Groups["title"].Value) + ", " + day;
                                    video.Airdate = day;
                                    video.VideoUrl = o.Groups["url"].Value;
                                    video.VideoUrl = ORF_BASE + video.VideoUrl;
                                    video.Thumb = thumb;
                                    video.Other = CategoryType.VOD;

                                    //only add episode from the same show (e.g. ZiB 9, ZiB 13, ...)
                                    if (CompareEpisodes(videos[0].Title, video.Title))
                                    {
                                        videos.Add(video);
                                    }
                                }

                                o = o.NextMatch();
                            }

                            c = c.NextMatch();
                        }
                    }
                }

            }
            return videos;
        }

        /// <summary>
        /// Compares 2 episodes titles and returns true if it's the same show
        /// </summary>
        /// <param name="_ep1">Episode 1</param>
        /// <param name="_ep2">Episode 2</param>
        /// <returns>true if it's the same show</returns>
        private bool CompareEpisodes(string _ep1, string _ep2)
        {
            try
            {
                if (_ep1 != null && _ep2 != null && !_ep1.Equals("") && !_ep2.Equals(""))
                {
                    String show1 = _ep1.Remove(_ep1.ToLower().IndexOfAny(new char[] { ',', '-', ':' })).Trim();
                    String show2 = _ep2.Remove(_ep2.ToLower().IndexOfAny(new char[] { ',', '-', ':' })).Trim();

                    int croppedPos = GetPositionOfCrop(show1, show2);
                    if (croppedPos != -99)
                    {
                        show1 = show1.Remove(croppedPos);
                        show2 = show2.Remove(croppedPos);
                    }
                    if (show1.Equals(show2)) return true;
                }
            }
            catch (Exception) { }
            return false;
        }

        /// <summary>
        /// Some shows will have their names cropped by ... if they are too long, so if
        /// that happens only compare the string until their cropped position (e.g. "Frisch gekocht")
        /// </summary>
        /// <param name="_string1">String 1</param>
        /// <param name="_string2">String 2</param>
        /// <returns>The position of the string that got cropped most</returns>
        private int GetPositionOfCrop(string _string1, string _string2)
        {
            int croppedPos = -99;
            if (_string1.EndsWith("...")) croppedPos = _string1.IndexOf("...");
            if (_string2.EndsWith("..."))
            {
                int cropped2 = _string2.IndexOf("...");
                if (croppedPos == -99 || cropped2 < croppedPos) croppedPos = cropped2;
            }

            return croppedPos;
        }
    }
}