using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.UI.Players.Video;
using DirectShowLib;
using MediaPortal.UI.Players.Video.Tools;

namespace OnlineVideos.MediaPortal2
{
    public class OnlineVideosPlayer : VideoPlayer
    {
        public const string ONLINEVIDEOS_MIMETYPE = "video/online";

        protected override void AddFileSource()
        {
            Uri uri = new Uri(_resourceAccessor.ResourcePathName/*LocalFileSystemPath*/);
            // add the source filter
            string sourceFilterName = (uri.Scheme == "mms" || _resourceAccessor.ResourcePathName.ToLower().Contains(".asf") || _resourceAccessor.ResourcePathName.ToLower().Contains(".wmv")) ? "WM ASF Reader" : uri.Scheme == "http" ? "File Source (URL)" : string.Empty;
            IBaseFilter sourceFilter = FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory, sourceFilterName);
            int result = ((IFileSourceFilter)sourceFilter).Load(_resourceAccessor.ResourcePathName, null);
            FilterGraphTools.RenderOutputPins(_graphBuilder, sourceFilter);
            FilterGraphTools.TryRelease(ref sourceFilter);
        }
    }
}
