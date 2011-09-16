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
        [Category("OnlineVideosConfiguration"), Description("CatalogueWeb")]
        string W9catalogueWeb = "http://www.w9replay.fr/catalogue/4398.xml";
        [Category("OnlineVideosConfiguration"), Description("ThumbURL")]
        string W9thumbURL = "http://images.w9replay.fr";

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            base.catalogueWeb = W9catalogueWeb;
            base.thumbURL = W9thumbURL;
        }
    }
}
