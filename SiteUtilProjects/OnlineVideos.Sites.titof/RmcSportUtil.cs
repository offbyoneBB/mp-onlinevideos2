namespace OnlineVideos.Sites
{
    public class RmcSportUtil : BFMTVUtil
    {
        #region Methods

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            base._title = "RMC Sport";
            base._img = "rmcsport";

            base._urlToken = "http://api.nextradiotv.com/rmcsport-android/3/";
            base._urlMenu = "http://api.nextradiotv.com/rmcsport-android/3/{0}/getMainMenu";
            base._urlVideoList = "http://api.nextradiotv.com/rmcsport-android/3/{0}/getVideosList?count=40&page=1&category={1}";
            base._urlVideo = "http://api.nextradiotv.com/rmcsport-android/3/{0}/getVideo?idVideo={1}&quality=2";
        }

        #endregion Methods
    }
}