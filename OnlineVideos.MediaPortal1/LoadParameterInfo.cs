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

        public string Site { get; protected set; }
        public string Category { get; protected set; }
        public string Search { get; protected set; }
        public ReturnMode Return { get; protected set; }
        public GUIFacadeControl.ViewMode? View { get; protected set; }

        public LoadParameterInfo(string loadParam)
        {
            Return = ReturnMode.Root;

            if (string.IsNullOrEmpty(loadParam)) return;

            //site:<sitename>|category:<categoryname>|search:<searchstring>|return:<Locked|Root>|view:<List|SmallIcons|LargeIcons>
            //todo : add bool option on search to popup VK if nothing found or not

            Site = Regex.Match(loadParam, "site:([^|]*)").Groups[1].Value;
            Category = Regex.Match(loadParam, "category:([^|]*)").Groups[1].Value;
            Search = Regex.Match(loadParam, "search:([^|]*)").Groups[1].Value;
            try { Return = (ReturnMode)Enum.Parse(typeof(ReturnMode), Regex.Match(loadParam, "return:([^|]*)").Groups[1].Value); }
            catch { Return = ReturnMode.Root; }
            try { View = (GUIFacadeControl.ViewMode)Enum.Parse(typeof(GUIFacadeControl.ViewMode), Regex.Match(loadParam, "view:([^|]*)").Groups[1].Value); }
            catch { View = null; }
        }
    }
}
