using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
	/// <summary>
	/// The abstract base class for all hosters. 
    /// Instances might be hosted in a seperate AppDomain than the main application, so it can be unloaded at runtime.
	/// </summary>
    public abstract class HosterBase : UserConfigurable
    {
        #region UserConfigurable implementation

        internal override string GetConfigurationKey(string fieldName)
        {
            return string.Format("{0}|{1}", Helpers.FileUtils.GetSaveFilename(GetHosterUrl()), fieldName);
        }

        #endregion

        #region User Configurable Settings

        [Category(ONLINEVIDEOS_USERCONFIGURATION_CATEGORY), Description("You can give every Hoster a Priority, to control where in the list they appear (the higher the earlier). -1 will hide this Hoster, 0 is the default.")]
        protected int Priority = 0;

        public int UserPriority { get { return Priority; } }

        #endregion

        /// <summary>
        /// You should always call this implementation, even when overriding it. It is called after the instance has been created
        /// in order to configure settings from the xml for this hoster.
        /// </summary>
        public virtual void Initialize()
        {
            // apply custom settings
            foreach (FieldInfo field in this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                object[] attrs = field.GetCustomAttributes(typeof(CategoryAttribute), false);
                if (attrs.Length > 0)
                {
                    SetUserConfigurationValue(field, attrs[0] as CategoryAttribute);
                }
            }
        }

        public virtual Dictionary<string, string> GetPlaybackOptions(string url)
        {
            return new Dictionary<string, string>() { { GetType().Name, GetVideoUrl(url) } };
        }
        
		public virtual Dictionary<string, string> GetPlaybackOptions(string url, System.Net.IWebProxy proxy)
		{
			return new Dictionary<string, string>() { { GetType().Name, GetVideoUrl(url) } };
		}
        
        public abstract string GetVideoUrl(string url);

        public abstract string GetHosterUrl();

        public override string ToString()
        {
            return GetHosterUrl();
        }

        protected static RegexOptions defaultRegexOptions = RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace;

        protected static string FlashProvider(string url, string webData = null)
        {
            string page = webData;
            if (webData == null)
                page = WebCache.Instance.GetWebData<string>(url);

            if (!string.IsNullOrEmpty(page))
            {
                Match n = Regex.Match(page, @"addVariable\(""file"",""(?<url>[^""]+)""\);");
                if (n.Success && Helpers.UriUtils.IsValidUri(n.Groups["url"].Value)) return n.Groups["url"].Value;
                n = Regex.Match(page, @"flashvars.file=""(?<url>[^""]+)"";");
                if (n.Success && Helpers.UriUtils.IsValidUri(n.Groups["url"].Value)) return n.Groups["url"].Value;
                n = Regex.Match(page, @"flashvars.{0,50}file\s*?(?:=|:)\s*?(?:\'|"")?(?<url>[^\'""]+)(?:\'|"")?", defaultRegexOptions);
                if (n.Success && Helpers.UriUtils.IsValidUri(n.Groups["url"].Value)) return n.Groups["url"].Value;
            }
            return String.Empty;
        }

        protected static string DivxProvider(string url, string webData = null)
        {
            string page = webData;
            if (webData == null)
                page = WebCache.Instance.GetWebData<string>(url);

            if (!string.IsNullOrEmpty(page))
            {
                Match n = Regex.Match(page, @"var\surl\s=\s'(?<url>[^']+)';");
                if (n.Success) return n.Groups["url"].Value;
                n = Regex.Match(page, @"video/divx""\ssrc=""(?<url>[^""]+)""");
                if (n.Success) return n.Groups["url"].Value;
            }
            return String.Empty;
        }

        protected static string GetVal(string num, string[] pars)
        {
            int n = 0;
            for (int i = 0; i < num.Length; i++)
            {
                n = n * 36;
                char c = num[i];
                if (Char.IsDigit(c))
                    n += ((int)c) - 0x30;
                else
                    n += ((int)c) - 0x61 + 10;
            }
            if (n < 0 || n >= pars.Length)
                return n.ToString();

            return pars[n];
        }

        protected static string UnPack(string packed)
        {
            string res;
            int p = packed.IndexOf('|');
            if (p < 0) return null;
            p = packed.LastIndexOf('\'', p);

            string pattern = packed.Substring(0, p - 1);

            string[] pars = packed.Substring(p).TrimStart('\'').Split('|');
            for (int i = 0; i < pars.Length; i++)
                if (String.IsNullOrEmpty(pars[i]))
                    if (i < 10)
                        pars[i] = i.ToString();
                    else
                        if (i < 36)
                            pars[i] = ((char)(i + 0x61 - 10)).ToString();
                        else
                            pars[i] = (i - 26).ToString();
            res = String.Empty;
            string num = "";
            for (int i = 0; i < pattern.Length; i++)
            {
                char c = pattern[i];
                if (Char.IsDigit(c) || Char.IsLower(c))
                    num += c;
                else
                {
                    if (num.Length > 0)
                    {
                        res += GetVal(num, pars);
                        num = "";
                    }
                    res += c;
                }
            }
            if (num.Length > 0)
                res += GetVal(num, pars);

            return res;
        }

        protected static string GetSubString(string s, string start, string until)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            int p = s.IndexOf(start);
            if (p == -1) return String.Empty;
            p += start.Length;
            if (until == null) return s.Substring(p);
            int q = s.IndexOf(until, p);
            if (q == -1) return s.Substring(p);
            return s.Substring(p, q - p);
        }

        protected static string GetRegExData(string regex, string data, string group)
        {
            string result = string.Empty;
            Match m = Regex.Match(data, regex);
            if (m.Success)
                result = m.Groups[group].Value;
            return result == null ? string.Empty : result;
        }
    }
}
