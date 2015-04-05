using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Standalone.ViewModels
{
	public class Site
	{
		public Site(OnlineVideos.Sites.SiteUtilBase siteUtil)
		{
			Model = siteUtil;
			
			Name = siteUtil.Settings.Name;
			Language = siteUtil.Settings.Language;
			Description = siteUtil.Settings.Description;
		}

		public OnlineVideos.Sites.SiteUtilBase Model { get; set; }

		public string Name { get; protected set; }
		public string Language { get; protected set; }
		public string Description { get; protected set; }
	}

	public static class SiteList
	{
		public static ListCollectionView GetSitesView(OnlineVideosMainWindow window, string preselectedSiteName = null)
		{
			int? indexToSelect = null;
			List<Site> convertedSites = new List<Site>();
			int i = 0;
			foreach (var s in OnlineVideos.OnlineVideoSettings.Instance.SiteUtilsList.Values)
			{
				if (preselectedSiteName != null && s.Settings.Name == preselectedSiteName) indexToSelect = i;
				convertedSites.Add(new Site(s));
				i++;
			}
			ListCollectionView view = new ListCollectionView(convertedSites)
			{
				Filter = new Predicate<object>(s => ((ViewModels.Site)s).Name.ToLower().Contains(window.CurrentFilter)),
			};
			if (indexToSelect != null) view.MoveCurrentToPosition(indexToSelect.Value);
			return view;
		}
	}
}
