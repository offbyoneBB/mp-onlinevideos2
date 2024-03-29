﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class DivxDen : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "DivxDen.com";
        }

        public override string GetVideoUrl(string url)
        {
            if (url.Contains("embed"))
            {
                string page = WebCache.Instance.GetWebData(url);
                url = Regex.Match(page, @"<div><a\shref=""(?<url>[^""]+)""").Groups["url"].Value;
            }

            string[] urlParts = url.Split('/');

            string postData = @"op=download1&usr_login=&id=" + urlParts[3] + "&fname=" + urlParts[4] + "&referer=&method_free=Free+Stream";
            string webData = WebCache.Instance.GetWebData(url, postData);
            string packed = Helpers.StringUtils.GetSubString(webData, @"return p}", @"</script>");
            string unpacked = MyStringUtils.MyUnPack(packed);
            string res = Helpers.StringUtils.GetSubString(unpacked, @"'file','", @"'");
            if (!String.IsNullOrEmpty(res))
                return res;
            return Helpers.StringUtils.GetSubString(unpacked, @"name=""src""value=""", @"""");
        }


    }
}
