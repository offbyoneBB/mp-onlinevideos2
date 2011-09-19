using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using OnlineVideos.Hoster.Base;
using System.Xml;
using System.Web;
using OnlineVideos.Sites;

namespace OnlineVideos.Hoster
{
    /// <summary>
    /// Hoster class for www.sockshare.com
    /// 
    /// It's basically the same as putlocker.com, only on a different domain
    /// </summary>
    public class SockShare : PutLocker
    {
        public override string getHosterUrl()
        {
            return "sockshare.com";
        }
    }
}
