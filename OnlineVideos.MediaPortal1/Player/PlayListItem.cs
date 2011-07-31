using System;
using MePo = MediaPortal.Playlists;
using System.Collections.Generic;

namespace OnlineVideos.MediaPortal1.Player
{
    public class PlayList : List<PlayListItem>
    {
        public bool IsPlayAll { get; set; }
    }

    public class PlayListItem : MePo.PlayListItem
    {
        public PlayListItem(string description, string fileName) : base(description, fileName) { }

        public VideoInfo Video { get; set; }

        public Sites.SiteUtilBase Util { get; set; }

        public string ChosenPlaybackOption { get; set; }

        public OnlineVideos.PlayerType? ForcedPlayer { get; set; }
    }
}