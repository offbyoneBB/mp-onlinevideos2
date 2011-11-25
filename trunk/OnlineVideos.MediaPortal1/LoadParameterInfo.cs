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

		public string Group { get; protected set; }
        public string Site { get; protected set; }
        public string Category { get; protected set; }
        public string Search { get; protected set; }
        public bool ShowVKonFailedSearch { get { return _ShowVKonFailedSearch; } }
        public ReturnMode Return { get; protected set; }
        public string DownloadDir { get; protected set; }
        public string DownloadFilename { get; protected set; }
        public string DownloadMenuEntry { get; protected set; }

        public LoadParameterInfo(string loadParam)
        {
			// group:<groupname>
            // site:<sitename>|category:<categoryname>|search:<searchstring>|VKonfail:<true,false>|return:<Locked,Root>
            Return = ReturnMode.Root;

            if (string.IsNullOrEmpty(loadParam)) return;

			Group = Regex.Match(loadParam, "group:([^|]*)").Groups[1].Value;
            Site = Regex.Match(loadParam, "site:([^|]*)").Groups[1].Value;
            Category = Regex.Match(loadParam, "category:([^|]*)").Groups[1].Value;
            Search = Regex.Match(loadParam, "search:([^|]*)").Groups[1].Value;
            if (!bool.TryParse(Regex.Match(loadParam, "VKonfail:([^|]*)").Groups[1].Value, out _ShowVKonFailedSearch)) _ShowVKonFailedSearch = true;
            try { Return = (ReturnMode)Enum.Parse(typeof(ReturnMode), Regex.Match(loadParam, "return:([^|]*)").Groups[1].Value); }
            catch { Return = ReturnMode.Root; }
			if (Return == ReturnMode.Locked)
			{
				DownloadDir = Regex.Match(loadParam, "downloaddir:([^|]*)").Groups[1].Value;
				DownloadFilename = Regex.Match(loadParam, "downloadfilename:([^|]*)").Groups[1].Value;
				DownloadMenuEntry = Regex.Match(loadParam, "downloadmenuentry:([^|]*)").Groups[1].Value;
			}
        }

        public static string FromGuiProperties()
        {
            List<string> paramsFromGuiProps = new List<string>();
			if (!string.IsNullOrEmpty(GUIPropertyManager.GetProperty("#OnlineVideos.startparams.Group")))
			{
				paramsFromGuiProps.Add("group:" + GUIPropertyManager.GetProperty("#OnlineVideos.startparams.Group"));
				GUIPropertyManager.SetProperty("#OnlineVideos.startparams.Group", string.Empty);
			}
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
			if (!string.IsNullOrEmpty(GUIPropertyManager.GetProperty("#OnlineVideos.startparams.Downloaddir")))
			{
				paramsFromGuiProps.Add("downloaddir:" + GUIPropertyManager.GetProperty("#OnlineVideos.startparams.Downloaddir"));
				GUIPropertyManager.SetProperty("#OnlineVideos.startparams.Downloaddir", string.Empty);
			}
			if (!string.IsNullOrEmpty(GUIPropertyManager.GetProperty("#OnlineVideos.startparams.Downloadfilename")))
			{
				paramsFromGuiProps.Add("downloadfilename:" + GUIPropertyManager.GetProperty("#OnlineVideos.startparams.Downloadfilename"));
				GUIPropertyManager.SetProperty("#OnlineVideos.startparams.Downloadfilename", string.Empty);
			}
			if (!string.IsNullOrEmpty(GUIPropertyManager.GetProperty("#OnlineVideos.startparams.Downloadmenuentry")))
			{
				paramsFromGuiProps.Add("downloadmenuentry:" + GUIPropertyManager.GetProperty("#OnlineVideos.startparams.Downloadmenuentry"));
				GUIPropertyManager.SetProperty("#OnlineVideos.startparams.Downloadmenuentry", string.Empty);
			}
            if (paramsFromGuiProps.Count > 0) return string.Join("|", paramsFromGuiProps.ToArray());
            else return null;
        }
    }
}
