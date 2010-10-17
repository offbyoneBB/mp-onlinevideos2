using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OnlineVideos.Hoster.Base
{
    public static class HosterFactory
    {
        static Dictionary<String, HosterBase> hosters = new Dictionary<String, HosterBase>();

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
                        if (!hosters.ContainsKey(type.Name.ToLower()))
                            hosters.Add(type.Name.ToLower(), (HosterBase)Activator.CreateInstance(type));
                    }
                }
            }
        }

        public static HosterBase GetHoster(string name)
        {
            HosterBase hb = null;
            if (hosters.TryGetValue(name.ToLower(), out hb)) return hb;
            return null;
        }

        public static List<HosterBase> GetAllHosters()
        {
            return hosters.Values.ToList();
        }

        public static bool ContainsName(string name)
        {
            return hosters.ContainsKey(name);
        }
    }
}
