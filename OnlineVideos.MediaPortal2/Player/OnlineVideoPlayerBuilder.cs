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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.RawUrlResourceProvider;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Players.InputStreamPlayer;
using MediaPortal.UI.Players.Video.Settings;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortalWrapper;

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
            string mimeType;
            string title;
            if (!mediaItem.GetPlayData(out mimeType, out title))
                return null;

            PlaylistItem item = mediaItem as PlaylistItem;
            if (item != null && item.InputStreamSite != null)
            {
                Dictionary<string, string> properties;
                if (item.InputStreamSite.GetStreamProperties(item.VideoInfo, out properties))
                {
                    // Replace raw url / token source by resolved stream url
                    var resourceAccessor = new RawUrlResourceAccessor(properties["inputstream.streamurl"]);
                    MediaItemAspect providerResourceAspect = item.Aspects[ProviderResourceAspect.ASPECT_ID];
                    String raPath = resourceAccessor.CanonicalLocalResourcePath.Serialize();
                    providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, raPath);

                    IResourceLocator locator = mediaItem.GetResourceLocator();
                    InputStreamPlayer iplayer = new InputStreamPlayer();
                    try
                    {
                        InitOnline(iplayer, properties);
                        iplayer.SetMediaItem(locator, title);

                        if (iplayer.DecryptError)
                            throw new Exception("Decrypting by InputStream failed.");

                        return iplayer;
                    }
                    catch (Exception ex)
                    {
                        ServiceRegistration.Get<ILogger>().Warn("Error playing media item '{0}': {1}", locator, ex.Message);
                        iplayer.Dispose();
                    }
                }
            }

            // Also try browser player as fallback if InputStream decoding failed
            if (mimeType == WebBrowserVideoPlayer.ONLINEVIDEOSBROWSER_MIMETYPE)
            {
                var player = new WebBrowserVideoPlayer();
                if (!player.Init(mediaItem))
                {
                    player.Dispose();
                    return null;
                }
                return player;
            }

            return null;
        }

        #endregion

        public void InitOnline(InputStreamPlayer player, Dictionary<string, string> properties)
        {
            var videoSettings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>();
            var regionSettings = ServiceRegistration.Get<ISettingsManager>().Load<RegionSettings>();
            CultureInfo culture = CultureInfo.CurrentUICulture;
            try
            {
                if (!string.IsNullOrEmpty(regionSettings.Culture))
                    culture = CultureInfo.CreateSpecificCulture(regionSettings.Culture);
            }
            catch { }

            // Prefer video in screen resolution
            var height = SkinContext.CurrentDisplayMode.Height;
            var width = SkinContext.CurrentDisplayMode.Width;

            InputStream.StreamPreferences preferences = new InputStream.StreamPreferences
            {
                Width = width,
                Height = height,
                ThreeLetterLangCode = culture.ThreeLetterISOLanguageName,
                PreferMultiChannel = videoSettings.PreferMultiChannelAudio
            };

            InputStream onlineSource = new InputStream(properties["inputstream.streamurl"], properties, preferences);

            // Subtitle support depends on "files".
            string fakeFilename;
            if (properties.TryGetValue("fakefilename", out fakeFilename))
                onlineSource.FakeFilename = fakeFilename;

            foreach (string subKey in properties.Keys.Where(k => k.StartsWith("subtitle")))
                onlineSource.SubtitlePaths.Add(properties[subKey]);

            player.InitStream(onlineSource);
        }
    }
}

