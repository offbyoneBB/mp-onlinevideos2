#if NET5_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace OnlineVideos.CrossDomain
{
    public class OnlineVideosAssemblyLoadContext
    {
        AssemblyLoadContext _assemblyLoadContext;
        Dictionary<string, object> _instanceCache = new Dictionary<string, object>();

        PluginLoader _pluginLoader;
        internal PluginLoader PluginLoader
        {
            get { if (_pluginLoader == null) Load(); return _pluginLoader; }
        }

        bool _useSeperateDomain;

        public bool UseSeperateDomain
        {
            set
            {
                if (_assemblyLoadContext == null) _useSeperateDomain = value;
                else throw new Exception("Can't change after Domain is loaded.");
            }
        }

        void Load()
        {
            if (_useSeperateDomain)
            {
                // When passing the folder of OnlineVideos plugin to the AppDomain, it is able to lookup all dependencies in this folder.
                //string appBasePath = Path.GetDirectoryName(typeof(OnlineVideosAppDomain).Assembly.Location);
                _assemblyLoadContext = new AssemblyLoadContext/*SiteUtilLoadContext*/("OnlineVideosSiteUtilDlls", true);

                // we need to subscribe to AssemblyResolve on the MP2 AppDomain because OnlineVideos.dll is loaded in the LoadFrom Context
                // and when unwrapping transparent proxy from our AppDomain, resolving types will fail because it looks only in the default Load context
                // we simply help .Net by returning the already loaded assembly from the LoadFrom context
                AssemblyLoadContext.Default.Resolving += AssemblyResolve;

                //Assembly current = _assemblyLoadContext.LoadFromAssemblyPath(Assembly.GetExecutingAssembly().Location);

                //_pluginLoader = (PluginLoaderNew)current.CreateInstance(typeof(PluginLoaderNew).FullName, true, BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { _assemblyLoadContext }, null, null);
                _pluginLoader = new PluginLoader(_assemblyLoadContext);

                AssemblyLoadContext.Default.Resolving -= AssemblyResolve;

                _instanceCache[typeof(PluginLoader).FullName] = _pluginLoader;
            }
            else
            {
                _assemblyLoadContext = AssemblyLoadContext.Default;
                _pluginLoader = new PluginLoader(_assemblyLoadContext);
            }
        }

        static Assembly AssemblyResolve(AssemblyLoadContext assemblyLoadContext, AssemblyName assemblyName)
        {
            // this should only be called to resolve OnlineVideos.dll -> return it regardless of the version, only the name "OnlineVideos"
            var asm = assemblyLoadContext.Assemblies.FirstOrDefault(a => a.GetName().Name == assemblyName.Name);
            return asm;
        }

        // Equivalent implementation of creating a cross domain singleton but using AssemblyLoadContext rather than an AppDomain.
        // The behaviour of this method relies on this method being internal, see comment below, so any future changes to it's accessibility
        // need to take that into account
        internal object GetCrossDomainSingleton(Type type, params object[] args)
        {
            if (_assemblyLoadContext == null) Load();

            // try to get a cached instance
            if (!_instanceCache.TryGetValue(type.FullName, out object instance))
                instance = null;

            if (instance == null)
            {
                // When using AssemblyLoadContext, rather than an AppDomain, we have to take care to handle a few differences. When loading a (derived) type in an AppDomain
                // and unwrapping it to cross domains it is a requirement that the assembly containing the (base) type is loaded in both domains. In OnlineVideos' case this
                // means ensuring that OnlineVideos.dll is loaded in both domains. When using an AssemblyLoadContext however there is the opposite requirement, OnlineVideos.dll
                // should only be loaded in the main/default AssemblyLoadContext, if it is also loaded in the other load context then a type contained in OnlineVideos.dll created
                // from the other load context will be incompatible with the same type created in the default context as the runtime considers assemblies/types contained in different
                // load contexts to always be different, even if they originate from the same underlying dll on disk. It is therefore necessary to ensure that all types that originate
                // from OnlineVideos.dll are created in the default load context. Currently this method is internal, so therefore all calls must originate from OnlineVideos.dll, and
                // therefore all types must be created in the default load context. This behaviour may need to be changed if the accessibility of this method is changed.
                Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(type.Assembly.FullName));
                instance = assembly.CreateInstance(type.FullName, false, BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.NonPublic, null, args.Length == 0 ? null : args, null, null);
                _instanceCache[type.FullName] = instance;
            }

            // The below is copied from the AppDomain implementation, the references to the singletons could probably
            // be kept in a local field instead, but it is kept to keep the code comparable with the AppDomain implementation.
            // register this singleton name in the list of all Cross AppDomain Singletons in the MainAppDomain
            List<object> singletons = AppDomain.CurrentDomain.GetData("Singletons") as List<object>;
            if (singletons == null) singletons = new List<object>();
            singletons.Add(instance);
            AppDomain.CurrentDomain.SetData("Singletons", singletons);

            return instance; // return the instance
        }

        public object CreateCrossDomainInstance(Type type)
        {
            // When using AssemblyLoadContext the type should only exist in a single load context,
            // and no special handling is needed for it to work across load contexts, so simply create it
            return Activator.CreateInstance(type);
        }

        public object CreateCrossDomainInstance(Type type, BindingFlags bindingAttr, Binder binder, object[] args, System.Globalization.CultureInfo culture, object[] activationAttributes)
        {
            // When using AssemblyLoadContext the type should only exist in a single load context,
            // and no special handling is needed for it to work across load contexts, so simply create it
            return Activator.CreateInstance(type, bindingAttr, binder, args, culture, activationAttributes);
        }

        internal void Unload()
        {
            if (_assemblyLoadContext == null)
                return;
            List<object> singletonNames = AppDomain.CurrentDomain.GetData("Singletons") as List<object>;
            _assemblyLoadContext.Unload();
            _assemblyLoadContext = null;
            _pluginLoader = null;
            _instanceCache.Clear();
            if (singletonNames != null)
                foreach (var s in singletonNames)
                    s.GetType().InvokeMember("_Instance", BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.SetField, null, s, new object[] { null });
        }

        internal void Reload()
        {
            Unload();
            Load();
        }
    }
}
#endif
