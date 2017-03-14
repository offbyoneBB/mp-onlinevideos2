using System.Collections.Generic;
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
            Regex rgx = new Regex(@"<span[^>]+id=""[^""]+""[^>]*>(?<id>[0-9A-Za-z]+)</span>");
            Match m = rgx.Match(data);
            if (m.Success)
            {
                string id = m.Groups["id"].Value;
                string decoded = "";
                int firstChar = (int)id[0];
                int key = firstChar - 50;
                int maxKey = System.Math.Max(2, key);
                key = System.Math.Min(maxKey, id.Length - 22);
                string t = id.Substring(key, 20);
                int h = 0;
                List<int> chars = new List<int>();
                string v = id.Replace(t, "");
                while (h < t.Length)
                {
                    string f = t.Substring(h, 2);
                    chars.Add(int.Parse(f, System.Globalization.NumberStyles.HexNumber));
                    h += 2;
                }

                h = 0;
                while (h < v.Length)
                {
                    string b = v.Substring(h, 3);
                    int i = int.Parse(b, System.Globalization.NumberStyles.HexNumber);
                    if ((h / 3) % 3 == 0)
                        i = System.Convert.ToInt32(b, 8);
                    int index = (h / 3) % 10;
                    int a = chars[index];
                    i = i ^ 47;
                    i = i ^ a;
                    decoded += (char)i;
                    h += 3;
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