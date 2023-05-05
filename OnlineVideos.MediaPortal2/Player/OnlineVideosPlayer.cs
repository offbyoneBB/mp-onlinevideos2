using DirectShow;
using MediaPortal.Common;
using MediaPortal.UI.Players.Video;
using MediaPortal.UI.Players.Video.Tools;
using MediaPortal.UI.Presentation.Players;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace OnlineVideos.MediaPortal2
{
    public class OnlineVideosPlayer : VideoPlayer
    {
        private FilterFileWrapper _filterWrapper;
        public const string ONLINEVIDEOS_MIMETYPE = "video/online";

        private static string DefaultSplitter
        {
            get
            {
                return IntPtr.Size > 4 ?
                    "LAV Splitter Source" : // Our own MPUrlSourceSplitter filter is not yet available as 64-bit, so fall back to LAV Source Splitter
                    MPUrlSourceFilter.Downloader.FilterName;
            }
        }

        protected override void AddSourceFilter()
        {
            string sourceFilterName = GetSourceFilterName(_resourceAccessor.ResourcePathName);
            if (!string.IsNullOrEmpty(sourceFilterName))
            {
                IBaseFilter sourceFilter = null;
                try
                {
                    if (sourceFilterName == MPUrlSourceFilter.Downloader.FilterName)
                    {
                        // TODO Morpheus_xx, 2020-01-25: Support x64 build here (x86/x64 subfolders) once there is a x64 build of MPUrlSourceSplitter!
                        _filterWrapper = FilterLoader.LoadFilterFromDll(
                            Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), @"MPUrlSourceSplitter\MPUrlSourceSplitter.ax"),
                            new Guid(MPUrlSourceFilter.Downloader.FilterCLSID));
                        sourceFilter = _filterWrapper.GetFilter();
                        if (sourceFilter != null)
                        {
                            _graphBuilder.AddFilter(sourceFilter, MPUrlSourceFilter.Downloader.FilterName);
                        }
                    }
                    else
                    {
                        sourceFilter = FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory, sourceFilterName);
                    }

                    if (sourceFilter == null)
                        throw new OnlineVideosException(string.Format("Could not create instance of source filter: '{0}'", sourceFilterName));

                    var filterStateEx = sourceFilter as MPUrlSourceFilter.IFilterStateEx;
                    if (filterStateEx != null)
                        LoadAndWaitForMPUrlSourceFilter(filterStateEx);
                    else
                    {
                        var fileSourceFilter = sourceFilter as IFileSourceFilter;
                        if (fileSourceFilter != null)
                            Marshal.ThrowExceptionForHR(fileSourceFilter.Load(_resourceAccessor.ResourcePathName, null));
                        else
                            throw new OnlineVideosException(string.Format("'{0}' does not implement IFileSourceFilter", sourceFilterName));
                    }

                    FilterGraphTools.RenderOutputPins(_graphBuilder, sourceFilter);
                }
                finally
                {
                    FilterGraphTools.TryRelease(ref sourceFilter);
                }
            }
            else
            {
                base.AddSourceFilter();
            }
        }

        protected override void FreeCodecs()
        {
            base.FreeCodecs();
            FilterGraphTools.TryDispose(ref _filterWrapper);
        }

        void LoadAndWaitForMPUrlSourceFilter(MPUrlSourceFilter.IFilterStateEx filterStateEx)
        {
            string url = ApplyMPUrlSourceFilterSiteUserSettings(_resourceAccessor.ResourcePathName);
            int result = filterStateEx.LoadAsync(url);
            if (result < 0)
                throw new OnlineVideosException("Loading URL async error: " + result);

            bool opened = false;
            while (!opened)
            {
                System.Threading.Thread.Sleep(1);
                result = filterStateEx.IsStreamOpened(out opened);
                if (result < 0)
                    throw new OnlineVideosException("Check stream open error: " + result);
            }

            bool ready = false;
            while (!ready)
            {
                System.Threading.Thread.Sleep(50);
                result = filterStateEx.IsFilterReadyToConnectPins(out ready);
                if (result != 0)
                    throw new OnlineVideosException("IsFilterReadyToConnectPins error: " + result);
            }
        }

        PlaylistItem GetOnlineVideosMediaItemFromPlayerContexts()
        {
            IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
            for (int index = 0; index < playerContextManager.NumActivePlayerContexts; index++)
            {
                IPlayerContext playerContext = playerContextManager.GetPlayerContext(index);
                if (playerContext != null &&
                    playerContext.CurrentMediaItem is PlaylistItem &&
                    playerContext.CurrentMediaItem.GetResourceLocator().NativeResourcePath == _resourceLocator.NativeResourcePath)
                    return playerContext.CurrentMediaItem as PlaylistItem;
            }
            return null;
        }

        string ApplyMPUrlSourceFilterSiteUserSettings(string url)
        {
            Sites.SiteUtilBase siteUtil = null;
            var item = GetOnlineVideosMediaItemFromPlayerContexts();
            if (item != null)
                OnlineVideoSettings.Instance.SiteUtilsList.TryGetValue(item.SiteName, out siteUtil);

            return MPUrlSourceFilter.UrlBuilder.GetFilterUrl(siteUtil, url, true);
        }

        static string GetSourceFilterName(string url)
        {
            string sourceFilterName = null;
            Uri uri = new Uri(url);
            string protocol = uri.Scheme.Substring(0, Math.Min(uri.Scheme.Length, 4));
            switch (protocol)
            {
                case "http":
                    sourceFilterName = url.ToLower().Contains(".asf") ?
                        "WM ASF Reader" : DefaultSplitter;
                    break;
                case "rtmp":
                    sourceFilterName = DefaultSplitter;
                    break;
                case "sop":
                    sourceFilterName = "SopCast ASF Splitter";
                    break;
                case "mms":
                    sourceFilterName = "WM ASF Reader";
                    break;
            }
            return sourceFilterName;
        }
    }
}
