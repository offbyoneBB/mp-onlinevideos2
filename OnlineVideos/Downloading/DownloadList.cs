using System;
using System.Collections.Generic;

namespace OnlineVideos.Downloading
{
    public class DownloadList : MarshalByRefObject
    {
        #region MarshalByRefObject overrides
        public override object InitializeLifetimeService()
        {
            // In order to have the lease across appdomains live forever, we return null.
            return null;
        }
        #endregion

        public static DownloadList Create(DownloadInfo currentItem)
        {
            DownloadList di = (DownloadList)OnlineVideosAppDomain.Domain.CreateInstanceAndUnwrap(typeof(DownloadList).Assembly.FullName, typeof(DownloadList).FullName, false, System.Reflection.BindingFlags.CreateInstance | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, null, null, null);
            di.CurrentItem = currentItem;
            return di;
        }

        protected DownloadList() { }

        public DownloadInfo CurrentItem { get; set; }
        public List<DownloadInfo> DownloadItems { get; set; }
        public string ChosenPlaybackOption { get; set; }
    }
}
