using System;
using System.Collections.Generic;
using OnlineVideos.CrossDomain;

namespace OnlineVideos.Hoster
{
    public static class HosterFactory
    {
        public static HosterBase GetHoster(string name)
        {
            return OnlineVideosAssemblyContext.PluginLoader.GetHoster(name);
        }

        public static List<HosterBase> GetAllHosters()
        {
            return OnlineVideosAssemblyContext.PluginLoader.GetAllHosters();
        }

        public static bool ContainsName(string name)
        {
            return OnlineVideosAssemblyContext.PluginLoader.ContainsHoster(name);
        }

        public static bool Contains(Uri uri)
        {
            return OnlineVideosAssemblyContext.PluginLoader.ContainsHosters(uri);
        }
    }
}
