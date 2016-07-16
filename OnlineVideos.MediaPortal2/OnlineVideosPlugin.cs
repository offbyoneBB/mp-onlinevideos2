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

using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.PluginManager;
using OnlineVideos.MediaPortal2.Interfaces.Metadata;
using OnlineVideos.MediaPortal2.Models;

namespace OnlineVideos.MediaPortal2
{
    public class OnlineVideosPlugin : IPluginStateTracker
    {

        public void Activated(PluginRuntime pluginRuntime)
        {
            // All non-default media item aspects must be registered
            var miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
            miatr.RegisterLocallyKnownMediaItemAspectType(OnlineVideosAspect.Metadata);

            // Prepare OV2 environment
            ConfigurationHelper.InitEnvironment();
        }

        public bool RequestEnd()
        {
            return true;
        }

        public void Stop()
        {
        }

        public void Continue()
        {
        }

        public void Shutdown()
        {
        }
    }
}
