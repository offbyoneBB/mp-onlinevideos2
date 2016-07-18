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

using System.Timers;
using MediaPortal.Backend.Database;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.SystemResolver;
using OnlineVideos.MediaPortal2.Interfaces.Metadata;

namespace Amazon.Importer
{
  public class ImporterPlugin : IPluginStateTracker
  {
    private Timer _timer;

    public void Activated(PluginRuntime pluginRuntime)
    {
      var meta = pluginRuntime.Metadata;
      Logger.Info(string.Format("{0} v{1} [{2}] by {3}", meta.Name, meta.PluginVersion, meta.Description, meta.Author));

      _timer = new Timer(1000) { AutoReset = true };
      _timer.Elapsed += InitAsync;
      _timer.Start();
    }

    private void InitAsync(object sender, ElapsedEventArgs e)
    {
      var db = ServiceRegistration.Get<ISQLDatabase>(false);
      var ml = ServiceRegistration.Get<IMediaLibrary>(false);
      var sr = ServiceRegistration.Get<ISystemResolver>(false);

      if (db == null || ml == null || sr == null || !ml.OnlineClients.ContainsKey(sr.LocalSystemId))
        return;
      _timer.Stop();
      _timer.Dispose();

      // All non-default media item aspects must be registered
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectType(OnlineVideosAspect.Metadata);

      LibraryImporter imp = new LibraryImporter();
      imp.ImportSqlite();
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

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
