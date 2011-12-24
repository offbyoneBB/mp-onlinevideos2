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
            // add the source filter
            IBaseFilter sourceFilter = FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory, getSourceFilterName());
            int result = ((IFileSourceFilter)sourceFilter).Load(_resourceAccessor.ResourcePathName, null);
            FilterGraphTools.RenderOutputPins(_graphBuilder, sourceFilter);
            FilterGraphTools.TryRelease(ref sourceFilter);
        }

        string getSourceFilterName()
        {
            string sourceFilterName;
            Uri uri = new Uri(_resourceAccessor.ResourcePathName);
            string protocol = uri.Scheme.Substring(0, Math.Min(uri.Scheme.Length, 4));
            switch (protocol)
            {
                case "http":
                case "rtmp":
                    sourceFilterName = "MediaPortal Url Source Filter";
                    break;
                case "sop":
                    sourceFilterName = "SopCast ASF Splitter";
                    break;
                case "mms":
                    sourceFilterName = "WM ASF Reader";
                    break;
                default:
                    sourceFilterName = _resourceAccessor.ResourcePathName.ToLower().Contains(".asf") ? "WM ASF Reader" : string.Empty;
                    break;
            }

            return sourceFilterName;
        }
    }
}
