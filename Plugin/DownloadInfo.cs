using System;
using System.Net;

namespace OnlineVideos
{
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
        public DateTime Start { get; private set; }
        public int PercentComplete { get; private set; }
        public int KbTotal { get; private set; }        
        public void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
        {
            PercentComplete = e.ProgressPercentage;
            KbTotal = (int)(e.TotalBytesToReceive / 1024);
        }
        public void DownloadProgressCallback(object sender, MMSStreamProgressChangedEventArgs e)
        {
            PercentComplete = e.ProgressPercentage;
            KbTotal = (int)(e.TotalBytesToReceive / 1024);
        }
    }
}
