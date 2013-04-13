using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Web;
using System.Net;
using System.Xml;
using System.Text.RegularExpressions;
using System.Collections;
using System.ComponentModel;
using System.Security.Cryptography;
using System.IO;
using System.Text;

namespace OnlineVideos.Sites
{
    public class W9ReplayUtil : M6ReplayUtil
    {
        [Category("OnlineVideosConfiguration"), Description("site identifier")]
        string w9SiteIdentifier = "w9";

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            base.siteIdentifier = w9SiteIdentifier;
        }
    }
}
