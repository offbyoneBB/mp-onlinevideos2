using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites
{
    public class D17Util: Direct8Util
    {

        #region Methods

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            base._siteindex = "2";
            base._sitekey = "cstar";
            base._baselive = "http://hls-live-m5-l3.canal-plus.com/live/hls/d17-clair-hd-and/and-hd-clair/index.m3u8";
        }

        #endregion Methods
    }
}
