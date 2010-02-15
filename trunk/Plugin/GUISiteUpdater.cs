using System;
using MediaPortal.GUI.Library;
using MediaPortal.Dialogs;

namespace OnlineVideos
{    
    public class GUISiteUpdater : GUIWindow
    {
        [SkinControlAttribute(50)]
        protected GUIListControl GUI_infoList = null;

        public override int GetID
        {
            get { return 4756; }
            set { base.GetID = value; }
        }

        public override bool Init()
        {
            bool result = Load(GUIGraphicsContext.Skin + @"\myonlinevideosUpdater.xml");
            GUIPropertyManager.SetProperty("#OnlineVideos.owner", " ");
            return result;
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            GUIPropertyManager.SetProperty("#header.label", OnlineVideoSettings.getInstance().BasicHomeScreenName + " Updater");
            GUIPropertyManager.SetProperty("#header.image", OnlineVideoSettings.getInstance().BannerIconsDir + @"Banners/OnlineVideos.png");

            DisplayOnlineSites();
        }

        void DisplayOnlineSites()
        {
            GUIPropertyManager.SetProperty("#OnlineVideos.owner", String.Empty);
            GUIPropertyManager.SetProperty("#OnlineVideos.desc", String.Empty);

            OnlineVideosWebservice.Site[] sites = null;
            if (!Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
            {
                OnlineVideosWebservice.OnlineVideosService ws = new OnlineVideos.OnlineVideosWebservice.OnlineVideosService();
                sites = ws.GetSitesOverview();
            }, "getting site overview from webservice"))
            {
                return;
            }

            if (sites == null || sites.Length == 0) return;

            GUIControl.ClearControl(GetID, GUI_infoList.GetID);

            foreach (OnlineVideosWebservice.Site site in sites)
            {
                if (!site.IsAdult || !OnlineVideoSettings.getInstance().useAgeConfirmation || OnlineVideoSettings.getInstance().ageHasBeenConfirmed)
                {
                    GUIListItem loListItem = new GUIListItem(site.Name);
                    loListItem.TVTag = site;
                    loListItem.Label2 = site.Language;
                    loListItem.Label3 = site.LastUpdated.ToString("dd.MM.yy HH:mm");
                    string image = OnlineVideoSettings.getInstance().BannerIconsDir + @"Icons\" + site.Name + ".png";
                    if (System.IO.File.Exists(image)) { loListItem.IconImage = image; loListItem.ThumbnailImage = image; }
                    loListItem.PinImage = GUIGraphicsContext.Skin + @"\Media\OnlineVideos\" + site.State.ToString() + ".png";
                    loListItem.OnItemSelected += new MediaPortal.GUI.Library.GUIListItem.ItemSelectedHandler(OnSiteSelected);
                    GUI_infoList.Add(loListItem);
                }
            }

            GUIControl.SelectItemControl(GetID, GUI_infoList.GetID, 0);
        }

        private void OnSiteSelected(GUIListItem item, GUIControl parent)
        {
            OnlineVideosWebservice.Site site = item.TVTag as OnlineVideosWebservice.Site;
            if (site != null)
            {
                GUIPropertyManager.SetProperty("#OnlineVideos.owner", site.Owner_FK.Substring(0, site.Owner_FK.IndexOf('@')));
                if (!string.IsNullOrEmpty(site.Description)) GUIPropertyManager.SetProperty("#OnlineVideos.desc", site.Description);
                else GUIPropertyManager.SetProperty("#OnlineVideos.desc", String.Empty);
            }
        }
    }
}
