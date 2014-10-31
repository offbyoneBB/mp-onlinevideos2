using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Brownard.Extensions
{
    static class ExtensionMethods
    {
        public static string HtmlCleanup(this string s)
        {
            return s.Replace("&amp;", "&").Replace("&pound;", "£").Replace("&hellip;", "...").Trim();
        }
    }
}
