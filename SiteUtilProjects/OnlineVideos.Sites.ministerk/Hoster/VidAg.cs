using Jurassic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class VidAg : HosterBase
    {

        public override string GetHosterUrl()
        {
            return "vid.ag";
        }

        public override string GetVideoUrl(string url)
        {
            string data = GetWebData<string>(url);
            url = "";
            Regex rgx = new Regex(@">eval(?<js>.*)?</script>", RegexOptions.Singleline);
            Match m = rgx.Match(data);
            if (m.Success)
            {
                ScriptEngine engine = new ScriptEngine();
                engine.Execute("var player = " + m.Groups["js"].Value + ";");
                engine.Execute("function getPlayer() { return player; };");
                data = engine.CallGlobalFunction("getPlayer").ToString();
                rgx = new Regex(@"file:[^""]*""(?<url>.[^""]*)[^}]*label");
                m = rgx.Match(data);
                if (m.Success)
                {
                    url =  m.Groups["url"].Value;
                }
            }
            return url;
        }

    }
}
