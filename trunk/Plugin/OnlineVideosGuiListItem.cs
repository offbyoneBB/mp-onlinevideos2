using System;
using MediaPortal.GUI.Library;

namespace OnlineVideos
{
    /// <summary>
    /// Extends a <see cref="GUIListItem"/> with properties only used in OnlineVideos.
    /// </summary>
    public class OnlineVideosGuiListItem : GUIListItem
    {
        public OnlineVideosGuiListItem(string strLabel) : base(strLabel) { }

        /// <summary>
        /// The <see cref="SiteUtilBase"/>, <see cref="Category"/> or <see cref="VideoInfo"/> that belongs to this object.
        /// </summary>
        public object Item { get; set; }

        /// <summary>
        /// The url for the thumbnail for this item. It will be downloaded and set by the <see cref="ImageDownloader"/>.
        /// </summary>
        public string ThumbUrl { get; set; }
    }
}
