using System;

namespace OnlineVideos.Downloading
{
    public interface IDownloader
    {
        bool Cancelled { get; }
        void CancelAsync();
        void Abort();
        Exception Download(DownloadInfo downloadInfo);
    }
}
