using System;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace OnlineVideos
{
	public class DownloadManager : CrossDomanSingletonBase<DownloadManager>
	{
        object locker = new object();

        Dictionary<string, List<DownloadList>> CurrentDownloadsQueuedPerSite;
        List<DownloadList> CurrentDownloadsParallel;

		private DownloadManager()
		{
            CurrentDownloadsQueuedPerSite = new Dictionary<string, List<DownloadList>>();
            CurrentDownloadsParallel = new List<DownloadList>();
		}

        public void Add(string site, DownloadList downloadList)
		{
			lock (locker)
			{
                if (string.IsNullOrEmpty(site)) CurrentDownloadsParallel.Add(downloadList);
                else
                {
                    List<DownloadList> dlList;
                    if (!CurrentDownloadsQueuedPerSite.TryGetValue(site, out dlList)) CurrentDownloadsQueuedPerSite.Add(site, new List<DownloadList>() { downloadList });
                    else dlList.Add(downloadList);
                }
			}
		}

        public string Remove(DownloadList dlList)
		{
            lock (locker)
			{
                int index = CurrentDownloadsParallel.IndexOf(dlList);
                if (index >= 0)
                {
                    CurrentDownloadsParallel.RemoveAt(index);
                }
                else
                {
                    foreach (var item in CurrentDownloadsQueuedPerSite)
                    {
                        index = item.Value.IndexOf(dlList);
                        if (index >= 0)
                        {
                            item.Value.RemoveAt(index);
                            if (item.Value.Count == 0) CurrentDownloadsQueuedPerSite.Remove(item.Key);
                            else return item.Key; // return the name of the site on which this list was queued if there are more lists to download
                            break;
                        }
                    }
                }
                return null;
			}
		}

        public bool Contains(string site)
        {
            return CurrentDownloadsQueuedPerSite.ContainsKey(site);
        }

		public bool Contains(DownloadInfo dlInfo)
		{
            lock (locker)
			{
                bool result = CurrentDownloadsParallel.Any(c => 
                    (c.CurrentItem != null && c.CurrentItem.VideoInfo != null && c.CurrentItem.VideoInfo.VideoUrl == dlInfo.VideoInfo.VideoUrl) || 
                    (c.DownloadItems != null && c.DownloadItems.Any(i => i.VideoInfo.VideoUrl == dlInfo.VideoInfo.VideoUrl)));
                if (!result)
                {
                    result = CurrentDownloadsQueuedPerSite.Any(c => c.Value.Any(l =>
                        (l.CurrentItem != null && l.CurrentItem.VideoInfo != null && l.CurrentItem.VideoInfo.VideoUrl == dlInfo.VideoInfo.VideoUrl) || 
                        (l.DownloadItems != null && l.DownloadItems.Any(i => i.VideoInfo.VideoUrl == dlInfo.VideoInfo.VideoUrl))));
                }
                return result;
			}
		}

		public int Count 
		{
			get 
			{
                lock (locker)
				{
                    return CurrentDownloadsQueuedPerSite.Count + CurrentDownloadsParallel.Count;
				}
			}
		}

		public void StopAll()
		{
            lock (locker)
			{
                while (CurrentDownloadsParallel.Count > 0)
				{
                    var dl = CurrentDownloadsParallel[0];
					dl.CurrentItem.Downloader.Abort();
                    CurrentDownloadsParallel.RemoveAt(0);
				}
                while (CurrentDownloadsQueuedPerSite.Count > 0)
                {
                    var dl = CurrentDownloadsQueuedPerSite.First();
                    dl.Value.First().CurrentItem.Downloader.Abort();
                    CurrentDownloadsQueuedPerSite.Remove(dl.Key);
                }
			}
		}

        public List<DownloadInfo> GetAll()
        {
            List<DownloadInfo> result = CurrentDownloadsParallel.Select(c => c.CurrentItem).ToList();
            result.AddRange(CurrentDownloadsQueuedPerSite.Select(c => c.Value).SelectMany(l => l).Select(l => l.CurrentItem));
            return result;
        }

        public DownloadList GetNext(string site)
        {
            List<DownloadList> list;
            if (CurrentDownloadsQueuedPerSite.TryGetValue(site, out list))
            {
                return list.FirstOrDefault();
            }
            return null;
        }
	}

    public class DownloadList : MarshalByRefObject
    {
        #region MarshalByRefObject overrides
        public override object InitializeLifetimeService()
        {
            // In order to have the lease across appdomains live forever, we return null.
            return null;
        }
        #endregion

		public static DownloadList Create(DownloadInfo currentItem)
		{
			DownloadList di = (DownloadList)OnlineVideosAppDomain.Domain.CreateInstanceAndUnwrap(typeof(DownloadList).Assembly.FullName, typeof(DownloadList).FullName, false, System.Reflection.BindingFlags.CreateInstance | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, null, null, null);
			di.CurrentItem = currentItem;
			return di;
		}

		protected DownloadList() {}

        public DownloadInfo CurrentItem { get; set; }
        public List<DownloadInfo> DownloadItems { get; set; }
        public string ChosenPlaybackOption { get; set; }
    }

	public class DownloadInfo : MarshalByRefObject, System.ComponentModel.INotifyPropertyChanged
    {
		#region MarshalByRefObject overrides
		public override object InitializeLifetimeService()
		{
			// In order to have the lease across appdomains live forever, we return null.
			return null;
		}
		#endregion

		public static DownloadInfo Create(VideoInfo video, Category category, Sites.SiteUtilBase site)
		{
			DownloadInfo di = (DownloadInfo)OnlineVideosAppDomain.Domain.CreateInstanceAndUnwrap(typeof(DownloadInfo).Assembly.FullName, typeof(DownloadInfo).FullName, false, System.Reflection.BindingFlags.CreateInstance | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, null, null, null);
			di.VideoInfo = video;
			di.Category = category;
			di.Util = site;
			return di;
		}

        protected DownloadInfo()
        {
            Start = DateTime.Now;
        }

        public string Url { get; set; }
        public string Title { get; set; }
        public string ThumbFile { get; set; }
        public string LocalFile { get; set; }
        public DateTime Start { get; set; }
        public int PercentComplete { get; private set; }
        public int KbDownloaded { get; private set; }
        public int KbTotal { get; private set; }
        public IDownloader Downloader { get; set; }
        public VideoInfo VideoInfo { get; set; }
		public Category Category { get; set; }
		public Sites.SiteUtilBase Util { get; set; }
		public string OverrideFolder { get; set; }
		public string OverrideFileName { get; set; }

        public void DownloadProgressCallback(long TotalBytesToReceive, long currentBytes)
        {
            if (TotalBytesToReceive > 0)
            {
                PercentComplete = (int)((float)currentBytes / TotalBytesToReceive * 100f);
                KbTotal = (int)(TotalBytesToReceive / 1024);
            }
            KbDownloaded = (int)(currentBytes / 1024);
            NotifyPropertyChanged("ProgressInfo");
        }
        public void DownloadProgressCallback(byte percent)
        {
            PercentComplete = percent;
            NotifyPropertyChanged("ProgressInfo");
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string property)
        {
            if (PropertyChanged != null) PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(property));
        }

        public string ProgressInfo
        {
            get
            {
                return (PercentComplete != 0 || KbTotal != 0 || KbDownloaded != 0) ?
                    string.Format("{0}{1} KB - {2} KB/sec", PercentComplete > 0 ? PercentComplete + "% / " : "", KbTotal > 0 ? KbTotal.ToString("n0") : KbDownloaded.ToString("n0"), (int)(KbDownloaded / (DateTime.Now - Start).TotalSeconds)) : Translation.Instance.Queued;
            }
        }
    }
}
