using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;

namespace OnlineVideos
{
    internal class PluginLoader : MarshalByRefObject
    {
        static string onlineVideosMainDllName = Assembly.GetExecutingAssembly().GetName().Name;
        Dictionary<String, Type> utils = new Dictionary<String, Type>();
        Dictionary<String, HosterBase> hostersByName = new Dictionary<String, HosterBase>();
        Dictionary<String, HosterBase> hostersByDNS = new Dictionary<String, HosterBase>();

        public PluginLoader()
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
        }

        Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // this should only be called to resolve OnlineVideos.dll -> return it regardless of the version, only the name "OnlineVideos"
            AssemblyName an = new AssemblyName(args.Name);
            var asm = (sender as AppDomain).GetAssemblies().FirstOrDefault(a => a.GetName().Name == an.Name);
            return asm;
        }

        internal void LoadAllSiteUtilDlls(string path)
        {
			var assemblies = new Dictionary<Assembly, DateTime>();
            try
            {
				assemblies.Add(Assembly.GetExecutingAssembly(), Utils.RetrieveLinkerTimestamp(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath));
                if (Directory.Exists(path))
                {
                    string[] dllFilesToCheck = Directory.GetFiles(path, "OnlineVideos.Sites.*.dll");
                    foreach (string aDll in dllFilesToCheck)
                    {
						assemblies.Add(AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(aDll)), Utils.RetrieveLinkerTimestamp(aDll));
                    }
                }
				// search all assemblies for exported SiteUtilBase and HosterBase implementing non abstract classes
                foreach (var assembly in assemblies)
                {
                    Log.Info("Looking for SiteUtils and Hosters in {0} (Version: {1}, Compiled: {2})", 
						assembly.Key.GetName().Name, 
						assembly.Key.GetName().Version.ToString(), 
						assembly.Value.ToString("yyyy-MM-dd HH:mm:ss"));

                    Type[] typeArray = assembly.Key.GetExportedTypes();
                    foreach (Type type in typeArray)
                    {
                        if (type.BaseType != null && type.IsSubclassOf(typeof(SiteUtilBase)) && !type.IsAbstract)
                        {
                            string shortName = type.Name;
                            if (shortName.EndsWith("Util")) shortName = shortName.Substring(0, shortName.Length - 4);

							Type alreadyAddedType = null;
							if (!utils.TryGetValue(shortName, out alreadyAddedType))
                            {
								utils.Add(shortName, type);
                            }
                            else
                            {
								// use the type from the assembly with the latest CompileTime
								if (assemblies[alreadyAddedType.Assembly] < assembly.Value)
								{
									utils[shortName] = type;
									Log.Warn(string.Format("Duplicate SiteUtil '{0}'. Using the one from '{1}', because DLL has newer compile time than '{2}'.",
										shortName,
										type.Assembly.GetName().Name.Replace("OnlineVideos.Sites.", ""),
										alreadyAddedType.Assembly.GetName().Name.Replace("OnlineVideos.Sites.", "")));
								}
								else
								{
									Log.Warn(string.Format("Duplicate SiteUtil '{0}'. Using the one from '{1}', because DLL has newer compile time than '{2}'.",
										shortName,
										alreadyAddedType.Assembly.GetName().Name.Replace("OnlineVideos.Sites.", ""),
										type.Assembly.GetName().Name.Replace("OnlineVideos.Sites.", "")));
								}
                            }
                        }
                        else if (type.BaseType != null && type.IsSubclassOf(typeof(HosterBase)) && !type.IsAbstract)
                        {
							string shortName = type.Name.ToLower();
							HosterBase alreadyAddedHoster = null;
							if (!hostersByName.TryGetValue(shortName, out alreadyAddedHoster))
							{
								HosterBase hb = (HosterBase)Activator.CreateInstance(type);
								hb.Initialize();
								hostersByName.Add(shortName, hb);
								if (!hostersByDNS.ContainsKey(hb.GetHosterUrl().ToLower())) hostersByDNS.Add(hb.GetHosterUrl().ToLower(), hb);
							}
							else
							{
								// use the type from the assembly with the latest CompileTime
								if (assemblies[alreadyAddedHoster.GetType().Assembly] < assembly.Value)
								{
									HosterBase hb = (HosterBase)Activator.CreateInstance(type);
									hb.Initialize();
									hostersByName[shortName] = hb;
									Log.Warn(string.Format("Duplicate Hoster '{0}'. Using the one from '{1}', because DLL has newer compile time than '{2}'.",
										shortName,
										type.Assembly.GetName().Name.Replace("OnlineVideos.Sites.", ""),
										alreadyAddedHoster.GetType().Assembly.GetName().Name.Replace("OnlineVideos.Sites.", "")));
								}
								else
								{
									Log.Warn(string.Format("Duplicate Hoster '{0}'. Using the one from '{1}', because DLL has newer compile time than '{2}'.",
										shortName,
										alreadyAddedHoster.GetType().Assembly.GetName().Name.Replace("OnlineVideos.Sites.", ""),
										type.Assembly.GetName().Name.Replace("OnlineVideos.Sites.", "")));
								}
							}
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
			Log.Info("Found {0} SiteUtils and {1} Hosters in {2} assemblies", utils.Count, hostersByName.Count, assemblies.Count);
        }

        public bool UtilExists(string shortName)
        {
            if (string.IsNullOrEmpty(shortName)) return false;
            else return utils.ContainsKey(shortName);
        }

        public SiteUtilBase CreateUtilFromShortName(string name, SiteSettings settings)
        {
            Type result = null;
            if (utils.TryGetValue(name, out result))
            {
                SiteUtilBase util = null;
                try
                {
                    util = (SiteUtilBase)Activator.CreateInstance(result);
                    util.Initialize(settings);
                    return util;
                }
                catch (Exception ex)
                {
                    Log.Warn("SiteUtil '{0}' is faulty or not compatible with this build of OnlineVideos: {1}", name, ex.Message);
                    return util;
                }
            }
            else
            {
                Log.Warn(string.Format("SiteUtil with name: {0} not found!", name));
                return null;
            }
        }

        public SiteUtilBase CloneFreshSiteFromExisting(SiteUtilBase site)
        {
            // create new instance of this site with reset settings
            SerializableSettings s = new SerializableSettings() { Sites = new BindingList<SiteSettings>() };
            s.Sites.Add(site.Settings);
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            Utils.SiteSettingsToXml(s, ms);
            ms.Position = 0;
            SiteSettings originalSettings = Utils.SiteSettingsFromXml(new StreamReader(ms))[0];
            return CreateUtilFromShortName(site.Settings.UtilName, originalSettings);
        }

        public IList<SiteSettings> CreateSiteSettingsFromXml(string siteXml)
        {
            return Utils.SiteSettingsFromXml(new System.IO.StringReader(siteXml));
        }

        public string GetRequiredDllForUtil(string name)
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

        public string[] GetAllUtilNames()
        {
            string[] names = new string[utils.Count];
            utils.Keys.CopyTo(names, 0);
            Array.Sort(names);
            return names;
        }

        public HosterBase GetHoster(string name)
        {
            HosterBase hb = null;
            if (hostersByName.TryGetValue(name.ToLower(), out hb)) return hb;
            return null;
        }

        public List<HosterBase> GetAllHosters()
        {
            return hostersByName.Values.OrderByDescending(hb => hb.UserPriority).ToList();
        }

        public bool ContainsHoster(string name)
        {
            return hostersByName.ContainsKey(name);
        }

        public bool ContainsHosters(Uri uri)
        {
            return hostersByDNS.ContainsKey(uri.Host.Replace("www.", ""));
        }

        #region MarshalByRefObject overrides
        public override object InitializeLifetimeService()
        {
            // In order to have the lease across appdomains live forever, we return null.
            return null;
        }
        #endregion
    }
}
