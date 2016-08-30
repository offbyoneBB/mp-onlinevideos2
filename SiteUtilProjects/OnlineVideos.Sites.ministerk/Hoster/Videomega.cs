using Jurassic;
using System;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class Videomega : HosterBase, ISubtitle
    {

        public override string GetHosterUrl()
        {
            return "videomega.tv";
        }

        public override string GetVideoUrl(string url)
        {

            string url2 = url;
            if (url.ToLower().Contains("view.php"))
            {
                url2 = url.Replace("view.php", "iframe.php");
            }
            else if (!url.ToLower().Contains("iframe.php"))
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
            SetSub(webData);
            return m.Groups["url"].Value;
        }

        private string sub = "";

        private void SetSub(string data)
        {
            try
            {
                Regex r = new Regex(@"captions""\s+src=""(?<u>[^""]*)[^>]*?default");
                Match m = r.Match(data);
                if (m.Success)
                {
                    sub = m.Groups["u"].Value;
                    sub = GetWebData(sub);
                    sub = sub.Substring(sub.IndexOf("1"));
                }
            }
            catch
            {
                sub = "";
            }
        }

        public string SubtitleText
        {
            get { return sub; }
        }
    }
}
