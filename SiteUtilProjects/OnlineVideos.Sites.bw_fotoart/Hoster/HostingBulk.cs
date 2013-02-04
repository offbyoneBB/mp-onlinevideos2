using System;
using System.Text.RegularExpressions;

using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;

namespace OnlineVideos.Hoster
{
    public class HostingBulk : HosterBase
    {
        public override string getHosterUrl()
        {
            return "hostingbulk.com";
        }

        public override string getVideoUrls(string url)
        {
            //Get HTML from url
            string page = SiteUtilBase.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                //Grab content and decompress Dean Edwards compressor
                string partial = GetSubString(page, @"<script type='text/javascript'>", @"</script>");
                //Decompress Dean Edwards compressor
                string packed = GetSubString(partial, @"return p}", @"</script>");
                packed = packed.Replace(@"\'", @"'");
                string unpacked = UnPack(packed);

                //Grab file url from decompresst content
                string res = GetSubString(unpacked, @"file','", @"'");                
                //Extract file url from HTML
                Match n = Regex.Match(page, @"{url:\s'(?<url>[^']*)'");

                if (!String.IsNullOrEmpty(res))
                {
                    videoType = VideoType.flv;
                    return res;
                }
            }
            return String.Empty;
        }
    }
}
