using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Utils.NaviX
{
    class NaviXNookie
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Processor { get; set; }
        public DateTime Expires { get; set; }

        static List<NaviXNookie> nookies = null;
        public static List<NaviXNookie> GetNookies(string processor)
        {
            List<NaviXNookie> results = new List<NaviXNookie>();
            if (nookies == null)
                return results;
            DateTime now = DateTime.Now;
            for (int x = 0; x < nookies.Count; x++)
            {
                NaviXNookie nookie = nookies[x];
                if (nookie.Expires < now)
                    nookies.RemoveAt(x);
                else if (nookie.Processor == processor)
                    results.Add(nookie);
            }
            return results;
        }
        public static void AddNookie(string processor, string name, string value, string expires)
        {
            if (nookies == null)
                nookies = new List<NaviXNookie>();
            NaviXNookie nookie = null;
            for (int x = 0; x < nookies.Count; x++ )
                if (nookies[x].Processor == processor && nookies[x].Name == name)
                {
                    nookie = nookies[x];
                    nookies.RemoveAt(x);
                    break;
                }
            if (nookie == null)
                nookie = new NaviXNookie() { Name = name, Processor = processor };
            
            nookie.Value = value;
            nookie.Expires = DateTime.MaxValue;

            if (!string.IsNullOrEmpty(expires) && expires != "0")
            {
                System.Text.RegularExpressions.Match m = new System.Text.RegularExpressions.Regex(@"(\d+)([mhd])").Match(expires);
                if (!m.Success)
                    return;
                int exp = int.Parse(m.Groups[1].Value);
                DateTime expTime = DateTime.Now;
                switch (m.Groups[2].Value)
                {
                    case "m":
                        expTime = expTime.AddMinutes(exp);
                        break;
                    case "h":
                        expTime = expTime.AddHours(exp);
                        break;
                    case "d":
                        expTime = expTime.AddDays(exp);
                        break;
                }
                nookie.Expires = expTime;
            }
            nookies.Add(nookie);
        }

    }
}
