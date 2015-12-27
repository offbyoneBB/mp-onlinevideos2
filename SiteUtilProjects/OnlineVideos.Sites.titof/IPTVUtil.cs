using System.Collections.Generic;
using System.ComponentModel;

namespace OnlineVideos.Sites
{
    public class IPTVUtil : GenericSiteUtil
    {
        #region Fields

        [Category("OnlineVideosConfiguration"), Description("site identifier")]
        protected string siteIdentifier = "IPTV";

        #endregion Fields

        #region Methods

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> listVideos = new List<VideoInfo>();

            string surl = (category as RssLink).Url;
            using (M3U.M3U.M3UPlaylist pl = new M3U.M3U.M3UPlaylist())
            {
                pl.Read(surl);
                foreach (M3U.M3U.M3UElement flux in pl)
                {
                    if (!string.IsNullOrEmpty(flux.Path) && flux.Options.Count > 0)
                    {
                        VideoInfo vid = new VideoInfo()
                        {
                            VideoUrl = flux.Path,
                            Title = flux.Options["INFOS"].ToString()
                        };
                        listVideos.Add(vid);
                    }
                }
            }

            return listVideos;
        }

        #endregion Methods
    }
}