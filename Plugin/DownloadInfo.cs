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

    public class DownloadInfo
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
        public Sites.SiteUtilBase Util { get; set; }

        public void DownloadProgressCallback(long TotalBytesToReceive, long currentBytes)
        {
            if (TotalBytesToReceive > 0)
            {
                PercentComplete = (int)((float)currentBytes / TotalBytesToReceive * 100f);
                KbTotal = (int)(TotalBytesToReceive / 1024);
            }
            KbDownloaded = (int)(currentBytes / 1024);
        }
        public void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
        {
            PercentComplete = e.ProgressPercentage;
            KbTotal = (int)(e.TotalBytesToReceive / 1024);
            KbDownloaded = (int)(e.BytesReceived / 1024);
        }
        public void DownloadProgressCallback(object sender, MMSStreamProgressChangedEventArgs e)
        {
            PercentComplete = e.ProgressPercentage;
            KbTotal = (int)(e.TotalBytesToReceive / 1024);
            KbDownloaded = (int)(e.BytesReceived / 1024);
        }
    }
}
