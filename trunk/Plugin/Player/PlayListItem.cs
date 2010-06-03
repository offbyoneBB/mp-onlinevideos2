using System;

namespace OnlineVideos.Player
{
    public class PlayListItem : MediaPortal.Playlists.PlayListItem
    {
        public PlayListItem(string description, string fileName) : base(description, fileName) { }

        public VideoInfo Video { get; set; }

        public Sites.SiteUtilBase Util { get; set; }
    }
}