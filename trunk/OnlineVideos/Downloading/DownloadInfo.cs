using System;
using System.ComponentModel;

namespace OnlineVideos.Downloading
{
	public class DownloadInfo : MarshalByRefObject, INotifyPropertyChanged
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
        
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string property)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(property));
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
