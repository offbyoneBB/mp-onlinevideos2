using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OnlineVideos.Hoster.Base
{
    public static class HosterFactory
    {
        static Dictionary<String, HosterBase> hostersByName = new Dictionary<String, HosterBase>();
        static Dictionary<String, HosterBase> hostersByDNS = new Dictionary<String, HosterBase>();

        static HosterFactory()
        {
            List<Assembly> assemblies = new List<Assembly>();
            assemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.Contains("OnlineVideos.Sites")));
            assemblies.Add(Assembly.GetExecutingAssembly());
            foreach (Assembly assembly in assemblies)
            {
                foreach (Type type in assembly.GetExportedTypes())
                {
                    if (type.BaseType != null && type.IsSubclassOf(typeof(HosterBase)) && type.Namespace.Contains("OnlineVideos.Hoster"))
                    {
                        if (!hostersByName.ContainsKey(type.Name.ToLower()))
                        {
                            HosterBase hb = (HosterBase)Activator.CreateInstance(type);
                            hostersByName.Add(type.Name.ToLower(), hb);
                            if (!hostersByDNS.ContainsKey(hb.getHosterUrl().ToLower())) hostersByDNS.Add(hb.getHosterUrl().ToLower(), hb);
                        }
                    }
                }
            }
        }

        public static HosterBase GetHoster(string name)
        {
            HosterBase hb = null;
            if (hostersByName.TryGetValue(name.ToLower(), out hb)) return hb;
            return null;
        }

        public static List<HosterBase> GetAllHosters()
        {
            return hostersByName.Values.ToList();
        }

        public static bool ContainsName(string name)
        {
            return hostersByName.ContainsKey(name);
        }

        public static bool Contains(Uri uri)
        {
            return hostersByDNS.ContainsKey(uri.Host.Replace("www.", ""));
        }
    }
}
