using System;
using System.Net;
using System.Collections.Generic;

namespace OnlineVideos
{
    public class DownloadList
    {
        public DownloadInfo CurrentItem { get; set; }
        public List<DownloadInfo> DownloadItems { get; set; }
        public string ChosenPlaybackOption { get; set; }
    }

    public class DownloadInfo : System.ComponentModel.INotifyPropertyChanged
    {
        public DownloadInfo()
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
