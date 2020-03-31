using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OnlineVideos.CrossDomain
{
    public static class OnlineVideosAppDomain
    {
        static AppDomain _domain;
        public static AppDomain Domain
        {
            get { if (_domain == null) Load(); return _domain; }
        }

        static PluginLoader _pluginLoader;
        internal static PluginLoader PluginLoader
        {
            get { if (_pluginLoader == null) Load(); return _pluginLoader; }
        }

        static bool _useSeperateDomain;

        public static bool UseSeperateDomain
        {
            set
            {
                if (_domain == null) _useSeperateDomain = value;
                else throw new Exception("Can't change after Domain is loaded.");
            }
        }

        static void Load()
        {
            if (AppDomain.CurrentDomain.FriendlyName != "OnlineVideosSiteUtilDlls")
            {
                if (_useSeperateDomain)
                {
                    // When passing the folder of OnlineVideos plugin to the AppDomain, it is able to lookup all dependencies in this folder.
                    string appBasePath = Path.GetDirectoryName(typeof(OnlineVideosAppDomain).Assembly.Location);
                    _domain = AppDomain.CreateDomain("OnlineVideosSiteUtilDlls", AppDomain.CurrentDomain.Evidence, appBasePath, null, true);

                    // we need to subscribe to AssemblyResolve on the MP2 AppDomain because OnlineVideos.dll is loaded in the LoadFrom Context
                    // and when unwrapping transparent proxy from our AppDomain, resolving types will fail because it looks only in the default Load context
                    // we simply help .Net by returning the already loaded assembly from the LoadFrom context
                    AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;

                    _pluginLoader = (PluginLoader)_domain.CreateInstanceFromAndUnwrap(
                      Assembly.GetExecutingAssembly().Location,
                      typeof(PluginLoader).FullName);

                    AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;

                    _domain.SetData(typeof(PluginLoader).FullName, _pluginLoader);
                }
                else
                {
                    _domain = AppDomain.CurrentDomain;
                    _pluginLoader = new PluginLoader();
                }
            }
            else
            {
                _domain = AppDomain.CurrentDomain;
                _pluginLoader = (PluginLoader)AppDomain.CurrentDomain.GetData(typeof(PluginLoader).FullName);
            }
        }

        static Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // this should only be called to resolve OnlineVideos.dll -> return it regardless of the version, only the name "OnlineVideos"
            AssemblyName an = new AssemblyName(args.Name);
            var asm = (sender as AppDomain).GetAssemblies().FirstOrDefault(a => a.GetName().Name == an.Name);
            return asm;
        }

        internal static object GetCrossDomainSingleton(Type type, params object[] args)
        {
            if (_domain == null) Load();

            object instance = _domain.GetData(type.FullName); // try to get an instance from the OV domain
            if (instance == null) // no instance in the OV domain yet
            {
                // create an instance in the OV domain
                instance = _domain.CreateInstanceAndUnwrap(type.Assembly.FullName, type.FullName, false, BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.NonPublic, null, args.Length == 0 ? null : args, null, null);
                // register the instance in the OV domain
                _domain.SetData(type.FullName, instance);
            }
            if (AppDomain.CurrentDomain != _domain) // call is coming from the MainAppDomain
            {
                // register this singleton name in the list of all Cross AppDomain Singletons in the MainAppDomain
                List<object> singletons = AppDomain.CurrentDomain.GetData("Singletons") as List<object>;
                if (singletons == null) singletons = new List<object>();
                singletons.Add(instance);
                AppDomain.CurrentDomain.SetData("Singletons", singletons);
            }
            return instance; // return the instance
        }

        internal static void Reload()
        {
            List<object> singletonNames = AppDomain.CurrentDomain.GetData("Singletons") as List<object>;
            AppDomain.Unload(_domain);
            _domain = null;
            _pluginLoader = null;
            if (singletonNames != null)
                foreach (var s in singletonNames)
                    s.GetType().InvokeMember("_Instance", BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.SetField, null, s, new object[] { null });
            Load();
        }
    }
}
