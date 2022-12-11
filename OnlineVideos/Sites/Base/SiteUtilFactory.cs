using OnlineVideos.CrossDomain;

namespace OnlineVideos.Sites
{
	public class SiteUtilFactory : CrossDomainSingleton<SiteUtilFactory>
	{
		private SiteUtilFactory()
		{
            OnlineVideosAssemblyContext.PluginLoader.LoadAllSiteUtilDlls(OnlineVideoSettings.Instance.DllsDir);
		}

		public bool UtilExists(string shortName)
		{
			return OnlineVideosAssemblyContext.PluginLoader.UtilExists(shortName);
		}

		public SiteUtilBase CreateFromShortName(string name, SiteSettings settings)
		{
			return OnlineVideosAssemblyContext.PluginLoader.CreateUtilFromShortName(name, settings);
		}

		public SiteUtilBase CloneFreshSiteFromExisting(SiteUtilBase site)
		{
			return OnlineVideosAssemblyContext.PluginLoader.CloneFreshSiteFromExisting(site);
		}

		public string RequiredDll(string name)
		{
			return OnlineVideosAssemblyContext.PluginLoader.GetRequiredDllForUtil(name);
		}

		public string[] GetAllNames()
		{
			return OnlineVideosAssemblyContext.PluginLoader.GetAllUtilNames();
		}
	}
}
