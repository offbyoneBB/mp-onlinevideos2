using OnlineVideos.CrossDomain;

namespace OnlineVideos.Sites
{
	public static class SiteUtilFactory
	{
		static SiteUtilFactory()
		{
            OnlineVideosAssemblyContext.PluginLoader.LoadAllSiteUtilDlls(OnlineVideoSettings.Instance.DllsDir);
		}

		public static bool UtilExists(string shortName)
		{
			return OnlineVideosAssemblyContext.PluginLoader.UtilExists(shortName);
		}

		public static SiteUtilBase CreateFromShortName(string name, SiteSettings settings)
		{
			return OnlineVideosAssemblyContext.PluginLoader.CreateUtilFromShortName(name, settings);
		}

		public static SiteUtilBase CloneFreshSiteFromExisting(SiteUtilBase site)
		{
			return OnlineVideosAssemblyContext.PluginLoader.CloneFreshSiteFromExisting(site);
		}

		public static string RequiredDll(string name)
		{
			return OnlineVideosAssemblyContext.PluginLoader.GetRequiredDllForUtil(name);
		}

		public static string[] GetAllNames()
		{
			return OnlineVideosAssemblyContext.PluginLoader.GetAllUtilNames();
		}
	}
}
