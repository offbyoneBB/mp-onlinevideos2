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
                        string th = System.IO.Path.Combine (new string[] {OnlineVideos.OnlineVideoSettings.Instance.ThumbsDir, "Icons",vid.Title+".png"} );
                        if (System.IO.File.Exists(th))
                        {
                            vid.Thumb = th;
                        }
                        else 
                        {
                            OnlineVideos.Sites.Helper.TvLogoDB.LogoChannel[] tChan = OnlineVideos.Sites.Helper.TvLogoDB.LogoChannel.GetChannel(vid.Title);
                            if (tChan.Length > 0) vid.Thumb = tChan[0].LogoWide;
                        }
                        listVideos.Add(vid);
                    }
                }
            }

            return listVideos;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            if (video.VideoUrl.ToLower().Contains("m3u8") || video.VideoUrl.ToLower().Contains("hls"))
            {
                M3U.M3U.M3UPlaylist pl = new M3U.M3U.M3UPlaylist();
                pl.Read(video.VideoUrl);
                if (pl.Options.ContainsKey("#EXT-X-MEDIA-SEQUENCE")) 
                {
                    return video.VideoUrl;
                }
                else if (pl.Count > 0)
                {
                    video.PlaybackOptions = new Dictionary<string, string>();
                    for (int idx = pl.Count; idx > 0; idx--)
                    {
                        string sName = "Item "+idx;
                        var item = pl[pl.Count - idx];
                        if (item.Options!=null && item.Options.ContainsKey("RESOLUTION"))
                            sName = "RESOLUTION " + item.Options["RESOLUTION"].ToString();
                        video.PlaybackOptions.Add (sName, item.Path);
                    }                    

                    string url = pl[pl.Count -1].Path;
                    return url;
                }
                else 
                {
                    string url = video.VideoUrl;
                    return url;
                }
                
            }
            else
            return base.GetVideoUrl(video);
        }
        #endregion Methods
    }
}