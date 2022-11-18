using System;
using System.Reflection;

namespace OnlineVideos.CrossDomain
{
    public static class OnlineVideosAssemblyContext
    {
#if NETFRAMEWORK
        static OnlineVideosAppDomain _assemblyContext = new OnlineVideosAppDomain();
#else
        static OnlineVideosAssemblyLoadContext _assemblyContext = new OnlineVideosAssemblyLoadContext();
#endif

        internal static PluginLoader PluginLoader
        {
            get { return _assemblyContext.PluginLoader; }
        }

        public static bool UseSeperateDomain
        {
            set { _assemblyContext.UseSeperateDomain = value; }
        }

        internal static object GetCrossDomainSingleton(Type type, params object[] args)
        {
            return _assemblyContext.GetCrossDomainSingleton(type, args);
        }

        public static object CreateCrossDomainInstance(Type type)
        {
            return _assemblyContext.CreateCrossDomainInstance(type);
        }

        public static object CreateCrossDomainInstance(Type type, BindingFlags bindingAttr, Binder binder, object[] args, System.Globalization.CultureInfo culture, object[] activationAttributes)
        {
            return _assemblyContext.CreateCrossDomainInstance(type, bindingAttr, binder, args, culture, activationAttributes);
        }

        internal static void Unload()
        {
            _assemblyContext.Unload();
        }

        internal static void Reload()
        {
            _assemblyContext.Reload();
        }
    }
}
