using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites
{
    public class RmcSportUtil : BFMTVUtil
    {
        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            base._title = "RMC Sport";
            base._img = "rmcsport";

            base._urlToken = "http://api.nextradiotv.com/rmcsport-android/3/";
            base._urlMenu = "http://api.nextradiotv.com/rmcsport-android/3/{0}/getMainMenu";
            base._urlVideoList = "http://api.nextradiotv.com/rmcsport-android/3/{0}/getVideosList?count=40&page=1&category={1}";
            base._urlVideo = "http://api.nextradiotv.com/rmcsport-android/3/{0}/getVideo?idVideo={1}&quality=2";

            //base._urlToken = "http://api.nextradiotv.com/01net-android/3/";

            //base._urlMenu = "http://api.nextradiotv.com/01net-android/3/{0}/getMainMenu";
            //base._urlVideoList = "http://api.nextradiotv.com/01net-android/3/{0}/getVideosList?count=40&page=1&category={1}";
            //base._urlVideo = "http://api.nextradiotv.com/01net-android/3/{0}/getVideo?idVideo={1}&quality=2";

            //api.nextradiotv.com/01net-android/3/
        }
    }
}
