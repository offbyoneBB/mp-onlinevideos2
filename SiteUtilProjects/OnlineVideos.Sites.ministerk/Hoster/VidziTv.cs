using Jurassic;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class VidziTv : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "vidzi.tv";
        }

        public override string GetVideoUrl(string url)
        {

            string data = GetWebData<string>(url);
            Regex rgx = new Regex(@">eval(?<js>.*?)</script>", RegexOptions.Singleline);
            Match m = rgx.Match(data);
            if (m.Success)
            {
                ScriptEngine engine = new ScriptEngine();
                string js = m.Groups["js"].Value;
                engine.Execute("var player = " + js + ";");
                engine.Execute("function getPlayer() { return player; };");
                data = engine.CallGlobalFunction("getPlayer").ToString();
                rgx = new Regex(@"file:""(?<url>[^""]*?.mp4[^""]*)");
                m = rgx.Match(data);
                if (m.Success)
                {
                    return m.Groups["url"].Value;
                }
            }
            return "";
        }
    }
}
