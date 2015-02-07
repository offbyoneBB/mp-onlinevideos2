using System;
using System.Text.RegularExpressions;

using OnlineVideos.Hoster;
using OnlineVideos.Sites;

namespace OnlineVideos.Hoster
{
    public class UploadC : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "uploadc.com";
        }

        public override string GetVideoUrl(string url)
        {
            if (url.Contains("embed"))
            {
                string webData = WebCache.Instance.GetWebData(url);
                //Several Dean Edwards compressor in Html grab here the compressor between the "player_code divs"
                string partial = GetSubString(webData, @"</a> </div>", @"<div class=""left_iframe"">");
                //Grab content and decompress Dean Edwards compressor
                string packed = GetSubString(partial, @"return p}", @"</script>");
                packed = packed.Replace(@"\'", @"'");
                string unpacked = UnPack(packed);

                //Grab file url from decompresst content
                string res = GetSubString(unpacked, @"src""value=""", @"""");

                if (!String.IsNullOrEmpty(res))
                {
                    return res;
                }
                return String.Empty;
            }
            else
            {
                //Get HTML from url
                string page = WebCache.Instance.GetWebData(url);

                //Extract fname value from HTML form
                string fname = Regex.Match(page, @"fname""\svalue=""(?<value>[^""]*)").Groups["value"].Value;
                //Extract fname value from HTML form
                string id = Regex.Match(page, @"id""\svalue=""(?<value>[^""]*)").Groups["value"].Value;

                //Send Postdata (simulates a button click)
                string postData = @"op=download2&usr_login=&id=" + id + "&fname=" + fname + "&referer=&method_free=Slow access";
                string webData = WebCache.Instance.GetWebData(url, postData);

                //Several Dean Edwards compressor in Html grab here the compressor between the "player_code divs"
                string partial = GetSubString(webData, @"</a> </div>", @"<div class=""left_iframe"">");
                //Grab content and decompress Dean Edwards compressor
                string packed = GetSubString(partial, @"return p}", @"</script>");
                packed = packed.Replace(@"\'", @"'");
                string unpacked = UnPack(packed);

                //Grab file url from decompresst content
                string res = GetSubString(unpacked, @"src""value=""", @"""");

                if (!String.IsNullOrEmpty(res))
                {
                    return res;
                }
            }
            return String.Empty;
        }
    }
}
