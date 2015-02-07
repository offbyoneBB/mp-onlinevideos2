using System;

using OnlineVideos.Hoster;
using OnlineVideos.Sites;

namespace OnlineVideos.Hoster
{
    public class XvidStage : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "xvidstage.com";
        }

        public override string GetVideoUrl(string url)
        {
             string page = WebCache.Instance.GetWebData(url);

             //Grab hidden value: op, usr_login, id, fname, referer, method_free
            // string formOp = GetSubString(page, @"name=""op"" value=""", @""">");
            // string formUsrlogin = GetSubString(page, @"name=""usr_login"" value=""", @""">");
            // string formId = GetSubString(page, @"name=""id"" value=""", @""">");
            // string formFname = GetSubString(page, @"name=""fname"" value=""", @""">");
            // string formReferer = GetSubString(page, @"name=""referer"" value=""", @""">");
            // string formMethodFree = GetSubString(page, @"name=""method_free"" value=""", @""">");        

            ////Send Postdata (simulates a button click)
            //string postData = @"op=" + formOp + "&usr_login=" + formUsrlogin + "&id=" + formId + "&fname=" + formFname + "&referer=" + formReferer + "&method_free=" + formMethodFree;
            //string webData = GenericSiteUtil.GetWebDataFromPost(url, postData);

            ////Extract iframe url from HTML
            //Match n = Regex.Match(webData, @"<IFRAME SRC=""(?<url>[^""]*)""[^""]*>");
            ////Get HTML from iframe url
            //string site = WebCache.Instance.GetWebData(n.Groups["url"].Value);
           

            //Grab content and decompress Dean Edwards compressor
            string packed = GetSubString(page, @"<div id=""player_code"">", @"</script>");
            string unpacked = UnPack(packed);

            //Grab file url from decompresst content
            string res = GetSubString(unpacked, @"name=""src""value=""", @"""");

            if (!String.IsNullOrEmpty(res))
            {
                return res;
            }
			return String.Empty;
        }
    }
}