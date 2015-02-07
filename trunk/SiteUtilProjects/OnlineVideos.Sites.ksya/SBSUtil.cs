using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OnlineVideos.AMF;

namespace OnlineVideos.Sites
{
    public class SBSUtil : BrightCoveUtil
    {
        public override int ParseSubCategories(Category parentCategory, string data)
        {
            if (parentCategory is RssLink && regEx_dynamicSubCategories != null)
            {
                if (data == null)
                    data = GetWebData((parentCategory as RssLink).Url, cookies: GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
                if (!string.IsNullOrEmpty(data))
                {
                    List<Category> dynamicSubCategories = new List<Category>(); // put all new discovered Categories in a separate list
                    Match m = regEx_dynamicSubCategories.Match(data);
                    while (m.Success)
                    {
                        RssLink cat = new RssLink();
                        cat.Url = FormatDecodeAbsolutifyUrl(baseUrl, m.Groups["url"].Value, dynamicSubCategoryUrlFormatString, dynamicSubCategoryUrlDecoding);
                        cat.Name = Regex.Unescape(m.Groups["title"].Value.Trim());
                        cat.Thumb = m.Groups["thumb"].Value;
                        if (!String.IsNullOrEmpty(cat.Thumb) && !Uri.IsWellFormedUriString(cat.Thumb, System.UriKind.Absolute)) cat.Thumb = new Uri(new Uri(baseUrl), cat.Thumb).AbsoluteUri;
                        cat.Description = m.Groups["description"].Value;
                        cat.ParentCategory = parentCategory;
                        ExtraSubCategoryMatch(cat, m.Groups);
                        dynamicSubCategories.Add(cat);
                        m = m.NextMatch();
                    }
                    // discovery finished, copy them to the actual list -> prevents double entries if error occurs in the middle of adding
                    if (parentCategory.SubCategories == null) parentCategory.SubCategories = new List<Category>();
                    foreach (Category cat in dynamicSubCategories) parentCategory.SubCategories.Add(cat);
                    parentCategory.SubCategoriesDiscovered = dynamicSubCategories.Count > 0; // only set to true if actually discovered (forces re-discovery until found)
                    // Paging for SubCategories
                    if (parentCategory.SubCategories.Count > 0 && regEx_dynamicSubCategoriesNextPage != null)
                    {
                        m = regEx_dynamicSubCategoriesNextPage.Match(data);
                        if (m.Success)
                        {
                            string nextCatPageUrl = m.Groups["url"].Value;
                            if (!Uri.IsWellFormedUriString(nextCatPageUrl, System.UriKind.Absolute)) nextCatPageUrl = new Uri(new Uri(baseUrl), nextCatPageUrl).AbsoluteUri;
                            parentCategory.SubCategories.Add(new NextPageCategory() { Url = nextCatPageUrl, ParentCategory = parentCategory });
                        }
                    }
                }
                return parentCategory.SubCategories == null ? 0 : parentCategory.SubCategories.Count;
            }
            else
            {
                return base.DiscoverSubCategories(parentCategory);
            }
        }

        protected override List<VideoInfo> Parse(string url, string data)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();
            if (string.IsNullOrEmpty(data)) data = GetWebData(url, cookies: GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
            if (data.Length > 0)
            {
                if (regEx_VideoList != null)
                {
                    Match m = regEx_VideoList.Match(data);
                    while (m.Success)
                    {
                        VideoInfo videoInfo = CreateVideoInfo();
                        videoInfo.Title = Regex.Unescape(m.Groups["Title"].Value);
                        // get, format and if needed absolutify the video url
                        videoInfo.VideoUrl = FormatDecodeAbsolutifyUrl(url, m.Groups["VideoUrl"].Value, videoListRegExFormatString, videoListUrlDecoding);
                        // get, format and if needed absolutify the thumb url
                        if (!String.IsNullOrEmpty(m.Groups["ImageUrl"].Value))
                            videoInfo.Thumb = FormatDecodeAbsolutifyUrl(url, Regex.Unescape(m.Groups["ImageUrl"].Value), videoThumbFormatString, UrlDecoding.None);
                        videoInfo.Length = OnlineVideos.Utils.PlainTextFromHtml(m.Groups["Duration"].Value);
                        videoInfo.Airdate = OnlineVideos.Utils.PlainTextFromHtml(m.Groups["Airdate"].Value);
                        videoInfo.Description = m.Groups["Description"].Value;
                        ExtraVideoMatch(videoInfo, m.Groups);
                        videoList.Add(videoInfo);
                        m = m.NextMatch();
                    }
                    return videoList;
                }
            }

            return base.Parse(url, data);
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            Log.Info("SBSUtil: video.VideoUrl: " + video.VideoUrl);
            string[] parts = video.VideoUrl.Split('=');
            string vidId = parts[parts.Length - 1];
            Log.Debug("id: " + vidId);
            string url = @"http://embed.kijk.nl/?width=868&height=491&video=" + vidId;
            string webdata = GetWebData(url, referer: video.VideoUrl);

            //string webdataBody = Regex.Split(webdata, "<body")[1];

            Match m = regEx_FileUrl.Match(webdata);

            if (!m.Success)
                return String.Empty;

            AMFArray renditions = GetResultsFromViewerExperienceRequest(m, url);
            return FillPlaybackOptions(video, renditions);
        }

        //public override string getUrl(VideoInfo video)
        //{
        //    string[] parts = video.VideoUrl.Split('=');
        //    string vidId = parts[parts.Length - 1];
        //    Log.Debug("SBSUtil: geturl " + resultUrl);
        //    Log.Debug("id: " + vidId);
        //    string playUrl = vidIDtoStream(vidId);
        //    return playUrl;
        //}

        //public string vidIDtoStream(string id)
        //{
        //    string refUrl = String.Format("http://plus-api.sbsnet.nl/kijkframe.php?videoId={0}&width=868&height=488", id);
        //    string url = String.Format("http://embed.kijk.nl/?video={0}", id);

        //    string webdata = GetWebData(url, referer: refUrl);
        //    String myExp = Regex.Match(webdata, @"#myExperience([\d]*)").Groups[1].ToString();
        //    String playerID = Regex.Match(webdata, "<param name=\\\\\"playerID\\\\\" value=\\\\\"(.*)\\\\\" />").Groups[1].ToString();
        //    String playerKey = Regex.Match(webdata, "<param name=\\\\\"playerKey\\\\\" value=\\\\\"(.*)\\\\\" />").Groups[1].ToString();
        //    String videoPlayer = Regex.Match(webdata, "<param name=\\\\\"@videoPlayer\\\\\" value=\\\\\"(.*)\\\\\" />").Groups[1].ToString();

        //    string coveUrl = String.Format("http://c.brightcove.com/services/viewer/htmlFederated?&width=868&height=488&flashID=myExperience{0}&bgcolor=%23FFFFFF&playerID={1}&playerKey={2}&isVid=true&isUI=true&dynamicStreaming=true&wmode=opaque&%40videoPlayer={3}&branding=sbs&playertitle=true&templateReadyHandler=onTemplateReadySmartAPI&autoStart=&debuggerID=&refURL={4}", myExp, playerID, playerKey.Replace(",", "%2C"), videoPlayer, refUrl);
        //    Log.Debug("SBSUtil: BrightCove Url:: " + coveUrl);

        //    string coveWebdata = GetWebData(coveUrl, referer: url);
        //    String m3uUrl = Regex.Unescape(Regex.Match(coveWebdata, "\"defaultURL\":\"([^\"]+)\"").Groups[1].ToString());
        //    Log.Info("SBSUtil: m3uUrlt: " + m3uUrl);

        //    return m3uUrl;
        //}
    }
}
