using System;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace OnlineVideos
{
	public class DownloadManager : CrossDomanSingletonBase<DownloadManager>
	{
		internal Dictionary<string, DownloadInfo> CurrentDownloads { get; private set; }

		private DownloadManager()
		{
			CurrentDownloads = new Dictionary<string, DownloadInfo>();
		}

		public void Add(string url, DownloadInfo downloadInfo)
		{
			lock (CurrentDownloads)
			{
				CurrentDownloads.Add(url, downloadInfo);
			}
		}

		public void Remove(string url)
		{
			lock (CurrentDownloads)
			{
				CurrentDownloads.Remove(url);
			}
		}

		public bool Contains(string url)
		{
			lock (CurrentDownloads)
			{
				return CurrentDownloads.ContainsKey(url);
			}
		}

		public int Count 
		{
			get 
			{
				lock (CurrentDownloads)
				{
					return CurrentDownloads.Count;
				}
			}
		}

		public void StopAll()
		{
			lock (CurrentDownloads)
			{
				while (CurrentDownloads.Count > 0)
				{
					var dl = CurrentDownloads.First();
					dl.Value.Downloader.Abort();
					CurrentDownloads.Remove(dl.Key);
				}
			}
		}
	}

    public class DownloadList
    {
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
			DownloadInfo di = (DownloadInfo)OnlineVideosAppDomain.Domain.CreateInstanceAndUnwrap(typeof(DownloadInfo).Assembly.FullName, typeof(DownloadInfo).FullName, false, System.Reflection.BindingFlags.CreateInstance | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, null, null, null, null);
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
        public void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
        {
            PercentComplete = e.ProgressPercentage;
            KbTotal = (int)(e.TotalBytesToReceive / 1024);
            KbDownloaded = (int)(e.BytesReceived / 1024);
            NotifyPropertyChanged("ProgressInfo");
        }
        public void DownloadProgressCallback(object sender, MMSStreamProgressChangedEventArgs e)
        {
            PercentComplete = e.ProgressPercentage;
            KbTotal = (int)(e.TotalBytesToReceive / 1024);
            KbDownloaded = (int)(e.BytesReceived / 1024);
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
                    string.Format("{0}{1} KB - {2} KB/sec", PercentComplete > 0 ? PercentComplete + "% / " : "", KbTotal > 0 ? KbTotal.ToString("n0") : KbDownloaded.ToString("n0"), (int)(KbDownloaded / (DateTime.Now - Start).TotalSeconds)) : "";
            }
        }
    }
}
