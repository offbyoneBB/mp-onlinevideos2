using OnlineVideos.CrossDomain;

namespace OnlineVideos.Sites
{
	public static class SiteUtilFactory
	{
		static SiteUtilFactory()
		{
			OnlineVideosAppDomain.PluginLoader.LoadAllSiteUtilDlls(OnlineVideoSettings.Instance.DllsDir);
		}

		public static bool UtilExists(string shortName)
		{
			return OnlineVideosAppDomain.PluginLoader.UtilExists(shortName);
		}

		public static SiteUtilBase CreateFromShortName(string name, SiteSettings settings)
		{
			return OnlineVideosAppDomain.PluginLoader.CreateUtilFromShortName(name, settings);
		}

		public static SiteUtilBase CloneFreshSiteFromExisting(SiteUtilBase site)
		{
			return OnlineVideosAppDomain.PluginLoader.CloneFreshSiteFromExisting(site);
		}

		public static string RequiredDll(string name)
		{
			return OnlineVideosAppDomain.PluginLoader.GetRequiredDllForUtil(name);
		}

		public static string[] GetAllNames()
		{
			return OnlineVideosAppDomain.PluginLoader.GetAllUtilNames();
		}
	}
}