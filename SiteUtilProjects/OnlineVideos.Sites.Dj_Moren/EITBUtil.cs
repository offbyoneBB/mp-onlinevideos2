using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Net;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using OnlineVideos.AMF;
using OnlineVideos.MPUrlSourceFilter;

namespace OnlineVideos.Sites
{
    public class EITBUtil : BrightCoveUtil
    {

        [Category("OnlineVideosConfiguration")]
        protected string submenu2RegEx;
        [Category("OnlineVideosConfiguration")]
        protected string noSubmenuRegEx;
        [Category("OnlineVideosConfiguration")]
        protected string submenu3RegEx;
        [Category("OnlineVideosConfiguration")]
        protected string submenu4RegEx;
        [Category("OnlineVideosConfiguration")]
        protected string submenuRegEx;

        [Category("OnlineVideosConfiguration")]
        protected string tipoSubmenu2Programas;
        [Category("OnlineVideosConfiguration")]
        protected string tipoSubmenu2Categorias;

        [Category("OnlineVideosConfiguration")]
        protected string menu1;
        [Category("OnlineVideosConfiguration")]
        protected string menu2;
        [Category("OnlineVideosConfiguration")]
        protected string menu3;
        [Category("OnlineVideosConfiguration")]
        protected string menu4;
        [Category("OnlineVideosConfiguration")]
        protected string menu5;
        [Category("OnlineVideosConfiguration")]
        protected string menu6;
        
        internal enum CategoryType
        {
            None,
            submenu2,
            submenu3,
            submenu4,
            nosubmenu,
            submenu
        }

        private String replaceRegExp(String s)
        {
            return s.Replace("\\","\\\\")
                .Replace(" ", "\\s")
                .Replace("(","\\(")
                .Replace(")","\\)")
                .Replace("[","\\[")
                .Replace("]","\\]")
                .Replace("^","\\^")
                .Replace("$","\\$")
                .Replace(".","\\.")
                .Replace("|","\\|")
                .Replace("?","\\?")
                .Replace("*","\\*")
                .Replace("+","\\+")
                .Replace("#","\\#");
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            // GET /es/get/playlist/676435921001/ HTTP/1.1
            List<VideoInfo> videoList = new List<VideoInfo>();
            String data = GetWebData(baseUrl + "get/playlist/" + (category as RssLink).Url + "/");
            JObject jsonEpisodios = JObject.Parse(data);
            JArray episodios = (JArray)jsonEpisodios["videos"];
            foreach (JToken episodio in episodios)
            {
                VideoInfo video = new VideoInfo();
                JObject customFields = JObject.Parse(episodio["customFields"].ToString());
                video.Title = (String)customFields.SelectToken("name_c");
                video.Title2 = (String)episodio.SelectToken("name");
                video.Description = (String)customFields.SelectToken("shortdescription_c");
                video.Airdate = ConvertFromUnixTimestamp(double.Parse((String)episodio.SelectToken("publishedDate"))).ToString("dd-MM-yyyy HH:mm:ss");
                TimeSpan ts = TimeSpan.FromMilliseconds(double.Parse(episodio.SelectToken("length").ToString()));
                TimeSpan tsAux = TimeSpan.FromMilliseconds(ts.Milliseconds);
                video.Length = ts.Subtract(tsAux).ToString();
                video.ImageUrl = (String)episodio.SelectToken("thumbnailURL");
                video.VideoUrl = baseUrl + "#/video/" + episodio.SelectToken("id").ToString();
                videoList.Add(video);
            }
            return videoList;
        }

        static DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddMilliseconds(timestamp);
        }
               
        public override int DiscoverSubCategories(Category parentCategory)
        {
            List<Category> subCategories = null;

            switch ((CategoryType)parentCategory.Other)
            {
                case CategoryType.submenu2:
                    if (parentCategory.Name == menu1)
                    {
                        regEx_dynamicSubCategories = new Regex(submenu2RegEx.Replace("tipoSubMenu2", tipoSubmenu2Programas), defaultRegexOptions);
                    }
                    else if (parentCategory.Name == menu2)
                    {
                        regEx_dynamicSubCategories = new Regex(submenu2RegEx.Replace("tipoSubMenu2", tipoSubmenu2Categorias), defaultRegexOptions);
                    }
                    break;
                case CategoryType.submenu3:
                    regEx_dynamicSubCategories = new Regex(submenu3RegEx.Replace("tipoSubmenu3Menu1", replaceRegExp(parentCategory.ParentCategory.Name))
                    .Replace("tipoSubmenu3Menu2", replaceRegExp(parentCategory.Name.Replace("Ñ", "ñ")))
                    .Replace("tipoNombre", parentCategory.ParentCategory.Name == menu1 ? "temporada" : "programa"), defaultRegexOptions);
                    break;
                case CategoryType.submenu4:
                    regEx_dynamicSubCategories = new Regex(submenu4RegEx.Replace("tipoSubmenu3Menu1", replaceRegExp(parentCategory.ParentCategory.ParentCategory.Name))
                    .Replace("tipoSubmenu3Menu2", replaceRegExp(parentCategory.ParentCategory.Name))
                    .Replace("tipoSubmenu4", replaceRegExp(parentCategory.Name)), defaultRegexOptions);
                    break;
                case CategoryType.submenu:
                    regEx_dynamicSubCategories = new Regex(submenuRegEx.Replace("tipoSubmenu", replaceRegExp(parentCategory.Name.ToLower())), defaultRegexOptions);
                    break;
            }
            subCategories = DiscoverSubmenu(parentCategory as RssLink, regEx_dynamicSubCategories);

            parentCategory.SubCategories = subCategories;
            parentCategory.SubCategoriesDiscovered = true;
            parentCategory.HasSubCategories = subCategories == null ? false : subCategories.Count > 0;

            return parentCategory.HasSubCategories ? subCategories.Count : 0;
        }

        internal List<Category> DiscoverSubmenu(RssLink parentCategory, Regex regexp)
        {
            List<Category> result = new List<Category>();
            String data = GetWebData(baseUrl);
            Match match = regexp.Match(data);
            while (match.Success)
            {
                String name = match.Groups["title"].Value;
                if (parentCategory.Name == menu1)
                {
                    name = name.ToUpper();
                }
                String url = match.Groups["url"].Value;
                result.Add(CreateCategory(name, url, null, getCategoria(parentCategory, url), "", parentCategory));
                match = match.NextMatch();
            }
            return result;
        }

        internal CategoryType getCategoria(RssLink parentCategory, String url)
        {
            if (parentCategory.Other.Equals(CategoryType.submenu2))
            {
                return CategoryType.submenu3;
            }
            else if (parentCategory.Other.Equals(CategoryType.submenu3) && url == "")
            {
                return CategoryType.submenu4;
            }
            return CategoryType.None;
        }

        internal RssLink CreateCategory(String name, String url, String thumbUrl, CategoryType categoryType, String description, Category parentCategory)
        {
            RssLink category = new RssLink();

            category.Name = name;
            category.Url = url;
            category.Thumb = thumbUrl;
            category.Other = categoryType;
            category.Description = description;
            category.HasSubCategories = categoryType != CategoryType.None;
            category.SubCategoriesDiscovered = false;
            category.ParentCategory = parentCategory;

            return category;
        }

        public override int DiscoverDynamicCategories()
        {
            foreach (Category c in Settings.Categories){
                if (c.Name == menu1 || c.Name == menu2)
                {
                    c.Other = CategoryType.submenu2;
                    c.HasSubCategories = true;
                }
                else if (c.Name == menu3 || c.Name == menu4 || c.Name == menu6)
                {
                    c.Other = CategoryType.nosubmenu;
                    regEx_dynamicSubCategories = new Regex(noSubmenuRegEx.Replace("tipoNoSubmenu", replaceRegExp(c.Name)), defaultRegexOptions);
                    List<Category> subCategories = DiscoverSubmenu(c as RssLink, regEx_dynamicSubCategories);
                    (c as RssLink).Url = (subCategories.ElementAt(0) as RssLink).Url;
                    c.HasSubCategories = false;
                }
                else if (c.Name == menu5)
                {
                    c.Other = CategoryType.submenu;
                    c.HasSubCategories = true;
                }
            }
            Settings.DynamicCategoriesDiscovered = true;

            return Settings.Categories.Count;
        }


        //********************************************************************************************************************************
        //****************************************************** Brightcove util *********************************************************
        //********************************************************************************************************************************

        public override string getUrl(VideoInfo video)
        {
            string webdata = GetWebData(video.VideoUrl);
            return GetFileUrl(video, webdata);
        }

        protected new string GetFileUrl(VideoInfo video, string data)
        {
            Match m = regEx_FileUrl.Match(data);

            if (!m.Success)
                return String.Empty;

            AMFArray renditions = GetResultsFromFindByMediaId(m, video.VideoUrl);

            return FillPlaybackOptions(video, renditions, m);
        }
              

        protected AMFArray GetResultsFromFindByMediaId(Match m, string videoUrl)
        {
            AMFSerializer ser = new AMFSerializer();
            object[] values = new object[4];
            values[0] = hashValue;
            values[1] = Convert.ToDouble(m.Groups["experienceId"].Value);
            values[2] = Convert.ToDouble(videoUrl.Substring(videoUrl.LastIndexOf("/") + 1));
            values[3] = Convert.ToDouble(array4);
            byte[] data = ser.Serialize2("com.brightcove.player.runtime.PlayerMediaFacade.findMediaById", values);
            AMFObject obj = AMFObject.GetResponse(requestUrl + m.Groups["playerKey"].Value, data);
            return obj.GetArray("renditions");
        }

        protected new string FillPlaybackOptions(VideoInfo video, AMFArray renditions, Match m)
        {
            video.PlaybackOptions = new Dictionary<string, string>();

            foreach (AMFObject rendition in renditions.OrderBy(u => u.GetIntProperty("encodingRate")))
            {
                string nm = String.Format("{0}x{1} {2}K",
                    rendition.GetIntProperty("frameWidth"), rendition.GetIntProperty("frameHeight"),
                    rendition.GetIntProperty("encodingRate") / 1024);
                string url = HttpUtility.UrlDecode(rendition.GetStringProperty("defaultURL")); //"rtmp://brightcove.fcod.llnwd.net/a500/e1/uds/rtmp/ondemand/&mp4:102076681001/102076681001_986209826001_26930-20110610-122117.mp4&1368558000000&0aa762d184c16de09a21fe533394c3ea"
                if (url.StartsWith("rtmp"))
                {
                    //tested with ztele
                    string auth = String.Empty;
                    if (url.Contains('?'))
                        auth = '?' + url.Split('?')[1];
                    string[] parts = url.Split('&');

                    string rtmp = parts[0] + auth; //"rtmp://brightcove.fcod.llnwd.net/a500/e1/uds/rtmp/ondemand/"
                    string playpath = parts[1].Split('?')[0] + auth; //"mp4:102076681001/102076681001_986209826001_26930-20110610-122117.mp4"
                    if (url.IndexOf("edgefcs.net") != -1)
                    {
                        /*rtmpdump --rtmp "rtmp://cp150446.edgefcs.net/ondemand/&mp4:102076681001/102076681001_1506435728001_66034-20120314-120404.mp4?__nn__=1497926354001&slist=102076681001/&auth=daEbVc2bZd2bpcZdcbxbVdld6cEdWcpb4dC-brKM2q-bWG-rnBBssvx_ABAo_DDCB_GuD&aifp=bcosuds" --app="ondemand?__nn__=1497926354001&slist=102076681001/&auth=daEbVc2bZd2bpcZdcbxbVdld6cEdWcpb4dC-brKM2q-bWG-rnBBssvx_ABAo_DDCB_GuD&aifp=bcosuds&videoId=1506302142001&lineUpId=&pubId=102076681001&playerId=2202962695001" --swfUrl="http://admin.brightcove.com/viewer/us20121213.1025/federatedVideoUI/BrightcovePlayer.swf?uid=1355746343102" --playpath="mp4:102076681001/102076681001_1506435728001_66034-20120314-120404.mp4?__nn__=1497926354001&slist=102076681001/&auth=daEbVc2bZd2bpcZdcbxbVdld6cEdWcpb4dC-brKM2q-bWG-rnBBssvx_ABAo_DDCB_GuD&aifp=bcosuds&videoId=1506302142001" --pageUrl="http://www.eitb.tv/es/#/video/1506302142001" -o "Aduriz-La_cocina_de_las_palabras_-_Aduriz-Hitzen_sukaldea-.mp4"*/
                        url = new MPUrlSourceFilter.RtmpUrl(rtmp) { PlayPath = playpath }.ToString();
                    }
                    else
                    {
                        /*
                         rtmpdump --rtmp "rtmp://brightcove.fcod.llnwd.net/a500/e1/uds/rtmp/ondemand/&mp4:102076681001/102076681001_986252687001_26930-20110610-122117.mp4&1368558000000&0aa762d184c16de09a21fe533394c3ea" --app="a500/e1/uds/rtmp/ondemand?videoId=986121629001&lineUpId=&pubId=102076681001&playerId=2202962695001" --swfUrl="http://admin.brightcove.com/viewer/us20121218.1107/federatedVideoUI/BrightcovePlayer.swf?uid=1355158765470" --playpath="mp4:102076681001/102076681001_986252687001_26930-20110610-122117.mp4?videoId=986121629001&lineUpId=&pubId=102076681001&playerId=2202962695001" --pageUrl="http://www.eitb.tv/es/#/video/986121629001" -C "B:0" -C "S:mp4:102076681001/102076681001_986252687001_26930-20110610-122117.mp4&1368558000000&0aa762d184c16de09a21fe533394c3ea" -o "Sukalde_maisuak_-_Aduriz-Hitzen_sukaldea-.mp4"
                         */
                        string cadena = url.Substring(url.IndexOf(".net/") + 5);
                        cadena = cadena.Remove(cadena.IndexOf("/&"));
                        cadena += "?videoId=" + video.VideoUrl.Substring(video.VideoUrl.LastIndexOf("/") + 1) 
                            + "&lineUpId=&pubId=" + array4 + "&playerId=" + m.Groups["experienceId"].Value;
                        RtmpUrl rtmpUrl = new RtmpUrl(rtmp)
                        {
                            PlayPath = playpath,
                            //App = "a500/e1/uds/rtmp/ondemand?videoId=986121629001&lineUpId=&pubId=102076681001&playerId=2202962695001"
                            App = cadena
                        };
                        RtmpBooleanArbitraryData p1 = new RtmpBooleanArbitraryData(false);
                        RtmpStringArbitraryData p2 = new RtmpStringArbitraryData(parts[1]+"&"+parts[2]+"&"+parts[3]);
                        rtmpUrl.ArbitraryData.Add(p1);
                        rtmpUrl.ArbitraryData.Add(p2);
                        
                        url = rtmpUrl.ToString();
                    }

                }
                video.PlaybackOptions.Add(nm, url);
            }

            if (video.PlaybackOptions.Count == 0) return "";// if no match, return empty url -> error
            else
                if (video.PlaybackOptions.Count == 1)
                {
                    string resultUrl = video.PlaybackOptions.Last().Value;
                    video.PlaybackOptions = null;// only one url found, PlaybackOptions not needed
                    return resultUrl;
                }
                else
                {
                    return video.PlaybackOptions.Last().Value;
                }
        }

    }
    
    
}
