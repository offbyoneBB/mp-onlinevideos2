using System;
using MePo = MediaPortal.Playlists;

namespace OnlineVideos.MediaPortal1.Player
{
    public class PlayListItem : MePo.PlayListItem
    {
        public PlayListItem(string description, string fileName) : base(description, fileName) { }

        public VideoInfo Video { get; set; }

        public Sites.SiteUtilBase Util { get; set; }
    }
}