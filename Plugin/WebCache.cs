using System;
using System.Collections.Generic;
using System.Threading;

namespace OnlineVideos
{
    public class WebCache
    {
        class WebCacheEntry
        {
            public DateTime LastUpdated { get; set; }
            public string Data { get; set; }
        }

        #region Singleton
        WebCache() 
        {
            // only use cache if a timeout > 0 was set
            if (OnlineVideoSettings.Instance.CacheTimeout > 0)
            {
                cleanUpTimer = new Timer(CleanCache, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
            }
        }
        static WebCache instance;
        public static WebCache Instance { get { if (instance == null) instance = new WebCache(); return instance; } }
        #endregion

        Timer cleanUpTimer;
        Dictionary<string, WebCacheEntry> cache = new Dictionary<string, WebCacheEntry>();

        public string this[string url]
        {
            get 
            {
                if (OnlineVideoSettings.Instance.CacheTimeout > 0) // only use cache if a timeout > 0 was set
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
                if (OnlineVideoSettings.Instance.CacheTimeout > 0) // only use cache if a timeout > 0 was set
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
                    if ((DateTime.Now - cache[key].LastUpdated).TotalMinutes >= OnlineVideoSettings.Instance.CacheTimeout)
                        outdatedKeys.Add(key);

                foreach(string key in outdatedKeys) cache.Remove(key);
            }
        }
    }
}
