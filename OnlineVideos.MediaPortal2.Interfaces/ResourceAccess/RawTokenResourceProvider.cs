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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;

namespace OnlineVideos.MediaPortal2.ResourceAccess
{
  /// <summary>
  /// Resource provider implementation for token resources. A token can be any kind of string identifier and doesn't require a specific format.
  /// </summary>
  public class RawTokenResourceProvider : IBaseResourceProvider
  {
    #region Public constants

    /// <summary>
    /// GUID string for the raw token resource provider.
    /// </summary>
    protected const string RAW_TOKEN_RESOURCE_PROVIDER_ID_STR = "{D1CC4B65-7A01-42D1-A363-548D2C68C0A1}";

    /// <summary>
    /// Raw token resource provider GUID.
    /// </summary>
    public static Guid RAW_TOKEN_RESOURCE_PROVIDER_ID = new Guid(RAW_TOKEN_RESOURCE_PROVIDER_ID_STR);

    protected const string RES_RESOURCE_PROVIDER_NAME = "[RawTokenResourceProvider.Name]";
    protected const string RES_RESOURCE_PROVIDER_DESCRIPTION = "[RawTokenResourceProvider.Description]";

    #endregion

    #region Protected fields

    protected ResourceProviderMetadata _metadata;

    #endregion

    #region Ctor

    public RawTokenResourceProvider()
    {
      _metadata = new ResourceProviderMetadata(RAW_TOKEN_RESOURCE_PROVIDER_ID, RES_RESOURCE_PROVIDER_NAME, RES_RESOURCE_PROVIDER_DESCRIPTION, true, true);
    }

    #endregion

    #region IBaseResourceProvider Member

    public bool TryCreateResourceAccessor(string path, out IResourceAccessor result)
    {
      result = null;
      if (!IsResource(path))
        return false;

      result = new RawTokenResourceAccessor(path);
      return true;
    }

    public ResourcePath ExpandResourcePathFromString(string pathStr)
    {
      if (IsResource(pathStr))
        return new ResourcePath(new[] { new ProviderPathSegment(_metadata.ResourceProviderId, pathStr, true) });
      return null;
    }

    public bool IsResource(string url)
    {
      return !string.IsNullOrEmpty(url);
    }

    #endregion

    #region IResourceProvider Member

    public ResourceProviderMetadata Metadata
    {
      get { return _metadata; }
    }

    #endregion

    public static ResourcePath ToProviderResourcePath(string path)
    {
      return ResourcePath.BuildBaseProviderPath(RAW_TOKEN_RESOURCE_PROVIDER_ID, path);
    }
  }
}
