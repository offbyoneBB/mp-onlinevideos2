using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;

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
                            Title = flux.Options["INFOS"].ToString(),
                            Airdate= System.DateTime.Now.ToString()
                        };
                        string th = System.IO.Path.Combine (new string[] {OnlineVideos.OnlineVideoSettings.Instance.ThumbsDir, "Icons",vid.Title+".png"} );
                        if (System.IO.File.Exists(th))
                        {
                            vid.Thumb = th;
                        }
                        else
                        {
                            try 
                            {
                                OnlineVideos.Sites.Helper.TvLogoDB.LogoChannel[] tChan = OnlineVideos.Sites.Helper.TvLogoDB.LogoChannel.GetChannel(vid.Title);
                                if (tChan.Length > 0)
                                {
                                    if (!string.IsNullOrEmpty(tChan[0].LogoWide))
                                    {
                                        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(tChan[0].LogoWide + "/medium");
                                        HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                                        using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                                        {
                                            System.Drawing.Image bmp = System.Drawing.Bitmap.FromStream((System.IO.Stream)sr.BaseStream);
                                            sr.Close();
                                            bmp.Save(th);
                                        }
                                        vid.Thumb = th;
                                    }
                                }
                            }
                            catch { }
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
                if (pl.IsHLS() ) 
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
                        if (!video.PlaybackOptions.ContainsKey(sName))
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