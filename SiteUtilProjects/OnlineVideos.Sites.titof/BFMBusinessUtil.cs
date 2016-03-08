namespace OnlineVideos.Sites
{
    public class BFMBusinessUtil : BFMTVUtil
    {
        #region Methods

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            base._title = "BFM Business";
            base._img = "bfmbusiness";
            base._urlToken = "http://api.nextradiotv.com/bfmbusiness-iphone/3/";
            base._urlMenu = "http://api.nextradiotv.com/bfmbusiness-iphone/3/{0}/getMainMenu";
            base._urlVideoList = "http://api.nextradiotv.com/bfmbusiness-iphone/3/{0}/getVideosList?count=40&page=1&category={1}";
            base._urlVideo = "http://api.nextradiotv.com/bfmbusiness-iphone/3/{0}/getVideo?idVideo={1}&quality=2";
        }

        #endregion Methods
    }
}