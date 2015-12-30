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

using MediaPortal.Common.ResourceAccess;

namespace OnlineVideos.MediaPortal2.ResourceAccess
{
  /// <summary>
  /// Simple <see cref="INetworkResourceAccessor"/> implementation that handles a raw tokens.
  /// Bound to the <see cref="RawTokenResourceProvider"/>.
  /// </summary>
  public class RawTokenResourceAccessor : IResourceAccessor
  {
    protected string _rawToken;

    public RawTokenResourceAccessor(string token)
    {
      _rawToken = token;
    }

    public string Token
    {
      get { return _rawToken; }
    }

    public ResourcePath CanonicalLocalResourcePath
    {
      get { return ResourcePath.BuildBaseProviderPath(RawTokenResourceProvider.RAW_TOKEN_RESOURCE_PROVIDER_ID, RawTokenResourceProvider.ToProviderResourcePath(_rawToken).Serialize()); }
    }

    public IResourceAccessor Clone()
    {
      return new RawTokenResourceAccessor(_rawToken);
    }

    public IResourceProvider ParentProvider
    {
      get { return null; }
    }

    public string Path
    {
      get { return ResourcePath.BuildBaseProviderPath(RawTokenResourceProvider.RAW_TOKEN_RESOURCE_PROVIDER_ID, _rawToken).Serialize(); }
    }

    public string ResourceName
    {
      get { return _rawToken; }
    }

    public string ResourcePathName
    {
      get { return _rawToken; }
    }

    public void Dispose ()
    {
    }
  }
}

