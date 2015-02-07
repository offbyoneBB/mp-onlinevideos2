using System.Collections.Generic;
using System.Linq;
using OnlineVideos.CrossDomain;

namespace OnlineVideos.Downloading
{
    public class DownloadManager : CrossDomainSingleton<DownloadManager>
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
}
