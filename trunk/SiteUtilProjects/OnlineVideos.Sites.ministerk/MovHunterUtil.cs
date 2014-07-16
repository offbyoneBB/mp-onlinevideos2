using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class MovHunterUtil : GenericSiteUtil
    {
        public override int DiscoverDynamicCategories()
        {
            int count = base.DiscoverDynamicCategories();
            foreach (Category cat in Settings.Categories) { cat.HasSubCategories = cat.Name == "TV Series"; }
            return count;
        }

        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            ITrackingInfo ti = new TrackingInfo();
            Regex rgx = new Regex(@"(.+)S(\d+)E(\d+)");
            uint s = 0;
            uint e = 0;
            Match m = rgx.Match(video.Title);
            if (m.Success)
            {
                ti.VideoKind = VideoKind.TvSeries;
                ti.Title = m.Groups[1].Value.Trim();
                uint.TryParse(m.Groups[2].Value, out s);
                ti.Season = s;
                uint.TryParse(m.Groups[3].Value, out e);
                ti.Episode = e;
            }
            else
            {
                ti.VideoKind = VideoKind.Movie;
                rgx = new Regex(@"(.+)\((\d{4})\)");
                m = rgx.Match(video.Title);
                uint y = 0;
                if (m.Success) //movies with year
                {
                    ti.Title = m.Groups[1].Value.Trim();
                    uint.TryParse(m.Groups[2].Value, out y);
                    ti.Year = y;
                }
                else // movie no year
                {
                    ti.Title = video.Title;
                }
            }
            return ti;
        }
    }
}
