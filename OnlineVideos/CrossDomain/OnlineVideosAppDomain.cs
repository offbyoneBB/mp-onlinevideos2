using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace OnlineVideos
{
	public static class OnlineVideosAppDomain
	{
		static AppDomain domain;
		public static AppDomain Domain
		{
			get { if (domain == null) Load(); return domain; }
		}

		static PluginLoader pluginLoader;
		internal static PluginLoader PluginLoader
		{
			get { if (pluginLoader == null) Load(); return pluginLoader; }
		}

		static bool useSeperateDomain;
		public static bool UseSeperateDomain 
		{ 
			set 
			{ 
				if (domain == null) useSeperateDomain = value; 
				else throw new Exception("Can't change after Domain is loaded."); 
			} 
		}

		static void Load()
		{
			if (AppDomain.CurrentDomain.FriendlyName != "OnlineVideosSiteUtilDlls")
			{
				if (useSeperateDomain)
				{
					domain = AppDomain.CreateDomain("OnlineVideosSiteUtilDlls", null, null, null, true);

					// we need to subscribe to AssemblyResolve on the MP2 AppDomain because OnlineVideos.dll is loaded in the LoadFrom Context
					// and when unwrapping transparent proxy from our AppDomain, resolving types will fail because it looks only in the default Load context
					// we simply help .Net by returning the already loaded assembly from the LoadFrom context
					AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;

					pluginLoader = (PluginLoader)domain.CreateInstanceFromAndUnwrap(
					  Assembly.GetExecutingAssembly().Location,
					  typeof(PluginLoader).FullName);

					AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;

					domain.SetData(typeof(PluginLoader).FullName, pluginLoader);
				}
				else
				{
					domain = AppDomain.CurrentDomain;
					pluginLoader = new PluginLoader();
				}
			}
			else
			{
				domain = AppDomain.CurrentDomain;
				pluginLoader = (PluginLoader)AppDomain.CurrentDomain.GetData(typeof(PluginLoader).FullName);
			}
		}

		static Assembly AssemblyResolve(object sender, ResolveEventArgs args)
		{
			// this should only be called to resolve OnlineVideos.dll -> return it regardless of the version, only the name "OnlineVideos"
			AssemblyName an = new AssemblyName(args.Name);
			var asm = (sender as AppDomain).GetAssemblies().FirstOrDefault(a => a.GetName().Name == an.Name);
			return asm;
		}

		internal static object GetCrossDomainSingleton(Type type)
		{
			if (domain == null) Load();

			object instance = domain.GetData(type.FullName); // try to get an instance from the OV domain
			if (instance == null) // no instance in the OV domain yet
			{
				// create an instance in the OV domain
				instance = domain.CreateInstanceAndUnwrap(type.Assembly.FullName, type.FullName, false, BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.NonPublic, null, null, null, null);
				// register the instance in the OV domain
				domain.SetData(type.FullName, instance);
			}
			if (AppDomain.CurrentDomain != domain) // call is coming from the MainAppDomain
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
			AppDomain.Unload(domain);
			domain = null;
			pluginLoader = null;
			if (singletonNames != null)
				foreach (var s in singletonNames)
					s.GetType().InvokeMember("_Instance", BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.SetField, null, s, new object[] { null });
			Load();
		}
	}
}
