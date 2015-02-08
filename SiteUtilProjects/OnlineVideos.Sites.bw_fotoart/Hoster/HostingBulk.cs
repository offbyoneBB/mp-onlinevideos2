using System;
using System.Text.RegularExpressions;

using OnlineVideos.Hoster;
using OnlineVideos.Sites;

namespace OnlineVideos.Hoster
{
    public class HostingBulk : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "hostingbulk.com";
        }

        public override string GetVideoUrl(string url)
        {
            //Get HTML from url
            string page = WebCache.Instance.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                //Grab content and decompress Dean Edwards compressor
                string partial = Helpers.StringUtils.GetSubString(page, @"<script type='text/javascript'>", @"</script>");
                //Decompress Dean Edwards compressor
                string packed = Helpers.StringUtils.GetSubString(partial, @"return p}", @"</script>");
                packed = packed.Replace(@"\'", @"'");
                string unpacked = Helpers.StringUtils.UnPack(packed);

                //Grab file url from decompresst content
                string res = Helpers.StringUtils.GetSubString(unpacked, @"file','", @"'");                
                //Extract file url from HTML
                Match n = Regex.Match(page, @"{url:\s'(?<url>[^']*)'");

                if (!String.IsNullOrEmpty(res))
                {
                    return res;
                }
            }
            return String.Empty;
        }
    }
}
