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
            if (!url.Contains("embed-"))
            {
                url = url.Replace("vidzi.tv/", "vidzi.tv/embed-");
            }
            if (!url.EndsWith(".html"))
            {
                url += ".html";
            }
            string data = GetWebData<string>(url);
            if (data.Contains("File was deleted or expired."))
                throw new OnlineVideosException("File was deleted or expired.");
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
