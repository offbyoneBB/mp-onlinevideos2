using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class FileHoot : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "filehoot.com";
        }

        public override string GetVideoUrl(string url)
        {
            if (!url.Contains("embed-"))
            {
                url = url.Replace("hoot.com/", "hoot.com/embed-");
            }
            if (!url.EndsWith(".html"))
            {
                url += ".html";
            }
            string data = GetWebData<string>(url);
            Regex rgx = new Regex(@"""(?<u>http[^""]*?.mp4)""");
            Match m = rgx.Match(data);
            if (m.Success)
            {
                return m.Groups["u"].Value;
            }
            return "";
        }
    }
}
