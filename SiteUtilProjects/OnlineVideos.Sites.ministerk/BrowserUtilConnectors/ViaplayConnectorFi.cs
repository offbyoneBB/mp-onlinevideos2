using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.BrowserUtilConnectors
{
    public class ViaplayConnectorFi : ViaplayConnectorBase
    {
        public override string BaseUrl
        {
            get { return "http://viaplay.fi"; }
        }

        public override string LoginUrl
        {
            get { return "https://account.viaplay.fi/login"; }
        }
    }
}
