#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Common.MediaManagement;
using MediaPortal.UI.Presentation.Players;

namespace OnlineVideos.MediaPortal2
{
    /// <summary>
    /// Player builder for all video players of the VideoPlayers plugin.
    /// </summary>
    public class OnlineVideoPlayerBuilder : IPlayerBuilder
    {
        #region IPlayerBuilder implementation

        public IPlayer GetPlayer(MediaItem mediaItem)
        {
            PlaylistItem playlistItem = mediaItem as PlaylistItem;
            if (playlistItem == null)
                return null;

            string mimeType;
            string title;
            if (!playlistItem.GetPlayData(out mimeType, out title))
                return null;

            if (mimeType == WebBrowserVideoPlayer.ONLINEVIDEOSBROWSER_MIMETYPE)
            {
                var player = new WebBrowserVideoPlayer();
                player.Initialise(playlistItem.Util);
                if (!player.Init(playlistItem))
                {
                    player.Dispose();
                    return null;
                }
                return player;
            }

            return null;
        }

        #endregion
    }
}
