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
using System.Linq;
using System.Net;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using OnlineVideos.MediaPortal2.Interfaces.Metadata;

namespace Amazon.Importer
{
  public class UrlFanartProvider : IBinaryFanArtProvider
  {
    private readonly static Guid[] NECESSARY_MIAS = { OnlineVideosAspect.ASPECT_ID };

    public bool TryGetFanArt(string mediaType, string fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom, out IList<FanArtImage> result)
    {
      result = null;
      Guid mediaItemId;

      // Don't try to load "fanart" for images
      if (!Guid.TryParse(name, out mediaItemId) || mediaType == FanArtMediaTypes.Image)
        return false;

      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return false;

      IFilter filter = new MediaItemIdFilter(mediaItemId);
      IList<MediaItem> items = mediaLibrary.Search(new MediaItemQuery(NECESSARY_MIAS, filter), false);
      if (items == null || items.Count == 0)
        return false;

      MediaItem mediaItem = items.First();

      string fanartUrl;
      MediaItemAspectMetadata.AttributeSpecification attrFanart = fanArtType == FanArtTypes.FanArt ? OnlineVideosAspect.ATTR_FANART : OnlineVideosAspect.ATTR_POSTER;

      if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, attrFanart, out fanartUrl))
      {
        try
        {
          using (WebClient client = new WebClient())
          {
            var data = client.DownloadData(fanartUrl);
            FanArtImage img = new FanArtImage(fanartUrl, data);
            result = new List<FanArtImage> { img };
            return true;
          }
        }
        catch (Exception)
        {
          return false;
        }
      }
      return false;
    }

    public bool TryGetFanArt(string mediaType, string fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom, out IList<IResourceLocator> result)
    {
      result = null;
      return false;
    }
  }
}
