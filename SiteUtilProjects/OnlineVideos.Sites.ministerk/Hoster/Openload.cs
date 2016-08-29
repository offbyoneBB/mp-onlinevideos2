using Jurassic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

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
            Regex rgx = new Regex(@"<span id=""hiddenurl"">(?<enc>.+?)</span>");
            Match m = rgx.Match(data);
            if (m.Success)
            {
                string enc = HttpUtility.HtmlDecode(m.Groups["enc"].Value);

                rgx = new Regex(@"<script type=""text/javascript"">(?<enc>ﾟ.*?\n)");
                m = rgx.Match(data);
                if (m.Success)
                {
                    string js = OnlineVideos.Sites.Utils.HelperUtils.AaDecode(m.Groups["enc"].Value);
                    rgx = new Regex(@"""#hiddenurl""\).text\(\);(?<js>[^\$]*)");
                    m = rgx.Match(js);
                    if (m.Success)
                    {
                        js = m.Groups["js"].Value;
                        js = "function aaDecode(x) { " + js + " return str; };";
                        ScriptEngine engine = new ScriptEngine();
                        engine.Execute(js);
                        string decoded = engine.CallGlobalFunction("aaDecode", enc).ToString();
                        return "https://openload.co/stream/" + decoded + "?mime=true";
                    }                    
                }
            }
            return "";
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
