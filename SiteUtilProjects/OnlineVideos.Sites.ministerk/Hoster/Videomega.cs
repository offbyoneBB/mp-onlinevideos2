using Jurassic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Hoster
{
    public class Videomega : HosterBase
    {

        public override string GetHosterUrl()
        {
            return "videomega.tv";
        }

        public override string GetVideoUrl(string url)
        {

            string url2 = url;
            if (!url.ToLower().Contains("iframe.php"))
            {
                int p = url.IndexOf('?');
                url2 = url.Insert(p, "iframe.php");
            }
            string webData = WebCache.Instance.GetWebData(url2);
            Match m = Regex.Match(webData, @"eval\((?<js>.*)?\)$", RegexOptions.Multiline);
            if (!m.Success)
                return string.Empty;
            string js = "var p = ";
            js += m.Groups["js"].Value;
            js += "; ";
            js += "function packed(){return p;};";
            ScriptEngine engine = new ScriptEngine();
            engine.Execute(js);
            string data = engine.CallGlobalFunction("packed").ToString();
            m = Regex.Match(data, @"""(?<url>http[^""]*)");
            if (!m.Success)
                return String.Empty;
            
            return m.Groups["url"].Value;
        }
    }
}
