using System;
using System.IO;
using DirectShow;
using MediaPortal.UI.Players.Video;
using MediaPortal.UI.Players.Video.Tools;

namespace OnlineVideos.MediaPortal2
{
    public class OnlineVideosPlayer : VideoPlayer
    {
        public const string ONLINEVIDEOS_MIMETYPE = "video/online";

		protected override void AddSourceFilter()
        {
            string sourceFilterName = getSourceFilterName();
            if (!string.IsNullOrEmpty(sourceFilterName))
            {
				IBaseFilter sourceFilter = null;
				if (sourceFilterName == MPUrlSourceFilter.MPUrlSourceFilterDownloader.FilterName)
				{
					sourceFilter = FilterLoader.LoadFilterFromDll(
						Path.Combine(Path.GetDirectoryName(this.GetType().Assembly.Location), @"MPUrlSourceSplitter\MPUrlSourceSplitter.ax"), 
						new Guid(MPUrlSourceFilter.MPUrlSourceFilterDownloader.FilterCLSID), false);
					if (sourceFilter != null)
					{
						_graphBuilder.AddFilter(sourceFilter, MPUrlSourceFilter.MPUrlSourceFilterDownloader.FilterName);
					}
				}
				else
				{
					sourceFilter = FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory, sourceFilterName);
				}
                int result = ((IFileSourceFilter)sourceFilter).Load(_resourceAccessor.ResourcePathName, null);

				if (sourceFilter != null)
				{
					OnlineVideos.MPUrlSourceFilter.IFilterState filterState = sourceFilter as OnlineVideos.MPUrlSourceFilter.IFilterState;
					if (filterState != null)
						while (!filterState.IsFilterReadyToConnectPins())
							System.Threading.Thread.Sleep(50); // no need to do this more often than 20 times per second

					FilterGraphTools.RenderOutputPins(_graphBuilder, sourceFilter);
					FilterGraphTools.TryRelease(ref sourceFilter);
				}
            }
            else
            {
				base.AddSourceFilter();
            }
        }

        string getSourceFilterName()
        {
            string sourceFilterName = null;
            Uri uri = new Uri(_resourceAccessor.ResourcePathName);
            string protocol = uri.Scheme.Substring(0, Math.Min(uri.Scheme.Length, 4));
            switch (protocol)
            {
                case "http":
                    sourceFilterName = _resourceAccessor.ResourcePathName.ToLower().Contains(".asf") ? 
                        "WM ASF Reader" : MPUrlSourceFilter.MPUrlSourceFilterDownloader.FilterName;
                    break;
                case "rtmp":
                    sourceFilterName = MPUrlSourceFilter.MPUrlSourceFilterDownloader.FilterName;
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
