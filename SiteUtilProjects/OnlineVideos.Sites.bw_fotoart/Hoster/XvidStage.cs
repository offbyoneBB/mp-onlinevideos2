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
            // string formOp = Helpers.StringUtils.GetSubString(page, @"name=""op"" value=""", @""">");
            // string formUsrlogin = Helpers.StringUtils.GetSubString(page, @"name=""usr_login"" value=""", @""">");
            // string formId = Helpers.StringUtils.GetSubString(page, @"name=""id"" value=""", @""">");
            // string formFname = Helpers.StringUtils.GetSubString(page, @"name=""fname"" value=""", @""">");
            // string formReferer = Helpers.StringUtils.GetSubString(page, @"name=""referer"" value=""", @""">");
            // string formMethodFree = Helpers.StringUtils.GetSubString(page, @"name=""method_free"" value=""", @""">");        

            ////Send Postdata (simulates a button click)
            //string postData = @"op=" + formOp + "&usr_login=" + formUsrlogin + "&id=" + formId + "&fname=" + formFname + "&referer=" + formReferer + "&method_free=" + formMethodFree;
            //string webData = GenericSiteUtil.GetWebDataFromPost(url, postData);

            ////Extract iframe url from HTML
            //Match n = Regex.Match(webData, @"<IFRAME SRC=""(?<url>[^""]*)""[^""]*>");
            ////Get HTML from iframe url
            //string site = WebCache.Instance.GetWebData(n.Groups["url"].Value);
           

            //Grab content and decompress Dean Edwards compressor
            string packed = Helpers.StringUtils.GetSubString(page, @"<div id=""player_code"">", @"</script>");
            string unpacked = Helpers.StringUtils.UnPack(packed);

            //Grab file url from decompresst content
            string res = Helpers.StringUtils.GetSubString(unpacked, @"name=""src""value=""", @"""");

            if (!String.IsNullOrEmpty(res))
            {
                return res;
            }
			return String.Empty;
        }
    }
}