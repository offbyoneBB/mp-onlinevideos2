using Jurassic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class Openload : HosterBase
    {

        public override string GetHosterUrl()
        {
            return "openload.co";
        }

        public override string GetVideoUrl(string url)
        {
            string data = GetWebData<string>(url);
            url = "";
            Regex rgx = new Regex(@"text/javascript"">(?<code>ﾟ.*?)</script>", RegexOptions.Singleline);
            List<string> matches = new List<string>();
            foreach (Match m in rgx.Matches(data))
            {
                matches.Add(m.Groups["code"].Value);
            }
            rgx = new Regex(@"welikekodi_ya_rly = (?<n0>\d+) - (?<n1>\d+)");
            Match match = rgx.Match(data);
            if (match.Success && matches.Count > 0)
            {
                int index = int.Parse(match.Groups["n0"].Value) - int.Parse(match.Groups["n1"].Value);
                string aaCode = matches[index];
                ScriptEngine engine = new ScriptEngine();
                engine.Execute(OnlineVideos.Sites.Properties.Resources.AADecode);
                string js = engine.CallGlobalFunction("decode", aaCode).ToString();
                rgx = new Regex(@"^window\.[^=]*=");
                js = rgx.Replace(js, "function html(){return ");
                js = js.Replace("window.vt='video/mp4'", "");
                js += "}";
                engine = new ScriptEngine();
                engine.Execute(js);
                url = engine.CallGlobalFunction("html").ToString();
            }
            return url;
        }
    }

    public class OpenloadIo : Openload
    {
        public override string GetHosterUrl()
        {
            return "openload.io";
        }

    }
}
