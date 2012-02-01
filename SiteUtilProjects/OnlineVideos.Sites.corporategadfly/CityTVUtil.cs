using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using OnlineVideos.AMF;

namespace OnlineVideos.Sites
{
    public class CityTVUtil : GenericSiteUtil
    {

        private static string baseUrlPrefix = @"http://video.citytv.com";
        private static string mainCategoriesUrl = baseUrlPrefix + @"/video/navigation.htm?N=0&type=shows&sort=Display";
        private static string brightcoveUrl = @"http://c.brightcove.com/services/messagebroker/amf";
        private static string hashValue = @"87eb505819265047c9e68cd5351c94272f67266c";

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            HtmlDocument document = GetWebData<HtmlDocument>(mainCategoriesUrl);

            if (document.DocumentNode != null)
            {
                // extract categories from main URL
                foreach (RssLink category in ExtractMainCategories(document))
                {
                    Settings.Categories.Add(category);
                }

                HtmlNodeCollection paginationLinks = document.DocumentNode.SelectNodes("//div[@class='bar']//ul[@class='pagination']//a[@href]");

                if (paginationLinks != null)
                {
                    // extract main categories from pagination links
                    foreach (HtmlNode link in paginationLinks)
                    {
                        Log.Debug(@"Pagination link: {0}", link.Attributes["href"].Value);
                        document = GetWebData<HtmlDocument>(baseUrlPrefix + link.Attributes["href"].Value);
                        foreach (RssLink category in ExtractMainCategories(document))
                        {
                            Settings.Categories.Add(category);
                        }
                    }
                }
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        private List<RssLink> ExtractMainCategories(HtmlDocument document)
        {
            List<RssLink> result = new List<RssLink>();

            if (document.DocumentNode != null)
            {
                HtmlNodeCollection thumbDivs = document.DocumentNode.SelectNodes("//div[@class='shows']/div[@class='item']/div[@class='thumb']");
                if (thumbDivs != null)
                {
                    foreach (HtmlNode thumbDiv in thumbDivs)
                    {
                        HtmlNode imgNode = thumbDiv.SelectSingleNode("a/img");

                        RssLink cat = new RssLink();
                        cat.Thumb = imgNode.Attributes["src"].Value;
                        cat.Name = imgNode.Attributes["title"].Value;
                        cat.Url = baseUrlPrefix + thumbDiv.SelectSingleNode("a").Attributes["href"].Value;
                        cat.HasSubCategories = true;
                        result.Add(cat);
                    }
                }
                else
                {
                    Log.Info(@"No thumbnails found for top-level shows");
                }
            }

            return result;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();

            RssLink parentRssLink = (RssLink) parentCategory;
            HtmlDocument document = GetWebData<HtmlDocument>(parentRssLink.Url);

            if (document.DocumentNode != null)
            {
                HtmlNodeCollection topTabAnchors = document.DocumentNode.SelectNodes("//div[@class='tabs']/div/a");
                if (topTabAnchors != null)
                {
                    foreach (HtmlNode anchor in topTabAnchors)
                    {
                        RssLink cat = new RssLink()
                        {
                            Name = anchor.InnerText.Trim(),
                            HasSubCategories = false,
                            Url = parentRssLink.Url,
                            ParentCategory = parentCategory
                        };
                        parentCategory.SubCategories.Add(cat);
                    }
                }
                else
                {
                    Log.Info(@"No top tab anchors found!");
                }
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            string selectorId = string.Empty;
            switch (category.Name)
            {
                case "Full Episodes":
                    selectorId = "episodes";
                    break;
                case "Video Clips":
                    selectorId = "clips";
                    break;
                default:
                    break;
            }

            RssLink rssLink = (RssLink) category;
            HtmlDocument document = GetWebData<HtmlDocument>(rssLink.Url);

            if (document.DocumentNode != null)
            {
                HtmlNodeCollection videoItems = document.DocumentNode.SelectNodes("//div[@id='" + selectorId + "']//div[@class='item']");

                if (videoItems != null)
                {
                    foreach (HtmlNode videoItem in videoItems)
                    {
                        VideoInfo video = new VideoInfo();
                        HtmlNode metaDiv = videoItem.SelectSingleNode(@"div[@class='meta']");
                        HtmlNode heading1 = metaDiv.SelectSingleNode(@"h1");
                        video.Length = heading1.SelectSingleNode(@"span").InnerText;
                        video.Title = metaDiv.SelectSingleNode(@"div/strong").InnerText + " - " + heading1.SelectSingleNode(@"a").InnerText;
                        video.Description = metaDiv.SelectSingleNode(@"p").InnerText;
                        video.Airdate = metaDiv.SelectSingleNode(@"h5").InnerText;
                        video.ThumbnailImage = videoItem.SelectSingleNode(@".//img").Attributes["src"].Value;
                        video.VideoUrl = baseUrlPrefix + heading1.SelectSingleNode(@"a[@href]").Attributes["href"].Value;

                        result.Add(video);
                    }
                }
            }

            return result;
        }

        public override string getUrl(VideoInfo video)
        {
            /*
               sample AMF request (expressed in JSON for clarity - captured using Flashbug in Firebug)
               [
                {"targetURI":"com.brightcove.experience.ExperienceRuntimeFacade.getDataForExperience",
                 "responseURI":"/1",
                 "length":"443 B",
                 "data":["87eb505819265047c9e68cd5351c94272f67266c",
                    {"contentOverrides":[
                        {"contentType":0,
                         "target":"videoPlayer",
                         "contentId":1400506603489,
                         "contentRefId":null,
                         "featuredRefId":null,
                         "contentRefIds":null,
                         "featuredId":null,
                         "contentIds":null,
                         "__traits":
                            {"type":"com.brightcove.experience.ContentOverride",
                            "members":["contentType","target","contentId","contentRefId","featuredRefId","contentRefIds","featuredId","contentIds"],
                            "count":8,
                            "externalizable":false,
                            "dynamic":false}}],
                         "URL":"http://video.citytv.com/video/detail/1400506604001.000000/little-bo-bleep/",
                         "playerKey":"",
                         "experienceId":897759285745,
                         "TTLToken":"",
                         "deliveryType":null,
                         "__traits":
                            {"type":"com.brightcove.experience.ViewerExperienceRequest",
                             "members":["contentOverrides","URL","playerKey","experienceId","TTLToken","deliveryType"],
                             "count":6,
                             "externalizable":false,
                             "dynamic":false}}]}]
             */

            string result = string.Empty;
            
            HtmlDocument document = GetWebData<HtmlDocument>(video.VideoUrl);

            if (document != null)
            {
                HtmlNode brightcoveExperience = document.DocumentNode.SelectSingleNode(@"//object[@class='BrightcoveExperience']");
                string playerId = brightcoveExperience.SelectSingleNode(@"./param[@name='playerID']").GetAttributeValue("value", "");
                string videoId = brightcoveExperience.SelectSingleNode(@"./param[@name='@videoPlayer']").GetAttributeValue("value", "");
                Log.Debug("PlayerId: {0}, VideoId: {1}", playerId, videoId);

                // content override
                AMFObject contentOverride = new AMFObject(@"com.brightcove.experience.ContentOverride");
                contentOverride.Add("contentId", videoId);
                contentOverride.Add("contentIds", null);
                contentOverride.Add("contentRefId", null);
                contentOverride.Add("contentRefIds", null);
                contentOverride.Add("contentType", 0);
                contentOverride.Add("featuredId", double.NaN);
                contentOverride.Add("featuredRefId", null);
                contentOverride.Add("target", "videoPlayer");
                AMFArray contentOverrideArray = new AMFArray();
                contentOverrideArray.Add(contentOverride);

                // viewer experience request
                AMFObject viewerExperenceRequest = new AMFObject(@"com.brightcove.experience.ViewerExperienceRequest");
                viewerExperenceRequest.Add("contentOverrides", contentOverrideArray);
                viewerExperenceRequest.Add("experienceId", playerId);
                viewerExperenceRequest.Add("deliveryType", null);
                viewerExperenceRequest.Add("playerKey", string.Empty);
                viewerExperenceRequest.Add("URL", video.VideoUrl);
                viewerExperenceRequest.Add("TTLToken", string.Empty);

                AMFSerializer serializer = new AMFSerializer();
                AMFObject response = AMFObject.GetResponse(brightcoveUrl, serializer.Serialize(viewerExperenceRequest, hashValue));

                AMFArray renditions = response.GetArray("programmedContent").GetObject("videoPlayer").GetObject("mediaDTO").GetArray("renditions");

                video.PlaybackOptions = new Dictionary<string, string>();

                for (int i = 0; i < renditions.Count; i++)
                {
                    AMFObject rendition = renditions.GetObject(i);
                    string optionKey = String.Format("{0}x{1} {2}K",
                        rendition.GetIntProperty("frameWidth"),
                        rendition.GetIntProperty("frameHeight"),
                        rendition.GetIntProperty("encodingRate") / 1024);
                    string url = HttpUtility.UrlDecode(rendition.GetStringProperty("defaultURL"));
                    Log.Debug("Option: {0} URL: {1}", optionKey, url);

                    if (url.StartsWith("rtmp"))
                    {
                        string[] parts = url.Split('&');
                        url = new MPUrlSourceFilter.RtmpUrl(parts[0]) { PlayPath = parts[1] }.ToString();
                    }
                    video.PlaybackOptions.Add(optionKey, url);
                }

                if (video.PlaybackOptions.Count > 0)
                {
                    if (video.PlaybackOptions.Count == 1)
                    {

                        result = video.PlaybackOptions.Last().Value;
                        // only one URL found, so PlaybackOptions not needed
                        video.PlaybackOptions = null;
                    }
                    else
                    {
                        // last value will be selected
                        result = video.PlaybackOptions.Last().Value;
                    }
                }
            }

             return result;
        }
    }
}
