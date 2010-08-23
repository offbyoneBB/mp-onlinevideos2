using System;
using MediaPortal.GUI.Library;

namespace OnlineVideos.MediaPortal1
{
    /// <summary>
    /// Extends a <see cref="GUIListItem"/> with properties and methods only used in OnlineVideos.
    /// </summary>
    public class OnlineVideosGuiListItem : GUIListItem
    {
        public OnlineVideosGuiListItem(string strLabel) : base(strLabel) { }

        protected object _Item;
        /// <summary>
        /// The <see cref="SiteUtilBase"/>, <see cref="Category"/> or <see cref="VideoInfo"/> that belongs to this object.
        /// </summary>
        public object Item 
        {
            get { return _Item; }
            set
            {
                _Item = value;
                System.ComponentModel.INotifyPropertyChanged notifier = value as System.ComponentModel.INotifyPropertyChanged;
                if (notifier != null) notifier.PropertyChanged += (s, e) => 
                {
                    if (s is VideoInfo && e.PropertyName == "ThumbnailImage") SetImageToGui((s as VideoInfo).ThumbnailImage);
                    if (s is Category && e.PropertyName == "Thumb") SetImageToGui((s as Category).Thumb);
                };
            }
        }        

        protected void SetImageToGui(string imageFilePath)
        {
            ThumbnailImage = imageFilePath;
            IconImage = imageFilePath;
            IconImageBig = imageFilePath;
            RefreshCoverArt();
        }
    }
}
