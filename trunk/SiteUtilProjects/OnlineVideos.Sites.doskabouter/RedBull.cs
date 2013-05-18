using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OnlineVideos.AMF;

namespace OnlineVideos.Sites
{
    public class RedBull : BrightCoveUtil
    {
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for videos. Group names: 'VideoUrl', 'ImageUrl', 'Title', 'Duration', 'Description', 'Airdate'.")]
        protected string videoListRegExLive;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for the playback url. Groups should be named 'm0', 'm1' and so on for the url. Multiple matches will be presented as playback choices. The name of a choice will be made of the result of groups named 'n0', 'n1' and so on.")]
        protected string fileUrlRegExLive;
        [Category("OnlineVideosConfiguration"), Description("HashValue")]
        protected string hashValueLive = null;
        [Category("OnlineVideosConfiguration"), Description("Url for request")]
        protected string requestUrlLive = null;

        private Regex regEx_VideoListShow;
        private Regex regEx_VideoListLive;
        private Regex regEx_FileUrlLive;
        private Regex regEx_FileUrlShow;

        private string hashValueShow;
        private string requestUrlShow;

        public override int DiscoverDynamicCategories()
        {
            regEx_VideoListShow = regEx_VideoList;
            regEx_VideoListLive = new Regex(videoListRegExLive, defaultRegexOptions);

            regEx_FileUrlShow = regEx_FileUrl;
            regEx_FileUrlLive = new Regex(fileUrlRegExLive, defaultRegexOptions);

            hashValueShow = hashValue;
            requestUrlShow = requestUrl;

            int res= base.DiscoverDynamicCategories();
            foreach (Category cat in Settings.Categories)
            {
                if (cat.SubCategories != null && cat.SubCategories.Count >0)
                {
                    cat.SubCategoriesDiscovered = true;
                    foreach (Category subCat in cat.SubCategories)
                    {
                        subCat.HasSubCategories = true;
                    }
                }
                else
                    cat.HasSubCategories = false;
            }
            return res;
        }

        protected override void ExtraVideoMatch(VideoInfo video, GroupCollection matchGroups)
        {
            bool isLive = video.VideoUrl.StartsWith(@"http://live");
            if (isLive)
            {
                string start = video.Airdate;
                string end = matchGroups["AirdateEnd"].Value;
                DateTime dtStart,dtEnd;
                if (DateTime.TryParseExact(start, "yyyy-M-d-H-mm-ss", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out dtStart) &&
                    DateTime.TryParseExact(end, "yyyy-M-d-H-mm-ss", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out dtEnd))
                {
                    video.Airdate = dtStart.ToString();
                    video.Length = (dtEnd - dtStart).ToString();
                }
            }
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string url = ((RssLink)category).Url;
            bool isLive = url.StartsWith(@"http://live");
            regEx_VideoList = isLive ? regEx_VideoListLive : regEx_VideoListShow;
            return base.getVideoList(category);
        }

        public override string getUrl(VideoInfo video)
        {
            bool isLive = video.VideoUrl.StartsWith(@"http://live");
            if (!isLive)
            {
                hashValue = hashValueShow;
                requestUrl = requestUrlShow;
                return base.getUrl(video);
            }
            hashValue = hashValueLive;
            requestUrl = requestUrlLive;

            string data = GetWebData(video.VideoUrl);
            Match m = regEx_FileUrlLive.Match(data);

            if (!m.Success)
                return String.Empty;

            AMFArray renditions = GetResultsFromViewerExperienceRequest(m, video.VideoUrl);
            string s = FillPlaybackOptions(video, renditions);
            if (video.PlaybackOptions != null)
            {
                Dictionary<string, string> newOptions = new Dictionary<string, string>();
                foreach (string key in video.PlaybackOptions.Keys)
                    newOptions.Add(key,Patch(video.PlaybackOptions[key], m.Groups["contentId"].Value));
                video.PlaybackOptions = newOptions;
            }
            return Patch(s, m.Groups["contentId"].Value);
        }

        private string Patch(string url, string contentId)
        {
            return String.Format(@"{0}?videoId={1}&lineUpId=&affiliateId=&v=2.11.3&fp=WIN%2011%2C7%2C700%2C169&r=XXXXX&g=XXXXXXXXXXXX&bandwidthEstimationTest=true", url, contentId);
        }
    }
}
