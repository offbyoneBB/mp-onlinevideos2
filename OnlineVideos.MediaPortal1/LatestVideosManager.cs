using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using OnlineVideos.Downloading;
using OnlineVideos.Helpers;

namespace OnlineVideos.MediaPortal1
{
	public class LatestVideosManager
	{
		Thread workerThread;
		bool pause = false;

		public void Start()
		{
			pause = false;
			if (workerThread == null && PluginConfiguration.Instance.LatestVideosMaxItems > 0)
			{
				workerThread = new Thread(Worker) { IsBackground = true, Name = "OVLatest" };
				workerThread.Start();
			}
		}

		public void Pause()
		{
			pause = true;
		}

		public void Stop()
		{
			if (workerThread != null) workerThread.Abort();
		}

		void Worker()
		{
			bool setOnce = false;
			uint currentRotationIndex = 0;
			DateTime lastDiscovery = DateTime.MinValue;
			List<KeyValuePair<string, VideoInfo>> latestVideos = new List<KeyValuePair<string, VideoInfo>>();
			try
			{
				while (true)
				{
					if ((DateTime.Now - lastDiscovery).TotalMinutes > PluginConfiguration.Instance.LatestVideosOnlineDataRefresh)
					{
						Dictionary<string, List<VideoInfo>> latestVideosPerSite = DiscoverAllLatestVideos();
						lastDiscovery = DateTime.Now;
						currentRotationIndex = 0;
						setOnce = false;
						int previousLatetsVideosCount = latestVideos.Count;
						latestVideos.Clear();
						foreach (var l in latestVideosPerSite) latestVideos.AddRange(l.Value.Select(v => new KeyValuePair<string, VideoInfo>(l.Key, v)));
						Log.Instance.Info("LatestVideosManager found {0} videos from {1} SiteUtils.", latestVideos.Count, latestVideosPerSite.Count);
						int less = Math.Min(previousLatetsVideosCount, (int)PluginConfiguration.Instance.LatestVideosMaxItems) - Math.Min(latestVideos.Count, (int)PluginConfiguration.Instance.LatestVideosMaxItems);
						while (less > 0)
						{
							// reset the GuiProperties in case we found less latest videos than before and than should be shown in rotation
							ResetLatestVideoGuiProperties((int)PluginConfiguration.Instance.LatestVideosMaxItems - less + 1);
							less--;
						}
						GUIPropertyManager.SetProperty("#OnlineVideos.LatestVideosEnabled", (Math.Min(latestVideos.Count, PluginConfiguration.Instance.LatestVideosMaxItems) > 0).ToString().ToLower());
						if (latestVideos.Count > 0)
						{
							if (PluginConfiguration.Instance.LatestVideosRandomize) latestVideos.Randomize();
							ImageDownloader.DownloadImages<VideoInfo>(latestVideos.Select(v => v.Value).ToList());
						}
					}
					if (latestVideos.Count > 0 && (!setOnce || latestVideos.Count > PluginConfiguration.Instance.LatestVideosMaxItems)) // only needed ONCE if there are no more latestVideos than amount to be shown
					{
						for (int i = 1; i <= Math.Min(latestVideos.Count, PluginConfiguration.Instance.LatestVideosMaxItems); i++)
						{
							int num = (int)currentRotationIndex + i - 1;
							if (num >= latestVideos.Count) num = i - 1;
							SetLatestVideoGuiProperties(latestVideos[num], i);
						}
						setOnce = true;
						currentRotationIndex++;
						if (currentRotationIndex >= latestVideos.Count) currentRotationIndex = 0;
					}
					Thread.Sleep(1000 * (int)PluginConfiguration.Instance.LatestVideosGuiDataRefresh);
					// don't do anything while Fullscreen Playback
					while (g_Player.FullScreen || pause) Thread.Sleep(1000);
				}
			}
			catch (ThreadAbortException)
			{
				Thread.ResetAbort(); // finish gracefully when thread was forcibly aborted
			}
			catch (Exception ex)
			{
				Log.Instance.Warn("LatestVideos thread ended unexpected: {0}", ex.Message);
			}
			workerThread = null;
		}

		Dictionary<string, List<VideoInfo>> DiscoverAllLatestVideos()
		{
			Log.Instance.Info("LatestVideosManager getting new data from SiteUtils.");
			Dictionary<string, List<VideoInfo>> latestVideos = new Dictionary<string, List<VideoInfo>>();
			foreach (var site in OnlineVideoSettings.Instance.LatestVideosSiteUtilsList)
			{
				if (site.LatestVideosCount > 0)
				{
					try
					{
						var l = site.GetLatestVideos();
						if (l != null && l.Count > 0)
						{
							latestVideos.Add(site.Settings.Name, l.Take((int)site.LatestVideosCount).ToList());
						}
					}
					catch (Exception ex)
					{
						Log.Instance.Warn("Error getting latest videos from '{0}': {1}", site.Settings.Name, ex.Message);
					}
				}
			}
			return latestVideos;
		}

		void SetLatestVideoGuiProperties(KeyValuePair<string, VideoInfo> video, int index)
		{
			GUIPropertyManager.SetProperty(string.Format("#OnlineVideos.LatestVideo{0}.Site", index), video.Key);

			string siteIcon = SiteImageExistenceCache.GetImageForSite(video.Key, null, "Icon");
			if (string.IsNullOrEmpty(siteIcon)) siteIcon = SiteImageExistenceCache.GetImageForSite("OnlineVideos", type: "Icon");
			if (siteIcon == null) siteIcon = string.Empty;
			GUIPropertyManager.SetProperty(string.Format("#OnlineVideos.LatestVideo{0}.SiteIcon", index), siteIcon);

			GUIPropertyManager.SetProperty(string.Format("#OnlineVideos.LatestVideo{0}.Title", index), video.Value.Title);
			GUIPropertyManager.SetProperty(string.Format("#OnlineVideos.LatestVideo{0}.Aired", index), video.Value.Airdate);
			GUIPropertyManager.SetProperty(string.Format("#OnlineVideos.LatestVideo{0}.Duration", index), video.Value.Length);
			GUIPropertyManager.SetProperty(string.Format("#OnlineVideos.LatestVideo{0}.Thumb", index), video.Value.ThumbnailImage);
			GUIPropertyManager.SetProperty(string.Format("#OnlineVideos.LatestVideo{0}.Description", index), video.Value.Description);
		}

		void ResetLatestVideoGuiProperties(int index)
		{
			GUIPropertyManager.SetProperty(string.Format("#OnlineVideos.LatestVideo{0}.Site", index), string.Empty);
			GUIPropertyManager.SetProperty(string.Format("#OnlineVideos.LatestVideo{0}.SiteIcon", index), string.Empty);
			GUIPropertyManager.SetProperty(string.Format("#OnlineVideos.LatestVideo{0}.Title", index), string.Empty);
			GUIPropertyManager.SetProperty(string.Format("#OnlineVideos.LatestVideo{0}.Aired", index), string.Empty);
			GUIPropertyManager.SetProperty(string.Format("#OnlineVideos.LatestVideo{0}.Duration", index), string.Empty);
			GUIPropertyManager.SetProperty(string.Format("#OnlineVideos.LatestVideo{0}.Thumb", index), string.Empty);
			GUIPropertyManager.SetProperty(string.Format("#OnlineVideos.LatestVideo{0}.Description", index), string.Empty);

		}
	}
}
