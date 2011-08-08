using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using OnlineVideos.Sites;

namespace OnlineVideos
{
	public static class SiteUtilFactory
	{
        static Dictionary<String, Type> utils = new Dictionary<String, Type>();
        static string onlineVideosMainDllName;
       
		static SiteUtilFactory()
        {
            // as we might be loaded as plugin using LoadFrom, we need to handle assembly resolve event to tell the Resolver that OnlineVideos.dll is already in the domain
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            try
            {
                List<Assembly> assemblies = new List<Assembly>();
                Assembly onlineVideosMainDll = Assembly.GetExecutingAssembly();
                assemblies.Add(onlineVideosMainDll);
                onlineVideosMainDllName = onlineVideosMainDll.GetName().Name;
                if (Directory.Exists(OnlineVideoSettings.Instance.DllsDir))
                {
                    // loading assembly as raw bytes, so it can be overwritten while app is still running, but cannot be debugged (running Configuration also needs to be loaded normally)
                    bool loadAsRawBytes = AppDomain.CurrentDomain.FriendlyName != "Configuration.exe";
#if DEBUG
                    if (System.Diagnostics.Debugger.IsAttached) loadAsRawBytes = false;
#endif

                    string[] dllFilesToCheck = Directory.GetFiles(OnlineVideoSettings.Instance.DllsDir, "OnlineVideos.Sites.*.dll");
                    foreach (string aDll in dllFilesToCheck)
                    {
                        if (loadAsRawBytes)
                            assemblies.Add(AppDomain.CurrentDomain.Load(File.ReadAllBytes(aDll)));
                        else
                            assemblies.Add(AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(aDll)));
                    }
                }
                foreach (Assembly assembly in assemblies)
                {
                    Log.Debug("Looking for SiteUtils in Assembly: {0} ({1}) from {2}", assembly.GetName().Name, assembly.GetName().Version.ToString(), Directory.GetLastWriteTime(new Uri(assembly.CodeBase).LocalPath).ToString("yyyy-MM-dd HH:mm:ss"));
                    Type[] typeArray = assembly.GetExportedTypes();
                    foreach (Type type in typeArray)
                    {
                        if (type.BaseType != null && type.IsSubclassOf(typeof(SiteUtilBase)) && !type.IsAbstract)
                        {
                            string shortName = type.Name;
                            if (shortName.EndsWith("Util")) shortName = shortName.Substring(0, shortName.Length - 4);

                            if (utils.ContainsKey(shortName))
                            {
                                Log.Error(string.Format("Unable to add util {0} because its short name has already been added.", type.Name));
                            }
                            else
                            {
                                utils.Add(shortName, type);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn(ex.ToString());
            }
            finally
            {
                // no need to handle any further resolve events
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            }
		}

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // this should only be called to resolve OnlineVideos.dll -> return it regardless of the version, only the name "OnlineVideos"
            AssemblyName an = new AssemblyName(args.Name);
            var asm = (sender as AppDomain).GetAssemblies().FirstOrDefault(a => a.GetName().Name == an.Name);
            return asm;
        }

        public static bool UtilExists(string shortName)
        {
            if (string.IsNullOrEmpty(shortName)) return false;
            else return utils.ContainsKey(shortName);
        }

        public static SiteUtilBase CreateFromShortName(string name, SiteSettings settings)
		{
            if (string.IsNullOrEmpty(name)) return null;

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

        public static string RequiredDll(string name)
        {
            Type result = null;
            if (utils.TryGetValue(name, out result))
            {
                string dll = result.Assembly.GetName().Name;
                return dll != onlineVideosMainDllName ? dll : null;
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
