using Jurassic;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class Openload : HosterBase, ISubtitle
    {

        public override string GetHosterUrl()
        {
            return "openload.co";
        }

        string sub = "";

        public override string GetVideoUrl(string url)
        {
            url = url.Replace("/f/", "/embed/");
            string data = GetWebData<string>(url);
            if (data.Contains("<h3>We’re Sorry!</h3>"))
                throw new OnlineVideosException("The video maybe got deleted by the owner or was removed due a copyright violation.");
            sub = "";
            Regex rgx = new Regex(@"<span[^>]+id=""[^""]+""[^>]*>(?<encoded>[0-9A-Za-z]{2,})</span>.*?(?<script>\$\(document\)\[.*?break;\}\}\);)",RegexOptions.Singleline);
            Match m = rgx.Match(data);
            if (m.Success)
            {
                string script = "var encoded = \"" + m.Groups["encoded"].Value;
                script += "\";\r\n";
                script += OnlineVideos.Sites.Properties.Resources.OpenloadDecode;
                script += "\r\n";
                script += m.Groups["script"].Value;
                script += "\r\n;";
                ScriptEngine engine = new ScriptEngine();
                engine.Execute(script);
                string decoded = engine.CallGlobalFunction("getDecodedValue").ToString();
                return decoded;
            }
            return "";
        }

        private void SetSub(string data)
        {
            try
            {
                Regex r = new Regex(@"captions""\s+src=""(?<u>[^""]*)[^>]*?default");
                Match m = r.Match(data);
                if (m.Success)
                {
                    sub = m.Groups["u"].Value;
                    sub = GetWebData(sub, encoding: System.Text.Encoding.UTF8, forceUTF8: true, allowUnsafeHeader: true);
                    string oldSub = sub;
                    r = new Regex(@"(?<time>\d\d:\d\d:\d\d.\d\d\d -->)");
                    int i = 1;
                    foreach (Match match in r.Matches(oldSub))
                    {
                        string time = match.Groups["time"].Value;
                        sub = sub.Replace(time, "\r\n" + i.ToString() + "\r\n" + time);
                        i++;
                    }
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
    
    public class OpenloadIo : Openload
    {
        public override string GetHosterUrl()
        {
            return "openload.io";
        }
    }

    public class Oload : Openload
    {
        public override string GetHosterUrl()
        {
            return "oload.tv";
        }
    }
}