using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.BrowserUtilConnectors
{
    public class ViaplayConnectorDa : ViaplayConnectorBase
    {
        public override string BaseUrl
        {
            get { return "http://viaplay.dk"; }
        }

        public override string LoginUrl
        {
            get { return "https://account.viaplay.dk/login"; }
        }

    }
}
