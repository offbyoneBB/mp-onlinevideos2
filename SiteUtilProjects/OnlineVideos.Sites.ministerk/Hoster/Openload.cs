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
            Regex rgx = new Regex(@"{}}}"">.*?<script type=""text/javascript"">(?<code>.*?)</script>", RegexOptions.Singleline);
            Match m = rgx.Match(data);
            if (m.Success)
            {
                string aaCode = m.Groups["code"].Value;
                ScriptEngine engine = new ScriptEngine();
                engine.Execute(OnlineVideos.Sites.Properties.Resources.AADecode);
                data = engine.CallGlobalFunction("decode", aaCode).ToString();
                data = data.Replace("window.vs=", "function html(){return ");
                data = data.Replace("window.vt='video/mp4'", "");
                data += "}";
                engine = new ScriptEngine();
                engine.Execute(data);
                url = engine.CallGlobalFunction("html").ToString();
            }
            return url;
        }
    }
}
