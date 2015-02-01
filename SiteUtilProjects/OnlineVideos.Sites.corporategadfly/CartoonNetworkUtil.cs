using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using OnlineVideos.AMF;
using OnlineVideos.MPUrlSourceFilter;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// site util for Cartoon Network
    /// </summary>
    public class CartoonNetworkUtil : GenericSiteUtil
    {
        private static string brightcoveUrl = @"http://c.brightcove.com/services/messagebroker/amf?playerKey=AQ~~,AAAAAG-l79c~,cgoFxi2dfuXdUNYnjFGieoNUUK85Kyae";
        private static string hashValue = @"1ddf0ed58803c0533b0f82a8ff68ae50e0e12f52";
        private static string playerId = @"1706768404001";
        private static string publisherId = @"1873145815";
        
        private static Regex rtmpUrlRegex = new Regex(@"(?<rtmp>rtmpe?)://(?<host>[^/]+)/(?<app>[^&]+)&(?<leftover>.+)",
                                                      RegexOptions.Compiled);

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            string webData = GetWebData(baseUrl);

            if (!string.IsNullOrEmpty(webData))
            {
                foreach (Match m in regEx_dynamicCategories.Matches(webData))
                {
                    RssLink cat = new RssLink() {
                        Name = m.Groups["title"].Value,
                        Url = m.Groups["url"].Value,
                        Thumb = m.Groups["thumb"].Value,
                        HasSubCategories = false
                    };

                    Settings.Categories.Add(cat);
                }
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }
        
        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> result = new List<VideoInfo>();

            /*
             sample AMF input for findPagingMediaCollectionByReferenceId (expressed in pretty-printed JSON for clarity - captured using Firebug)
            
                ["1ddf0ed58803c0533b0f82a8ff68ae50e0e12f52",1706768404001,"cn-johnny-test-playlist-clip",0,50,1873145815]
            */
           
            object[] amfInput = new object[6];
            amfInput[0] = hashValue;
            amfInput[1] = Convert.ToDouble(playerId);
            amfInput[2] = string.Format(@"cn-{0}-playlist-clip", ((RssLink) category).Url);
            amfInput[3] = Convert.ToDouble(@"0");
            amfInput[4] = Convert.ToDouble(@"50");
            amfInput[5] = Convert.ToDouble(publisherId);
            
            AMFSerializer serializer = new AMFSerializer();
            AMFObject response = AMFObject.GetResponse(brightcoveUrl, serializer.Serialize2("com.brightcove.player.runtime.PlayerMediaFacade.findPagingMediaCollectionByReferenceId", amfInput));

            //Log.Debug("AMF Response: {0}", response.ToString());
            string lineUpId = response.GetDoubleProperty("id").ToString();
            Log.Debug("LineUpId: {0}", lineUpId);
            
            AMFArray videoDTOs = response.GetArray("videoDTOs");
            
            if (videoDTOs != null)
            {
                for (int i = 0; i < videoDTOs.Count; i++)
                {
                    AMFObject videoDTO = videoDTOs.GetObject(i);
                    
                    string url = videoDTO.GetStringProperty("FLVFullLengthURL");
                    
                    // typical URL from FLVFullLengthURL
                    // rtmpe://cp102794.edgefcs.net/ondemand/&tv/johnnyTest/video/johnnyTest_clip2_eps19_en
                    //
                    // following rtmpdump style command works
                    // rtmpdump 
                    // --rtmp 'rtmpe://cp102794.edgefcs.net:1935/ondemand?videoId=3066558001&lineUpId=1664607936001&pubId=1873145815&playerId=1706768404001&affiliateId='
                    // --flv 'johnnyTest_clip2_eps19_en.mp4'
                    // --playpath 'tv/johnnyTest/video/johnnyTest_clip2_eps19_en?videoId=3066558001&lineUpId=1664607936001&pubId=1873145815&playerId=1706768404001&affiliateId='
                    
                    Match rtmpUrlMatch = rtmpUrlRegex.Match(url);
                    
                    if (rtmpUrlMatch.Success)
                    {
                        string videoId = videoDTO.GetDoubleProperty("id").ToString();
                        string query = string.Format(@"videoId={0}&lineUpId={1}&pubId={2}&playerId={3}&affiliateId=",
                                                     videoId, lineUpId, publisherId, playerId);
                        string rtmpUrl = String.Format("{0}://{1}/{2}?{3}",
                                                       rtmpUrlMatch.Groups["rtmp"].Value,
                                                       rtmpUrlMatch.Groups["host"].Value,
                                                       rtmpUrlMatch.Groups["app"].Value,
                                                       query);
                        string playPath = string.Format(@"{0}?{1}", rtmpUrlMatch.Groups["leftover"].Value, query);
                        Log.Debug(@"RTMP URL: {0}, playPath: {1}", rtmpUrl, playPath);
                        
                        VideoInfo video = new VideoInfo() {
                            Title = videoDTO.GetStringProperty("displayName"),
                            ImageUrl = videoDTO.GetStringProperty("thumbnailURL"),
                            Description = videoDTO.GetStringProperty("longDescription"),
                            Length = TimeSpan.FromSeconds(videoDTO.GetDoubleProperty("length")/1000).ToString(),
                            VideoUrl = new RtmpUrl(rtmpUrl) {
                                PlayPath = playPath
                            }.ToString()
                        };
                        
                        result.Add(video);
                    }
                    
                }
            }
            else
            {
                Log.Error(@"No videos found for {0}", category.Name);
            }
            return result;
        }
        
        public override string GetVideoUrl(VideoInfo video)
        {
            // override base getUrl method which was mangling the rtmp URL
            //      incoming URL: rtmpe://cp102794.edgefcs.net####Url=
            //      outgoing URL: rtmpe://cp102794.edgefcs.net/#%23%23%23Url=
            return video.VideoUrl;
        }
    }
}
