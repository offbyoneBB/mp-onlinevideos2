using System;
using System.Collections.Generic;
using System.Reflection;
using MediaPortal.GUI.Library;
using OnlineVideos.Sites;

namespace OnlineVideos
{
	public static class SiteUtilFactory
	{
        static Dictionary<String, SiteUtilBase> moSiteTable = new Dictionary<String, SiteUtilBase>();

		static SiteUtilFactory()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Log.Debug("Assembly name: {0}", assembly.GetName().Name);
            Type[] typeArray = assembly.GetTypes();
            foreach (Type type in typeArray)
            {
                if (type.BaseType != null && type.BaseType == typeof(SiteUtilBase))
                {
                    SiteUtilBase site = (SiteUtilBase)Activator.CreateInstance(type);
                    if (moSiteTable.ContainsKey(site.Name))
                    {
                        Log.Error(string.Format("Unable to add site id {0} because it has already been added.", site.Name));
                    }
                    else
                    {
                        moSiteTable.Add(site.Name, site);
                    }
                }
            }
		}		

		public static SiteUtilBase GetByName(string name)
		{
            SiteUtilBase result = null;
            if (moSiteTable.TryGetValue(name, out result))
                return result;
            else
                return null;
		}

        public static string[] GetAllNames()
        {
            string[] names = new string[moSiteTable.Count];
            moSiteTable.Keys.CopyTo(names, 0);
            Array.Sort(names);
            return names;
        }
	}
}
