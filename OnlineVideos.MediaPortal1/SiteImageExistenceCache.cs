using System.Collections.Generic;
using System.IO;
using MediaPortal.GUI.Library;

namespace OnlineVideos.MediaPortal1
{
	/// <summary>
	/// This class is used to lower the number of File.Exists checks for a Site's Icon and Banner. The exposed methods are threadsafe.
	/// </summary>
	internal static class SiteImageExistenceCache
	{
		static Dictionary<string, string> cachedImageForSite = new Dictionary<string, string>();

		internal static string GetImageForSite(string siteName, string utilName = "", string type = "Banner", bool logIfNotfound = true)
		{
			lock (cachedImageForSite)
			{
				string image = null;
				if (!cachedImageForSite.TryGetValue(string.Format("{0}{1}", siteName, type), out image))
				{
					// use png with the same name as the Site - first check subfolder of current skin (allows skinners to use custom icons)
					image = string.Format(@"{0}\Media\OnlineVideos\{1}s\{2}.png", GUIGraphicsContext.Skin, type, siteName);
					if (!File.Exists(image))
					{
						// use png with the same name as the Site
						image = string.Format(@"{0}{1}s\{2}.png", OnlineVideoSettings.Instance.ThumbsDir, type, siteName);
						if (!File.Exists(image))
						{
							image = string.Empty;
							// if that does not exist, try image with the same name as the Util
							if (!string.IsNullOrEmpty(utilName))
							{
								image = string.Format(@"{0}{1}s\{2}.png", OnlineVideoSettings.Instance.ThumbsDir, type, utilName);
								if (!File.Exists(image)) image = string.Empty;
							}
						}
					}
					if (logIfNotfound && string.IsNullOrEmpty(image)) Log.Instance.Debug("{0} for site '{1}' not found!", type, siteName);
					cachedImageForSite[string.Format("{0}{1}", siteName, type)] = image;
				}
				return image;
			}
		}

		/// <summary>
		/// Removes the Banner and Icon of the specified Site from the Cache, so the HDD is rechecked.
		/// </summary>
		/// <param name="siteName">The name of the site</param>
		internal static void UnCacheImageForSite(string siteName)
		{
			if (!string.IsNullOrEmpty(siteName))
			{
				lock (cachedImageForSite)
				{
					cachedImageForSite.Remove(string.Format("{0}{1}", siteName, "Icon"));
					cachedImageForSite.Remove(string.Format("{0}{1}", siteName, "Banner"));
				}
			}
		}

		/// <summary>
		/// Removes all cached Infos.
		/// </summary>
		internal static void ClearCache()
		{
			lock (cachedImageForSite)
			{
				cachedImageForSite.Clear();
			}
		}
	}
}
