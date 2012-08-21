using System;
using System.Collections.Generic;
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

namespace OnlineVideos.Sites
{
    public class LaSextaUtil : GenericSiteUtil, IChoice
    {
        internal String laSextaBaseUrl = "http://www.lasexta.com/lasextaon";
        internal String programsListContentRegex = "<div\\sclass=\"capaseccionl\\sitem_vip\">\\s*<div\\sclass=\"player\">\\s*<a\\shref=\"(?<url>[^\"]*)\">\\s*<img\\ssrc=\"(?<thumb>[^\"]*)\"\\swidth=\"182\"\\sheight=\"102\"\\stitle=\"(?<description>[^\"]*)\"\\salt=\"[^\"]*\"\\s/>\\s*<label\\sclass=\"item_vip_player_label\">(?<title>[^<]*)</label>\\s*<img\\ssrc=\"http://www\\.lasexta\\.com/media/common/img/1pxtrans\\.gif\"\\sclass=\"item_vip_player_link\"\\salt=\"[^\"]*\"/>";
        internal String nextPageRegex = "<a\\shref=\"javascript:reload_programs\\('(?<numero>[^']*)'\\);\"\\sclass=\"siguiente\"";
        internal String reloadProgramsURL = "http://www.lasexta.com/sextatv/reload_programs";
        internal String reloadProgramsARGS1 = "item_id=1&show_id=1&bd_id=1&pagina=";
        internal String reloadProgramsARGS2 = "&limit=3&_=";
        internal String programsData = "";
        internal String getProgramsURL1 = "http://www.lasexta.com/sextatv/change_videos/";
        internal String getProgramsURL2 = "/programasCompletos/undefined";
        internal String programVideosRegex = "href=\"javascript:change_videos\\('(?<programID>[^']*)','programasCompletos'\\);\"";
        internal String videoNextPageRegex = "href=\"javascript:reload\\('[^_]*_programasCompletos_(?<numero>[^']*)'\\);\"\\sclass=\"siguiente\"";
        internal String reloadVideosURL = "http://www.lasexta.com/sextatv/reload";
        internal String reloadVideosARGS1 = "seccion=";
        internal String reloadVideosARGS2 = "&pagina=";
        internal String reloadVideosARGS3 = "&tipo=programasCompletos&section_id&_=";
        internal String videosData = "";
        internal String programVideosContentRegex = "<div\\sclass=\"capaseccionl\\sitem\">\\s*<div\\sclass=\"player_programas\">\\s*<a\\shref=\"(?<VideoUrl>[^\"]*)\"><img\\ssrc=\"(?<ImageUrl>[^\"]*)\"\\swidth=\"170\"\\sheight=\"127\"\\stitle=\"[^\"]*\"\\salt=\"[^\"]*\"\\s/></a>\\s*<a\\shref=\"[^\"]*\"\\sclass=\"item_cortina\">\\s*<img\\ssrc=\"http://www\\.lasexta\\.com/media/common/img/1pxtrans\\.gif\"\\swidth=\"170\"\\sheight=\"127\"\\stitle=\"[^\"]*\"\\salt=\"[^\"]*\"\\s/>\\s*<label\\sclass=\"item_cortina_text\">(?<Description>[^<]*)</label>\\s*<label\\sclass=\"item_cortina_play\">PLAY</label>\\s*</a>\\s*</div>\\s*<h6\\sclass=\"fecha\">(?<Airdate>[^<]*)</h6>\\s*<h5\\sclass=\"titulo\">\\s*<a\\shref=\"[^\"]*\"\\stitle=\"[^\"]*\">(?<Title>[^<]*)</a></h5>\\s*</div>";
        internal String cipherURL1Regex = "_urlVideo=(?<url>[^&]*)&";
        internal String cipherURL2Regex = "_url_list=(?<url>[^&]*)&";
        internal String videoURLRegex = "<url>(?<url>[^<]*)<";
        internal String videoHDURLRegex = "<urlHD>(?<url>[^<]*)<";
        internal const String calidadSD = "Calidad estándar";
        internal const String calidadHD = "Calidad HD";
        internal byte[] keyParameter = {115,100,52,115,100,102,107,118,109,50,51,52};

        internal Regex regexProgramsListContent;
        internal Regex regexNextPage;
        internal Regex regexProgramVideos;
        internal Regex regexVideoNextPage;
        internal Regex regexProgramVideosContent;
        internal Regex regexCipherURL1;
        internal Regex regexCipherURL2;
        internal Regex regexVideoURL;
        internal Regex regexVideoHDURL;

        internal void InitializeRegex()
        {
            regexProgramsListContent = new Regex(programsListContentRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexNextPage = new Regex(nextPageRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexProgramVideos = new Regex(programVideosRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexVideoNextPage = new Regex(videoNextPageRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexProgramVideosContent = new Regex(programVideosContentRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexCipherURL1 = new Regex(cipherURL1Regex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexCipherURL2 = new Regex(cipherURL2Regex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexVideoURL = new Regex(videoURLRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexVideoHDURL = new Regex(videoHDURLRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
        }

        public override void Initialize(SiteSettings siteSettings)
        {
            InitializeRegex();
            siteSettings.DynamicCategoriesDiscovered = false;
            base.Initialize(siteSettings);
        }

        public override int DiscoverDynamicCategories()
        {
            string data = GetWebData(laSextaBaseUrl);
            if (!string.IsNullOrEmpty(data))
            {
                programsData += data;
                getNextPages(data);
                regEx_dynamicCategories = regexProgramsListContent;
                return ParseCategories(programsData);
            }
            return 0;
        }

        public void getNextPages(String data)
        {
            String newData = "";
            if (!string.IsNullOrEmpty(data))
            {
                Match match = regexNextPage.Match(data);
                if (match.Success)
                {
                    fileUrlPostString = reloadProgramsARGS1 + match.Groups["numero"].Value + reloadProgramsARGS2;
                    Log.Debug("LaSexta: searching next pages categories {0} {1}", reloadProgramsURL, fileUrlPostString);
                    newData = GetWebDataFromPost(reloadProgramsURL, fileUrlPostString);
                    programsData += newData;
                    match = regexNextPage.Match(newData);
                    if (match.Success)
                    {
                        getNextPages(newData);
                    }
                }
            }
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            Log.Debug("LaSexta: getting video list from category {0}", category.Name);
            List<VideoInfo> videoList = new List<VideoInfo>();
            String data = GetWebData((category as RssLink).Url);
            Match programVideosMatch = regexProgramVideos.Match(data);
            if (programVideosMatch.Success)
            {
                String programID = programVideosMatch.Groups["programID"].Value;
                //videosData = GetWebData(getProgramsURL1 + programID + getProgramsURL2);
                fileUrlPostString = reloadVideosARGS1 + programID + reloadVideosARGS2 + '0' + reloadVideosARGS3;
                videosData = GetWebDataFromPost(reloadVideosURL, fileUrlPostString);
                getNextVideos(videosData, programID);
                if (!string.IsNullOrEmpty(videosData))
                {
                    Match videoMatch = regexProgramVideosContent.Match(videosData);
                    while (videoMatch.Success)
                    {
                        VideoInfo video = new VideoInfo();
                        video.Title = videoMatch.Groups["Title"].Value;
                        video.Description = videoMatch.Groups["Description"].Value;
                        video.Airdate = videoMatch.Groups["Airdate"].Value;
                        video.ImageUrl = videoMatch.Groups["ImageUrl"].Value;
                        video.VideoUrl = videoMatch.Groups["VideoUrl"].Value;
                        videoList.Add(video);
                        videoMatch = videoMatch.NextMatch();
                    }
                }
            }
            return videoList;
        }

        public void getNextVideos(String data, String programID)
        {
            String newData = "";
            if (!string.IsNullOrEmpty(data))
            {
                Match match = regexVideoNextPage.Match(data);
                if (match.Success)
                {
                    fileUrlPostString = reloadVideosARGS1 + programID + reloadVideosARGS2 + match.Groups["numero"].Value + reloadVideosARGS3;
                    Log.Debug("LaSexta: searching next pages videos {0} {1}", reloadVideosURL, fileUrlPostString);
                    newData = GetWebDataFromPost(reloadVideosURL, fileUrlPostString);
                    videosData += newData;
                    match = regexVideoNextPage.Match(newData);
                    if (match.Success)
                    {
                        getNextVideos(newData, programID);
                    }
                }
            }
        }

        public VideoInfo setCustomVideoInfo(VideoInfo video, String definition, String URL)
        {
            VideoInfo vid = new VideoInfo();
            vid.Title = video.Title;
            vid.Title2 = definition;
            vid.Description = video.Description;
            vid.ImageUrl = video.ImageUrl;
            vid.ThumbnailImage = video.ThumbnailImage;
            vid.VideoUrl = URL;
            return vid;
        }

        public List<VideoInfo> getClips(VideoInfo video, Match match)
        {
            Log.Debug("LaSexta: getting clip info from video {0}", video.Title);
            List<VideoInfo> clips = new List<VideoInfo>();
            String videosURL = decryptVideosURL(match.Groups["url"].Value);
            String data2 = GetWebData(videosURL);
            Match matchVideo = regexVideoURL.Match(data2);
            if (matchVideo.Success)
            {
                VideoInfo vid = new VideoInfo();
                vid = setCustomVideoInfo(video, calidadSD, videosURL);
                clips.Add(vid);
            }
            Match matchVideoHD = regexVideoHDURL.Match(data2);
            if (matchVideoHD.Success)
            {
                VideoInfo vid = new VideoInfo();
                vid = setCustomVideoInfo(video, calidadHD, videosURL);
                clips.Add(vid);
            }
            return clips;
        }

        public List<VideoInfo> getVideoChoices(VideoInfo video)
        {
            Log.Debug("LaSexta: getting video choices info from video {0}", video.Title);
            List<VideoInfo> clips = new List<VideoInfo>();

            String data = GetWebData(video.VideoUrl);
            Match match = regexCipherURL1.Match(data);
            if (match.Success)
            {
                clips = getClips(video, match);
            }
            else
            {
                match = regexCipherURL2.Match(data);
                if (match.Success)
                {
                    clips = getClips(video, match);
                }
            }

            return clips;
        }

        public override List<String> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            Log.Debug("LaSexta: getting multiple video URLs from video {0} with quality {1}", video.Title, video.Title2);
            List<String> result = new List<String>();
            String data = GetWebData(video.VideoUrl);
            if (video.Title2.Equals(calidadSD))
            {
                Match matchVideo = regexVideoURL.Match(data);
                while (matchVideo.Success)
                {
                    String url = matchVideo.Groups["url"].Value;
                    url = replaceURL(url);
                    result.Add(url);
                    matchVideo = matchVideo.NextMatch();
                }
            }
            else if (video.Title2.Equals(calidadHD))
            {
                Match matchVideoHD = regexVideoHDURL.Match(data);
                while (matchVideoHD.Success)
                {
                    String url = decryptVideosURL(matchVideoHD.Groups["url"].Value);
                    url = replaceURL(url);
                    result.Add(url);
                    matchVideoHD = matchVideoHD.NextMatch();
                }
            }
            return result;
        }

        public String replaceURL(String url)
        {
            Log.Debug("LaSexta: replacing final URL: {0}", url);
            url = url.Replace("/mp4:", "/");
            url = url.Replace("/flv:", "/");
            //url = url.Replace("/_definst_/", "/");
            url = url.Replace("/manifest.f4m", "");
            url = url.Replace("http://lasextavod-f.akamaihd.net/z/", "http://descarga.lasexta.com/");
            Log.Debug("LaSexta: final URL replaced: {0}", url);
            return url;
        }

        public String decryptVideosURL(String cypheredHexURL)
        {
            var enc_data_b = ArrayFromHexstring(cypheredHexURL);
            byte[] dec_data = new byte[enc_data_b.Length];
            var rc4 = new Org.BouncyCastle.Crypto.Engines.RC4Engine();
            rc4.Init(false, new Org.BouncyCastle.Crypto.Parameters.KeyParameter(keyParameter));
            rc4.ProcessBytes(enc_data_b, 0, enc_data_b.Length, dec_data, 0);
            return ASCIIEncoding.ASCII.GetString(dec_data);
        }
        

        #region Array Helper
        static byte[] ArrayFromHexstring(string s)
        {
            List<byte> a = new List<byte>();
            for (int i = 0; i < s.Length; i = i + 2)
            {
                a.Add(byte.Parse(s.Substring(i, 2), System.Globalization.NumberStyles.HexNumber));
            }
            return a.ToArray();
        }

        static string HexstringFromArray(byte[] array)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < array.Length; i++)
            {
                sb.Append(array[i].ToString("x2"));
            }
            return sb.ToString();
        }
        #endregion

    }
}
