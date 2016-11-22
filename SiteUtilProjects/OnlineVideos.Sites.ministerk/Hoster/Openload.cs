using Jurassic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

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

            string data = GetWebData<string>(url);
            sub = "";
            Regex rgx = new Regex(@"<div style=""display:none;"">[\s\r\n]*<span id=""(?<id1>[^""]*)"">(?<enc1>.+?)</span>[\s\r\n]*<span id=""(?<id2>[^""]*)"">(?<enc2>.+?)</span>");
            Match m = rgx.Match(data);
            if (m.Success)
            {
                string enc1 = HttpUtility.HtmlDecode(m.Groups["enc1"].Value);
                string enc2 = HttpUtility.HtmlDecode(m.Groups["enc2"].Value);
                string id1 = m.Groups["id1"].Value;
                string id2 = m.Groups["id2"].Value;
                rgx = new Regex(@"<script type=""text/javascript"">(?<aaenc>ﾟ.*?\n)");
                m = rgx.Match(data);
                if (m.Success)
                {
                    string js = OnlineVideos.Sites.Utils.HelperUtils.AaDecode(m.Groups["aaenc"].Value);
                    js = js.Replace("$(document).ready(function() {", "function getTheUrl(enc1, enc2) {").Replace("});", "}");
                    js = js.Replace(@"$(""#" + id1 + @""").text()", @"enc1");
                    js = js.Replace(@"$(""#" + id2 + @""").text()", @"enc2");
                    js = js.Replace(@"$(""#streamurl"").text(str)", "return str");
                    ScriptEngine engine = new ScriptEngine();
                    engine.Execute(js);
                    string decoded = engine.CallGlobalFunction("getTheUrl", enc1, enc2).ToString();
                    SetSub(data);
                    return "https://openload.co/stream/" + decoded + "?mime=true";
                }
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
}