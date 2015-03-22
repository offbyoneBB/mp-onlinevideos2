using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class RedBull : BrightCoveUtil
    {
        [Category("OnlineVideosConfiguration")]
        protected string dynamicSubCategoriesSportsRegEx = null;

        private Regex regEx_dynamicSubCategoriesNormal;
        private Regex regEx_dynamicSubCategoriesSports;

        public override int DiscoverDynamicCategories()
        {
            regEx_dynamicSubCategoriesNormal = regEx_dynamicSubCategories;
            regEx_dynamicSubCategoriesSports = new Regex(dynamicSubCategoriesSportsRegEx, defaultRegexOptions);

            int res = base.DiscoverDynamicCategories();
            foreach (Category cat in Settings.Categories)
            {
                cat.Thumb = FixImageUrl(cat.Thumb);
                if (cat.Name == "Live")
                    cat.HasSubCategories = false;
            }
            return res;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (parentCategory.Name == "Sports")
                regEx_dynamicSubCategories = regEx_dynamicSubCategoriesSports;
            else
                regEx_dynamicSubCategories = regEx_dynamicSubCategoriesNormal;
            int res = base.DiscoverSubCategories(parentCategory);
            foreach (Category cat in parentCategory.SubCategories)
            {
                //if (cat is NextPageCategory) does not work! Webrequest transforms this back to "sports/" 
                //{
                //  ((NextPageCategory)cat).Url = ((NextPageCategory)cat).Url.Replace("sports/","sports%2F");
                //}
                if (parentCategory.Name == "Sports")
                    cat.HasSubCategories = true;
                cat.Thumb = FixImageUrl(cat.Thumb);
            }
            return res;
        }

        private string FixImageUrl(string url)
        {
            if (!String.IsNullOrEmpty(url))
            {
                if (url.StartsWith(@"https://api.redbull.tv/v1/images/http"))
                    url = HttpUtility.UrlDecode(url.Substring(33));
            }
            return url;
        }

        protected override void ExtraVideoMatch(VideoInfo video, GroupCollection matchGroups)
        {
            bool isLive = video.VideoUrl.StartsWith(@"http://live");
            if (isLive)
            {
                string start = video.Airdate;
                string end = matchGroups["AirdateEnd"].Value;
                DateTime dtStart, dtEnd;
                if (DateTime.TryParseExact(start, "yyyy-M-d-H-mm-ss", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out dtStart) &&
                    DateTime.TryParseExact(end, "yyyy-M-d-H-mm-ss", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out dtEnd))
                {
                    video.Airdate = dtStart.ToString();
                    video.Length = (dtEnd - dtStart).ToString();
                }
            }
            else
                if (matchGroups["Month"].Success && matchGroups["Date"].Success && String.IsNullOrEmpty(video.Airdate))
                {
                    video.Airdate = matchGroups["Month"].Value + ' ' + matchGroups["Date"].Value;
                }
            video.Thumb = FixImageUrl(video.Thumb);
        }
    }
}
