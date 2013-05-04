using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.General;

namespace OnlineVideos.MediaPortal2
{
	public class OnlineSiteViewModel : ListItem
	{
		protected OnlineVideosWebservice.Site _site;
		public OnlineVideosWebservice.Site Site
		{
			get { return _site; }
		}

		public string Owner { get; protected set; }
		public bool IsLocal { get; protected set; }

		public OnlineSiteViewModel(OnlineVideosWebservice.Site site, bool isLocal) 
			: base(Consts.KEY_NAME, site.Name)
        {
            _site = site;
			IsLocal = isLocal;
			Owner = !string.IsNullOrEmpty(site.Owner_FK) ? site.Owner_FK.Substring(0, site.Owner_FK.IndexOf('@')) : string.Empty;
		}
	}
}
