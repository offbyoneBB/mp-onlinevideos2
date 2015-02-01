using System;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class SilentException_NovaTV : GenericSiteUtil
    {
        private string URL_regEx;
        private string URLFile_regEx;
        private Regex regEx_URL;
        private Regex regEx_URLFile;

        private string ut_section_id;
        private string media_id;
        private string site_id;
        private string section_id;

        private string FileUrl;
        private string FileServer;
        private string FileType;


        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            URL_regEx = @"//user\strack\svariables.*?var\sut_section_id\s=\s""(?<ut_section_id>[^""]+).*?var\smedia_id\s=\s""(?<media_id>[^""]+).*?var\ssite_id\s=\s""(?<site_id>[^""]+).*?var\ssection_id\s=\s'(?<section_id>[^']+)";
            URLFile_regEx = @"\<item\stype="".+?src=""(?<FileUrl>[^""]+)"".+?server=""(?<FileServer>[^""]+)"".+?(?:mimetype=""(?<FileType>[^""]+)"")?";
            regEx_URL = new Regex(URL_regEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
            regEx_URLFile = new Regex(URLFile_regEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
        }

        public override String GetVideoUrl(VideoInfo video)
        {
            string data = GetWebData(video.VideoUrl);
            Match m = regEx_URL.Match(data);
            if (m.Success)
            {
                ut_section_id = m.Groups["ut_section_id"].Value;
                media_id = m.Groups["media_id"].Value;
                site_id = m.Groups["site_id"].Value;
                section_id = m.Groups["section_id"].Value;
            }

            data = GetWebData("http://dnevnik.hr/bin/player/?mod=serve&site_id=" + site_id + "&media_id=" + media_id +
                "&userad_id=&section_id=" + section_id);
            m = regEx_URLFile.Match(data);
            if (m.Success)
            {
                FileUrl = m.Groups["FileUrl"].Value;
                FileServer = m.Groups["FileServer"].Value;
                FileType = m.Groups["FileType"].Value;
                if (string.IsNullOrEmpty(FileType))
                    FileType = "flv";
                data = "http://vid" + FileServer + ".dnevnik.hr/" + FileUrl + "-2" + "." + FileType;
            }
            return data;
        }
    }
}
