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

        public static string FromGuiProperties()
        {
            List<string> paramsFromGuiProps = new List<string>();
            if (!string.IsNullOrEmpty(GUIPropertyManager.GetProperty("#OnlineVideos.startparams.Site")))
            {
                paramsFromGuiProps.Add("site:" + GUIPropertyManager.GetProperty("#OnlineVideos.startparams.Site"));
                GUIPropertyManager.SetProperty("#OnlineVideos.startparams.Site", string.Empty);
            }
            if (!string.IsNullOrEmpty(GUIPropertyManager.GetProperty("#OnlineVideos.startparams.Category")))
            {
                paramsFromGuiProps.Add("category:" + GUIPropertyManager.GetProperty("#OnlineVideos.startparams.Category"));
                GUIPropertyManager.SetProperty("#OnlineVideos.startparams.Category", string.Empty);
            }
            if (!string.IsNullOrEmpty(GUIPropertyManager.GetProperty("#OnlineVideos.startparams.Search")))
            {
                paramsFromGuiProps.Add("search:" + GUIPropertyManager.GetProperty("#OnlineVideos.startparams.Search"));
                GUIPropertyManager.SetProperty("#OnlineVideos.startparams.Search", string.Empty);
            }
            if (!string.IsNullOrEmpty(GUIPropertyManager.GetProperty("#OnlineVideos.startparams.VKonfail")))
            {
                paramsFromGuiProps.Add("VKonfail:" + GUIPropertyManager.GetProperty("#OnlineVideos.startparams.VKonfail"));
                GUIPropertyManager.SetProperty("#OnlineVideos.startparams.VKonfail", string.Empty);
            }
            if (!string.IsNullOrEmpty(GUIPropertyManager.GetProperty("#OnlineVideos.startparams.Return")))
            {
                paramsFromGuiProps.Add("return:" + GUIPropertyManager.GetProperty("#OnlineVideos.startparams.Return"));
                GUIPropertyManager.SetProperty("#OnlineVideos.startparams.Return", string.Empty);
            }
            if (paramsFromGuiProps.Count > 0) return string.Join("|", paramsFromGuiProps.ToArray());
            else return null;
        }
    }
}
