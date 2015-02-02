using System;
using System.Text.RegularExpressions;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;

namespace OnlineVideos.Hoster
{
    public class FileNuke : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "filenuke.com";
        }

        public override string GetVideoUrl(string url)
        {
            //Get HTML from url
            string page = WebCache.Instance.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                //Extract fname value from HTML form
                string fname = Regex.Match(page, @"fname""\svalue=""(?<value>[^""]*)").Groups["value"].Value;
                //Extract fname value from HTML form
                string id = Regex.Match(page, @"id""\svalue=""(?<value>[^""]*)").Groups["value"].Value;

                //Send Postdata (simulates a button click)
                string postData = @"op=download1&usr_login=&id=" + id + "&fname=" + fname + "&referer=&method_free=Kostenlos";
                string webData = WebCache.Instance.GetWebData(url, postData);

                //Several Dean Edwards compressor in Html grab here the compressor between the "player_code divs"
                string partial = GetSubString(webData, @"<div id=""player_code"">", @"</div>");
                //Grab content and decompress Dean Edwards compressor
                string packed = GetSubString(partial, @"return p}", @"</script>");
                packed = packed.Replace(@"\'", @"'");
                string unpacked = UnPack(packed);

                //Grab file url from decompresst content
                string res = GetSubString(unpacked, @"file','", @"'");                
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
