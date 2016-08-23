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
                string chars = "";

                for (int i = 0; i < enc.Count(); i++)
                {
                    int j = (int)enc[i];
                    if (j >= 33 && j <= 126)
                        j = ((j + 14) % 94) + 33;
                    chars += (char)j;
                }
                url = "https://openload.co/stream/" + chars + "?mime=true";
                return url;
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
