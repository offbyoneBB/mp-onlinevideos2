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
				if (sourceFilterName == MPUrlSourceFilter.Downloader.FilterName)
				{
					sourceFilter = FilterLoader.LoadFilterFromDll(
						Path.Combine(Path.GetDirectoryName(this.GetType().Assembly.Location), @"MPUrlSourceSplitter\MPUrlSourceSplitter.ax"),
                        new Guid(MPUrlSourceFilter.Downloader.FilterCLSID), false);
					if (sourceFilter != null)
					{
                        _graphBuilder.AddFilter(sourceFilter, MPUrlSourceFilter.Downloader.FilterName);
					}
				}
				else
				{
					sourceFilter = FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory, sourceFilterName);
				}

				if (sourceFilter != null)
				{
                    var filterStateEx = sourceFilter as OnlineVideos.MPUrlSourceFilter.IFilterStateEx;

                    int result = 0;

                    if (filterStateEx != null)
                    {
                        String url = OnlineVideos.MPUrlSourceFilter.UrlBuilder.GetFilterUrl(null/*siteUtil*/, _resourceAccessor.ResourcePathName);
                        result = filterStateEx.LoadAsync(url);
                        if (result < 0)
                        {
                            throw new OnlineVideosException("Loading URL async error: " + result);
                        }

                        bool opened = false;
                        while (!opened)
                        {
                            result = filterStateEx.IsStreamOpened(out opened);

                            if (result < 0)
                            {
                                throw new OnlineVideosException("Check stream open error: " + result);
                            }

                            if (opened)
                            {
                                break;
                            }

                            System.Threading.Thread.Sleep(1);
                        }
                    }
                    else
                        result = ((IFileSourceFilter)sourceFilter).Load(_resourceAccessor.ResourcePathName, null);

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
                        "WM ASF Reader" : MPUrlSourceFilter.Downloader.FilterName;
                    break;
                case "rtmp":
                    sourceFilterName = MPUrlSourceFilter.Downloader.FilterName;
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
