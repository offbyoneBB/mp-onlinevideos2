using System;
using System.Text.RegularExpressions;

using OnlineVideos.Hoster;
using OnlineVideos.Sites;

namespace OnlineVideos.Hoster
{
    public class FlashStream : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "flashstream.in";
        }

        public override string GetVideoUrl(string url)
        {
            if (url.Contains("embed"))
            {
                string page = WebCache.Instance.GetWebData(url);
                url = Regex.Match(page, @"<div><a\shref=""(?<url>[^""]+)""").Groups["url"].Value;
            }
            //Get HTML from url
            string webdata = WebCache.Instance.GetWebData(url);

            //Grab hidden value: op, usr_login, id, fname, referer, method_free
            string formOp = Helpers.StringUtils.GetSubString(webdata, @"name=""op"" value=""", @""">");
            string formUsrlogin = Helpers.StringUtils.GetSubString(webdata, @"name=""usr_login"" value=""", @""">");
            string formId = Helpers.StringUtils.GetSubString(webdata, @"name=""id"" value=""", @""">");
            string formFname = Helpers.StringUtils.GetSubString(webdata, @"name=""fname"" value=""", @""">");
            string formReferer = Helpers.StringUtils.GetSubString(webdata, @"name=""referer"" value=""", @""">");
            string formMethodFree = Helpers.StringUtils.GetSubString(webdata, @"name=""method_free"" value=""", @""">");

            //Send Postdata (simulates a button click)
            string postData = @"op=" + formOp + "&usr_login=" + formUsrlogin + "&id=" + formId + "&fname=" + formFname + "&referer=" + formReferer + "&method_free=" + formMethodFree;
            string webData = WebCache.Instance.GetWebData(url, postData);

            //Grab content and decompress Dean Edwards compressor
            string packed = Helpers.StringUtils.GetSubString(webData, @"swfobject.js'></script>", @"</script>");
            packed = packed.Replace(@"\'", @"'");
            string unpacked = Helpers.StringUtils.UnPack(packed);

            //Grab file url from decompresst content
            string res = Helpers.StringUtils.GetSubString(unpacked, @"file','", @"'");

            if (!String.IsNullOrEmpty(res))
			{
				return res;				
			}
			return String.Empty;
        }
    }
}