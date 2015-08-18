using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class KissAnimeUtil : GenericSiteUtil
    {
        [Category("OnlineVideosConfiguration")]
        protected string genresRegex;
        [Category("OnlineVideosConfiguration")]
        protected string alphabetRegex;

        private CookieContainer cc;
        private Regex Regex_series;

        public override int DiscoverDynamicCategories()
        {
            //test();
            cc = decryptCFDDOSProtection(baseUrl);
            foreach (Category cat in Settings.Categories)
            {
                cat.HasSubCategories = true;
                if (cat.Name == "Genres")
                    cat.Other = new Regex(genresRegex, defaultRegexOptions);
                else
                    cat.Other = new Regex(alphabetRegex, defaultRegexOptions);
            }
            Settings.DynamicCategoriesDiscovered = true;
            Regex_series = regEx_dynamicSubCategories;
            return 0;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            regEx_dynamicSubCategories = (Regex)parentCategory.Other;
            int res = base.DiscoverSubCategories(parentCategory);
            foreach (Category subcat in parentCategory.SubCategories)
            {
                if (regEx_dynamicSubCategories != Regex_series)
                {
                    subcat.Other = Regex_series;
                    subcat.HasSubCategories = true;
                }
            }
            return res;
        }

        protected override CookieContainer GetCookie()
        {
            return cc;
        }

        public override Dictionary<string, string> GetPlaybackOptions(string playlistUrl)
        {
            var resbase64 = base.GetPlaybackOptions(playlistUrl);
            var res = new Dictionary<string, string>();
            foreach (var kv in resbase64)
            {
                byte[] tmp = Convert.FromBase64String(kv.Value);
                res.Add(kv.Key, Encoding.ASCII.GetString(tmp));
            }
            return res;
        }

        private CookieContainer decryptCFDDOSProtection(string url)
        {
            CookieContainer result = new CookieContainer();
            //string data = System.IO.File.ReadAllText(@"d:\kissanime.htm");
            string data = GetWebData(baseUrl, cookies: result);
            Match m = Regex.Match(data, @"setTimeout\(function\(\){\s*\n*\s*(?:var \D,\D,\D,\D, [0-9A-Za-z]+={""[0-9A-Za-z]+""|.*?.*):(?<init>.*?)};");
            if (!m.Success)
                return null;
            string init = m.Groups["init"].Value;
            Log.Debug("Init:" + init);
            m = Regex.Match(data, @"challenge-form\'\);\s*\n*\r*\a*\s*(?<builder>.*)a.v");
            if (!m.Success)
                return null;
            string builder = m.Groups["builder"].Value;
            Log.Debug("Builder:" + builder);
            m = Regex.Match(data, @"f.submit\(\);\s*\n*\s*},\s*(?<timeout>\d+)\)");
            int timeToWait;
            if (!m.Success || !Int32.TryParse(m.Groups["timeout"].Value, out timeToWait))
                timeToWait = 9000;

            m = Regex.Match(data, @"name=""pass"" value=""(?<cfpass>.+?)""/>");
            string cfPass = "";
            if (m.Success)
                cfPass = m.Groups["cfpass"].Value;

            string cfFormAction = "/cdn-cgi/l/chk_jschl";
            m = Regex.Match(data, @"<form id=""challenge-form"" action=""(?<action>/[^""]+)"" method=""\D+"">");
            if (m.Success)
                cfFormAction = m.Groups["action"].Value;

            string jschl = "";
            m = Regex.Match(data, @"name=""jschl_vc"" value=""(?<jschl>.+?)""/>");
            if (m.Success)
                jschl = m.Groups["jschl"].Value;

            int decryptVal = CF_parseJSString(init);
            string[] lines = builder.Split(';');
            foreach (string line in lines)
                if (line.Length > 0 && line.Contains('='))
                {
                    string[] sections = line.Split('=');
                    string line_leftside = sections[0];
                    int line_val = CF_parseJSString(sections[1]);
                    switch (line_leftside[line_leftside.Length - 1])
                    {
                        case '+': decryptVal = decryptVal + line_val; break;
                        case '-': decryptVal = decryptVal - line_val; break;
                        case '*': decryptVal = decryptVal * line_val; break;
                    }
                }

            Log.Debug("Decryptval:" + decryptVal.ToString());
            Uri uri = new Uri(url);
            string host = uri.Host;
            if (host.Contains('/')) host = host.Split('/')[0];
            host = host.Replace("www.", "");
            int answer = decryptVal + host.Length;

            string domain = "";//domainD
            m = Regex.Match(url, @"(?<domain>\D+://.+?)/");
            if (m.Success)
                domain = m.Groups["domain"].Value;


            string query;
            if (cfPass == "")
                query = String.Format("{0}{1}?jschl_vc={2}&jschl_answer={3}", domain, cfFormAction, jschl, answer);
            else
                query = String.Format("{0}{1}?jschl_vc={2}&pass={3}&jschl_answer={4}", domain, cfFormAction, jschl, cfPass, answer);
            System.Threading.Thread.Sleep(Convert.ToInt32(timeToWait));
            string data2 = GetWebData(query, cookies: result);
            return result;
        }

        private int CF_parseJSString(string s)
        {
            //if (s == @"+[]")
            //  return 0;
            Match m = Regex.Match(s, @"\((?<number>[^\)]*)\)");
            bool first = true;
            if (!m.Success)
            {
                m = Regex.Match(s, @"(?<number>.+)");
                first = false;
            }
            int value = 0;
            while (m.Success)
            {
                string digit = m.Groups["number"].Value;
                int cnt = 0;
                if (digit != @"+[]")
                {
                    int pos = 0;
                    while ((pos = digit.IndexOf("[]", pos) + 1) != 0) cnt++;

                    if (first)
                        cnt--;
                }
                first = false;
                value = value * 10 + cnt;
                m = m.NextMatch();
            }
            Log.Debug("Decode " + s + " result " + value.ToString());
            return value;
        }

    }
}
