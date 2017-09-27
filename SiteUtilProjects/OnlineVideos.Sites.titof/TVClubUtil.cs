using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class TVClubUtil : SiteUtilBase
    {
        #region <<PRIVATE>>
        private string _playlistUrl = "http://tvclub.fr/xbmc.m3u";
        private string _logoUrl = "http://tvclub.fr/logoshd";
        private string _m3uparser = "#EXTINF:-[0-9]+ tvg-id=\"[\\w -.]*\" tvg-name=\"(?<name>[\\w -.]*)\" tvg-logo=\"(?<logo>[0-9.png]*)\"";
        private string _streamparser= @"http:\/\/(?<url>[a-z0-9.\/]*)";
        #endregion<<PRIVATE>>
        //%APPDATA%\ACEStream\engine\ace_engine.exe

        [Category("OnlineVideosConfiguration"), Description("site identifier")]
        protected string siteIdentifier = "tvclub";

        #region <<OVERRIDE>>
        public override int DiscoverDynamicCategories()
        {
            Category cat =new Category()
            {
                Name = "Le bouquet",
                Thumb = "http://tvclub.fr/images/logo.png"
            };
            Settings.Categories.Add(cat);
            Settings.DynamicCategoriesDiscovered = true;
            
            return 1;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            
            List<VideoInfo> tret = new List<VideoInfo>();

            string sContent = GetWebData(_playlistUrl);
            Regex rgx = new Regex(_m3uparser);
            var tmatch = rgx.Matches(sContent);
            foreach (Match item in tmatch)
            {
                string subitem = sContent.Substring(item.Index);
                Regex tsubrgx = new Regex(_streamparser);
                var tsubmatch = tsubrgx.Matches(subitem);

                if (tsubmatch.Count > 0)
                {

                    VideoInfo cat = new VideoInfo() 
                    {
                        VideoUrl  =  tsubmatch[0].Value,
                        Title = item.Groups["name"].Value,
                        Thumb = _logoUrl + "/" + item.Groups["logo"].Value
                    };
                    
                    if (cat.Title.ToLower().Contains("carre blanc") && Settings.Configuration.ContainsKey("showadult"))
                    {
                        if (Settings.Configuration["showadult"].ToString() == "true")
                        { tret.Add(cat); }
                    }
                    else
                    {
                        tret.Add(cat);
                    }
                }
            }
            return tret;
        }
        #endregion <<OVERRIDE>>
    }
}
