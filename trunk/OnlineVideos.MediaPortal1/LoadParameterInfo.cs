using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MediaPortal.GUI.Library;

namespace OnlineVideos.MediaPortal1
{
    public class LoadParameterInfo
    {
        public enum ReturnMode { Locked, Root };

        protected bool _ShowVKonFailedSearch = true;

        public string Site { get; protected set; }
        public string Category { get; protected set; }
        public string Search { get; protected set; }
        public bool ShowVKonFailedSearch { get { return _ShowVKonFailedSearch; } }
        public ReturnMode Return { get; protected set; }

        public LoadParameterInfo(string loadParam)
        {
            // site:<sitename>|category:<categoryname>|search:<searchstring>|VKonfail:<true,false>|return:<Locked,Root>
            Return = ReturnMode.Root;

            if (string.IsNullOrEmpty(loadParam)) return;

            Site = Regex.Match(loadParam, "site:([^|]*)").Groups[1].Value;
            Category = Regex.Match(loadParam, "category:([^|]*)").Groups[1].Value;
            Search = Regex.Match(loadParam, "search:([^|]*)").Groups[1].Value;
            if (!bool.TryParse(Regex.Match(loadParam, "VKonfail:([^|]*)").Groups[1].Value, out _ShowVKonFailedSearch)) _ShowVKonFailedSearch = true;
            try { Return = (ReturnMode)Enum.Parse(typeof(ReturnMode), Regex.Match(loadParam, "return:([^|]*)").Groups[1].Value); }
            catch { Return = ReturnMode.Root; }
        }
    }
}
