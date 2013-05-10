using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.General;

namespace OnlineVideos.MediaPortal2
{
	public class OnlineSiteViewModel : ListItem
	{
		public OnlineVideosWebservice.Site Site { get; protected set; }
		public string Owner { get; protected set; }
		public SiteSettings LocalSite { get; protected set; }

		public OnlineSiteViewModel(OnlineVideosWebservice.Site site, SiteSettings localSite)
			: base(Consts.KEY_NAME, site.Name)
        {
			Site = site;
			LocalSite = localSite;
			Owner = !string.IsNullOrEmpty(site.Owner_FK) ? site.Owner_FK.Substring(0, site.Owner_FK.IndexOf('@')) : string.Empty;
		}
	}
}
