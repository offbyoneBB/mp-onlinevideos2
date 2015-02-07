using System;

namespace OnlineVideos.CrossDomain
{
    /// <summary>
    /// Generic base class for single instance objects that will be accessed from the OnlineVideos AppDomain and the application's AppDomain.
    /// </summary>
    /// <typeparam name="T"></typeparam>
	public class CrossDomainSingleton<T> : MarshalByRefObject where T : class
	{
		protected static T _Instance = null;
		public static T Instance
		{
			get
			{
				if (_Instance == null)
				{
					_Instance = (T)OnlineVideosAppDomain.GetCrossDomainSingleton(typeof(T));
				}
				return _Instance;
			}
		}

		public override object InitializeLifetimeService()
		{
			// In order to have the lease across appdomains live forever, we return null.
			return null;
		}
	}
}
