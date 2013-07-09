using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Pondman
{
    abstract public class BaseUtil : SiteUtilBase
    {

        /// <summary>
        /// Make a webrequest
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        protected virtual string doWebRequest(string uri)
        {
            return GetWebData(uri, forceUTF8: true);
        }
    }
}
