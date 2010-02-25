using System;
using System.Collections.Generic;
using System.Reflection;
using MediaPortal.GUI.Library;
using OnlineVideos.Sites;

namespace OnlineVideos
{
	public static class SiteUtilFactory
	{
        static Dictionary<String, Type> utils = new Dictionary<String, Type>();
       
		static SiteUtilFactory()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Log.Debug("Assembly name: {0}", assembly.GetName().Name);
            Type[] typeArray = assembly.GetTypes();
            foreach (Type type in typeArray)
            {
                if (type.BaseType != null && type.BaseType == typeof(SiteUtilBase))
                {
                    string shortName = type.Name;
                    if (shortName.EndsWith("Util")) shortName = shortName.Substring(0, shortName.Length - 4);

                    if (utils.ContainsKey(shortName))
                    {
                        Log.Error(string.Format("Unable to add util {0} because its shot name has already been added.", type.Name));
                    }
                    else
                    {
                        utils.Add(shortName, type);
                    }
                }
            }
		}

        public static SiteUtilBase CreateFromShortName(string name, SiteSettings settings)
		{
            Type result = null;
            if (utils.TryGetValue(name, out result))
            {
                SiteUtilBase util = (SiteUtilBase)Activator.CreateInstance(result);
                util.Initialize(settings);
                return util;
            }
            else
            {
                Log.Error(string.Format("SiteUtil with name: {0} not found!", name));
                return null;
            }
		}

        public static string[] GetAllNames()
        {
            string[] names = new string[utils.Count];
            utils.Keys.CopyTo(names, 0);
            Array.Sort(names);
            return names;
        }
	}
}
