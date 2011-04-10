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
        /// The <see cref="SiteUtilBase"/>, <see cref="Category"/>, <see cref="VideoInfo"/> or <see cref="SitesGroup"/> that belongs to this object.
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
                    if (s is Category && e.PropertyName == "ThumbnailImage") SetImageToGui((s as Category).ThumbnailImage);
                };
            }
        }        

        protected void SetImageToGui(string imageFilePath)
        {
            ThumbnailImage = imageFilePath;
            IconImage = imageFilePath;
            IconImageBig = imageFilePath;

            // if selected and OnlineVideos is current window force an update of #selectedthumb
            GUIOnlineVideos ovsWindow = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow) as GUIOnlineVideos;
            if (ovsWindow != null)
            {
                GUIListItem selectedItem = GUIControl.GetSelectedListItem(GUIOnlineVideos.WindowId, 50);
                if (selectedItem == this)
                {
                    GUIWindowManager.SendThreadMessage(new GUIMessage(GUIMessage.MessageType.GUI_MSG_ITEM_SELECT, GUIWindowManager.ActiveWindow, 0, 50, ItemId ,0,null));
                }
            }
        }
    }
}
