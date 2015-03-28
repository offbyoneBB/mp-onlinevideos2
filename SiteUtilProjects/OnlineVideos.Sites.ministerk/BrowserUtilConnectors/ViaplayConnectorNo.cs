using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.BrowserUtilConnectors
{
    public class ViaplayConnectorNo : ViaplayConnectorBase
    {
        public override string BaseUrl
        {
            get { return "http://viaplay.no"; }
        }

        public override string LoginUrl
        {
            get { return "https://account.viaplay.no/login"; }
        }
    }
}
