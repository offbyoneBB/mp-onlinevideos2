using System;

namespace OnlineVideos
{
	public class CrossDomanSingletonBase<T> : MarshalByRefObject where T : class
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
