using System.Collections.Generic;
using System.Linq;
using OnlineVideos.CrossDomain;

namespace OnlineVideos.Downloading
{
    public class DownloadManager : CrossDomainSingleton<DownloadManager>
    {
        readonly object _locker = new object();

        readonly Dictionary<string, List<DownloadList>> _currentDownloadsQueuedPerSite;
        readonly List<DownloadList> _currentDownloadsParallel;

        private DownloadManager()
        {
            _currentDownloadsQueuedPerSite = new Dictionary<string, List<DownloadList>>();
            _currentDownloadsParallel = new List<DownloadList>();
        }

        public void Add(string site, DownloadList downloadList)
        {
            lock (_locker)
            {
                if (string.IsNullOrEmpty(site)) _currentDownloadsParallel.Add(downloadList);
                else
                {
                    List<DownloadList> dlList;
                    if (!_currentDownloadsQueuedPerSite.TryGetValue(site, out dlList)) _currentDownloadsQueuedPerSite.Add(site, new List<DownloadList>() { downloadList });
                    else dlList.Add(downloadList);
                }
            }
        }

        public string Remove(DownloadList dlList)
        {
            lock (_locker)
            {
                int index = _currentDownloadsParallel.IndexOf(dlList);
                if (index >= 0)
                {
                    _currentDownloadsParallel.RemoveAt(index);
                }
                else
                {
                    foreach (var item in _currentDownloadsQueuedPerSite)
                    {
                        index = item.Value.IndexOf(dlList);
                        if (index >= 0)
                        {
                            item.Value.RemoveAt(index);
                            if (item.Value.Count == 0) _currentDownloadsQueuedPerSite.Remove(item.Key);
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
            lock (_locker)
            {
                return _currentDownloadsQueuedPerSite.ContainsKey(site);
            }
        }

        public bool Contains(DownloadInfo dlInfo)
        {
            lock (_locker)
            {
                bool result = _currentDownloadsParallel.Any(c =>
                    (c.CurrentItem != null && c.CurrentItem.VideoInfo != null && c.CurrentItem.VideoInfo.VideoUrl == dlInfo.VideoInfo.VideoUrl) ||
                    (c.DownloadItems != null && c.DownloadItems.Any(i => i.VideoInfo.VideoUrl == dlInfo.VideoInfo.VideoUrl)));
                if (!result)
                {
                    result = _currentDownloadsQueuedPerSite.Any(c => c.Value.Any(l =>
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
                lock (_locker)
                {
                    return _currentDownloadsQueuedPerSite.Count + _currentDownloadsParallel.Count;
                }
            }
        }

        public void StopAll()
        {
            lock (_locker)
            {
                while (_currentDownloadsParallel.Count > 0)
                {
                    var dl = _currentDownloadsParallel[0];
                    dl.CurrentItem.Downloader.Abort();
                    _currentDownloadsParallel.RemoveAt(0);
                }
                while (_currentDownloadsQueuedPerSite.Count > 0)
                {
                    var dl = _currentDownloadsQueuedPerSite.First();
                    dl.Value.First().CurrentItem.Downloader.Abort();
                    _currentDownloadsQueuedPerSite.Remove(dl.Key);
                }
            }
        }

        public List<DownloadInfo> GetAll()
        {
            List<DownloadInfo> result = _currentDownloadsParallel.Select(c => c.CurrentItem).ToList();
            lock (_locker)
                result.AddRange(_currentDownloadsQueuedPerSite.Select(c => c.Value).SelectMany(l => l).Select(l => l.CurrentItem));
            return result;
        }

        public DownloadList GetNext(string site)
        {
            lock (_locker)
            {
                List<DownloadList> list;
                if (_currentDownloadsQueuedPerSite.TryGetValue(site, out list))
                {
                    return list.FirstOrDefault();
                }
            }
            return null;
        }
    }
}
