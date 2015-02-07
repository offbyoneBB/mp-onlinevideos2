using System;
using System.Collections.Generic;
using OnlineVideos.CrossDomain;

namespace OnlineVideos.Hoster.Base
{
    public static class HosterFactory
    {
        public static HosterBase GetHoster(string name)
        {
			return OnlineVideosAppDomain.PluginLoader.GetHoster(name);
        }

        public static List<HosterBase> GetAllHosters()
        {
			return OnlineVideosAppDomain.PluginLoader.GetAllHosters();
        }

        public static bool ContainsName(string name)
        {
			return OnlineVideosAppDomain.PluginLoader.ContainsHoster(name);
        }

        public static bool Contains(Uri uri)
        {
			return OnlineVideosAppDomain.PluginLoader.ContainsHosters(uri);
        }
    }
}
