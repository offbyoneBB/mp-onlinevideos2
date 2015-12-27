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
            base._sitekey = "d17";
        }

        #endregion Methods
    }
}
