using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.Workflow;
using OnlineVideos.CrossDomain;
using OnlineVideos.Downloading;
using OnlineVideos.MediaPortal2.Interfaces.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using OnlineVideos.Helpers;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Common.UserManagement;
using MediaPortal.UI.General;
using OnlineVideos.MediaPortal2.Configuration;

namespace OnlineVideos.MediaPortal2
{
    public class OnlineVideosPlugin : IPluginStateTracker
    {
        private static bool _isInitialized;

        private IIntervalWork _autoUpdateTask;
        private AsynchronousMessageQueue _messageQueue;
        private SettingsChangeWatcher<Configuration.Settings> _settingsWatcher;

        #region IPluginStateTracker implementation

        public void Activated(PluginRuntime pluginRuntime)
        {
            if (_isInitialized)
                return;

            _isInitialized = true;


            // All non-default media item aspects must be registered
            var miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
            miatr.RegisterLocallyKnownMediaItemAspectTypeAsync(OnlineVideosAspect.Metadata).Wait();

            InitializeOnlineVideoSettings();

            // create a message queue for OnlineVideos to broadcast that the list of site utils was rebuild
            _messageQueue = new AsynchronousMessageQueue(this, new string[] { UserMessaging.CHANNEL });
            _messageQueue.MessageReceived += OnUserMessageReceived;
            _messageQueue.Start();

            // load and update sites in a background thread, it takes time and we are on the Main thread delaying MP2 startup
            ServiceRegistration.Get<IThreadPool>().Add(
                InitialSitesUpdateAndLoad,
                "OnlineVideos Initial Sites Load & Update",
                QueuePriority.Low,
                ThreadPriority.BelowNormal,
                AfterInitialLoad);
        }

        private void OnUserMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
        {
            if (message.ChannelName == UserMessaging.CHANNEL)
            {
                if ((UserMessaging.MessageType)message.MessageType == UserMessaging.MessageType.UserChanged)
                {
                    Log.Info("Received UserChange message, update settings");

                    // re-load the xml that holds all configured sites
                    OnlineVideoSettings.Instance.LoadSites();

                    var settings = ServiceRegistration.Get<ISettingsManager>().Load<Settings>();
                    UpdateSettings(settings, true, true);
                }
            }
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
            // Forces unloading of AppDomain on exit. This is required i.e. for WebDriver player to stop external processes.
            OnlineVideoSettings.Unload();
            _messageQueue.Shutdown();
        }

        #endregion

        private static void InitializeOnlineVideoSettings()
        {
            string ovConfigPath = GetCurrentUserConfigDirectory();
            string ovDataPath = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\OnlineVideos");

            OnlineVideosAppDomain.UseSeperateDomain = true;

            OnlineVideoSettings.Instance.DllsDir = Path.Combine(ovDataPath, "SiteUtils");
            OnlineVideoSettings.Instance.ThumbsDir = Path.Combine(ovDataPath, "Thumbs");
            OnlineVideoSettings.Instance.ConfigDir = ovConfigPath;
            OnlineVideoSettings.Instance.GetConfigDir = new DelegateWrapper(GetCurrentUserConfigDirectory);
            OnlineVideoSettings.Instance.AddSupportedVideoExtensions(new List<string> { ".asf", ".asx", ".flv", ".m4v", ".mov", ".mkv", ".mp4", ".wmv", ".webm" });
            OnlineVideoSettings.Instance.Logger = new LogDelegator();
            OnlineVideoSettings.Instance.UserStore = new Configuration.UserSiteSettingsStore();

            ServiceRegistration.Get<ISettingsManager>().Load<Configuration.Settings>().SetValuesToApi();

            // The default connection limit is 2 in .Net on most platforms! This means downloading two files will block all other WebRequests.
            System.Net.ServicePointManager.DefaultConnectionLimit = 100;

            // The default .Net implementation for URI parsing removes trailing dots, which is not correct
            Helpers.DotNetFrameworkHelper.FixUriTrailingDots();
        }

        private static string GetCurrentUserConfigDirectory()
        {
            var userManagement = ServiceRegistration.Get<IUserManagement>();
            var profileId = userManagement.CurrentUser.ProfileId;
            // If not logged in, we use the system ID as profile (see UserManagement.GetOrCreateDefaultUser)
            if (profileId == Guid.Empty)
                profileId = Guid.Parse(ServiceRegistration.Get<ISystemResolver>().LocalSystemId);

            string ovConfigPath = ServiceRegistration.Get<IPathManager>().GetPath(string.Format(@"<CONFIG>\{0}\", profileId));
            return ovConfigPath;
        }

        private static void InitialSitesUpdateAndLoad()
        {
            // clear cache files that might be left over from an application crash
            MPUrlSourceFilter.Downloader.ClearDownloadCache();

            // load translation strings
            TranslationLoader.LoadTranslations(ServiceRegistration.Get<ILocalization>().CurrentCulture.TwoLetterISOLanguageName,
                Path.Combine(Path.GetDirectoryName(typeof(OnlineVideosPlugin).Assembly.Location), "Language"), "en", "strings_{0}.xml");

            // load the xml that holds all configured sites
            OnlineVideoSettings.Instance.LoadSites();

            var settingsManager = ServiceRegistration.Get<ISettingsManager>();
            var ovMP2Settings = settingsManager.Load<Configuration.Settings>();

            // if the current version is compatible and automatic update is enabled and due, run it before loading the site utils
            if (Sites.Updater.VersionCompatible &&
                ovMP2Settings.AutomaticUpdate &&
                ovMP2Settings.LastAutomaticUpdate.AddHours(ovMP2Settings.AutomaticUpdateInterval) < DateTime.Now)
            {
                Sites.Updater.UpdateSites();
                ovMP2Settings.LastAutomaticUpdate = DateTime.Now;
                settingsManager.Save(ovMP2Settings);

                // delete old cached thumbs (todo : no better place to do this for now, should be configurable)
                ImageDownloader.DeleteOldThumbs(30, r => { return true; });
            }

            // instantiate and initialize all siteutils
            OnlineVideoSettings.Instance.BuildSiteUtilsList();
        }

        private void AfterInitialLoad(WorkEventArgs args)
        {
            var ovMP2Settings = ServiceRegistration.Get<ISettingsManager>().Load<Configuration.Settings>();
            if (Sites.Updater.VersionCompatible && ovMP2Settings.AutomaticUpdate)
            {
                _autoUpdateTask = new IntervalWork(PeriodicSitesUpdate, TimeSpan.FromHours(ovMP2Settings.AutomaticUpdateInterval));
                ServiceRegistration.Get<IThreadPool>().AddIntervalWork(_autoUpdateTask, false);
            }

            // start listening to changes of the OnlineVideos configuration settings
            _settingsWatcher = new SettingsChangeWatcher<Configuration.Settings>();
            _settingsWatcher.SettingsChanged += OnlineVideosSettingsChanged;
        }

        private static void PeriodicSitesUpdate()
        {
            // don't run when any of the OV workflow models are currently active
            var workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            if (workflowManager.IsModelContainedInNavigationStack(Guids.WorkFlowModelOV) ||
                workflowManager.IsModelContainedInNavigationStack(Guids.WorkFlowModelSiteManagement) ||
                workflowManager.IsModelContainedInNavigationStack(Guids.WorkFlowModelSiteUpdate))
            {
                return;
            }

            var settingsManager = ServiceRegistration.Get<ISettingsManager>();
            var ovMP2Settings = settingsManager.Load<Configuration.Settings>();

            // if the current version is compatible and automatic update is enabled and due, run it now
            if (Sites.Updater.VersionCompatible &&
                ovMP2Settings.AutomaticUpdate &&
                ovMP2Settings.LastAutomaticUpdate.AddHours(ovMP2Settings.AutomaticUpdateInterval) < DateTime.Now)
            {
                var updateResult = Sites.Updater.UpdateSites();
                ovMP2Settings.LastAutomaticUpdate = DateTime.Now;
                settingsManager.Save(ovMP2Settings);

                if (updateResult == true)
                {
                    Log.Info("Reloading SiteUtil Dlls at runtime.");
                    DownloadManager.Instance.StopAll();
                    OnlineVideoSettings.Reload();
                    TranslationLoader.SetTranslationsToSingleton();
                    GC.Collect();
                    GC.WaitForFullGCComplete();
                }
                if (updateResult != false)
                {
                    OnlineVideoSettings.Instance.BuildSiteUtilsList();
                    ServiceRegistration.Get<IMessageBroker>().Send(OnlineVideosMessaging.CHANNEL, new SystemMessage(OnlineVideosMessaging.MessageType.RebuildSites));
                }
            }
        }

        private void OnlineVideosSettingsChanged(object sender, EventArgs e)
        {
            bool rebuildUtils = false;
            bool rebuildList = false;

            var settings = (sender as SettingsChangeWatcher<Configuration.Settings>).Settings;

            // a download dir was now configured or removed
            if ((string.IsNullOrEmpty(OnlineVideoSettings.Instance.DownloadDir) && !string.IsNullOrEmpty(settings.DownloadFolder)) ||
                (!string.IsNullOrEmpty(OnlineVideoSettings.Instance.DownloadDir) && string.IsNullOrEmpty(settings.DownloadFolder)))
            {
                rebuildUtils = true;
                rebuildList = true;
            }

            UpdateSettings(settings, rebuildList, rebuildUtils);
        }

        private void UpdateSettings(Settings settings, bool rebuildList, bool rebuildUtils)
        {
            // usage of age confirmation has changed
            if (settings.UseAgeConfirmation != OnlineVideoSettings.Instance.UseAgeConfirmation)
            {
                rebuildList = true;
            }

            settings.SetValuesToApi();

            if (OnlineVideoSettings.Instance.IsSiteUtilsListBuilt())
            {
                if (rebuildUtils)
                    OnlineVideoSettings.Instance.BuildSiteUtilsList();
                if (rebuildList)
                {
                    ServiceRegistration.Get<IMessageBroker>().Send(OnlineVideosMessaging.CHANNEL,
                        new SystemMessage(OnlineVideosMessaging.MessageType.RebuildSites));
                }
            }

            // check if automatic update is now enabled / disabled and start / stop or change interval to run
            var threadPool = ServiceRegistration.Get<IThreadPool>();
            if (_autoUpdateTask != null)
            {
                // if automatic update no longer requested or different update interval -> delete the current task
                if (!settings.AutomaticUpdate ||
                    (int) _autoUpdateTask.WorkInterval.TotalHours != settings.AutomaticUpdateInterval)
                {
                    threadPool.RemoveIntervalWork(_autoUpdateTask);
                    _autoUpdateTask = null;
                }
            }

            if (settings.AutomaticUpdate && _autoUpdateTask == null)
            {
                // create the task
                _autoUpdateTask = new IntervalWork(PeriodicSitesUpdate, TimeSpan.FromHours(settings.AutomaticUpdateInterval));
                threadPool.AddIntervalWork(_autoUpdateTask, false);
            }
        }
    }
}
