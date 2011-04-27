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
                    else if (s is Category && e.PropertyName == "ThumbnailImage") SetImageToGui((s as Category).ThumbnailImage);
                };
            }
        }

        public string Description
        {
            get
            {
                string desc = null;
                if (Item != null)
                {
                    SitesGroup group = Item as SitesGroup;
                    if (group != null) desc = group.Description;
                    else
                    {
                        Sites.SiteUtilBase site = Item as Sites.SiteUtilBase;
                        if (site != null) desc = site.Settings.Description;
                        else
                        {
                            Category cat = Item as Category;
                            if (cat != null) desc = cat.Description;
                            else
                            {
                                VideoInfo vid = Item as VideoInfo;
                                if (vid != null) desc = vid.Description;
                            }
                        }
                    }
                }
                return desc ?? string.Empty;
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
                int listControlId = ovsWindow.CurrentState == GUIOnlineVideos.State.details ? 51 : 50;
                GUIListItem selectedItem = GUIControl.GetSelectedListItem(GUIOnlineVideos.WindowId, listControlId);
                if (selectedItem == this)
                {
                    GUIWindowManager.SendThreadMessage(new GUIMessage(GUIMessage.MessageType.GUI_MSG_ITEM_SELECT, GUIWindowManager.ActiveWindow, 0, listControlId, ItemId, 0, null));
                }
            }
        }
    }
}
