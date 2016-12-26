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

            string data = GetWebData<string>(url);
            sub = "";
            Regex rgx = new Regex(@"<span\s+id=""[a-zA-Z0-9]*x"">(?<enc>\d*)<");
            Match m = rgx.Match(data);
            if (m.Success)
            {
                string enc = m.Groups["enc"].Value;
                int firstTwo = int.Parse(enc.Substring(0, 2));
                string decoded = "";
                int num = 2;
                while (num < enc.Length)
                {
                    decoded += (char)(int.Parse(enc.Substring(num, 3)) - firstTwo * int.Parse(enc.Substring(num + 3, 2)));
                    num += 5;
                }
                SetSub(data);
                return "https://openload.co/stream/" + decoded + "?mime=true";
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