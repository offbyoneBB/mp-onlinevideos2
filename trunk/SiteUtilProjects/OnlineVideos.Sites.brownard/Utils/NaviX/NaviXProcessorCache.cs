using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace OnlineVideos.Sites.Utils.NaviX
{
    class NaviXProcessorCache
    {
        #region Singleton
        static object instanceSync = new object();
        static NaviXProcessorCache instance = null;
        public static NaviXProcessorCache Instance
        {
            get
            {
                if (instance == null)
                    lock (instanceSync)
                        if (instance == null)
                            instance = new NaviXProcessorCache();
                return instance;
            }
        }
        #endregion

        class WebCacheEntry
        {
            public DateTime LastUpdated { get; set; }
            public string Data { get; set; }
        }

        NaviXProcessorCache()
        {
            cacheTimeout = OnlineVideoSettings.Instance.CacheTimeout;
            if (cacheTimeout > 0)
            {
                cleanUpTimer = new Timer(CleanCache, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
            }
        }

        int cacheTimeout;
        Timer cleanUpTimer;
        Dictionary<string, WebCacheEntry> cache = new Dictionary<string, WebCacheEntry>();

        public string this[string url]
        {
            get
            {
                if (cacheTimeout > 0) // only use cache if a timeout > 0 was set
                {
                    lock (this)
                    {
                        WebCacheEntry result = null;
                        if (cache.TryGetValue(url, out result))
                        {
                            return result.Data;
                        }
                    }
                }
                return null;
            }
            set
            {
                if (cacheTimeout > 0) // only use cache if a timeout > 0 was set
                {
                    lock (this)
                    {
                        cache[url] = new WebCacheEntry() { Data = value, LastUpdated = DateTime.Now };
                    }
                }
            }
        }

        void CleanCache(object state)
        {
            lock (this)
            {
                List<string> outdatedKeys = new List<string>();

                foreach (string key in cache.Keys)
                    if ((DateTime.Now - cache[key].LastUpdated).TotalMinutes >= cacheTimeout)
                        outdatedKeys.Add(key);

                foreach (string key in outdatedKeys) cache.Remove(key);
            }
        }
    }
}
