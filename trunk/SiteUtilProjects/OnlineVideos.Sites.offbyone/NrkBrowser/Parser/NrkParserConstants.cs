
namespace Vattenmelon.Nrk.Parser
{
    public class NrkParserConstants
    {
       

        public static string STREAM_PREFIX
        {
            get { return "mms://straumV.nrk.no/nrk_tv_webvid"; }
        }

        public static string RSS_URL
        {
            get { return "http://www1.nrk.no/nett-tv/MediaRss.ashx?loop="; }
        }

        public static string URL_GET_MEDIAXML
        {
            get { return "http://www1.nrk.no/nett-tv/silverlight/getmediaxml.ashx?id={0}&hastighet={1}"; }
        }

        public static string GEOBLOCK_URL_PART
        {
            get { return "geoblokk"; }
        }
        
        public static string RSS_CLIPURL_PREFIX
        {
            get { return "http://pd.nrk.no/nett-tv-stream/stream.ashx?id="; }
        }

        public static string QTICKET
        {
            get { return "qticket"; }
        }

        public static string VIGNETT_ID_NATURE
        {
            get { return "372980"; }
        }

        public static string VIGNETT_ID_SUPER
        {
            get { return "381994"; }
        }

        public static string VIGNETT_ID_NYHETER
        {
            get { return "410330"; }
        }

        public static string VIGNETT_ID_SPORT
        {
            get { return "410335"; }
        }

        public static string MOST_WATCHED_DATA_TO_POST
        {
            get { return "?=&__EVENTARGUMENT=&__EVENTTARGET=ctl00%24contentPlaceHolder%24asyncPBTrigger_loop_ms{0}&__VIEWSTATE={1}contentPlaceHolder%24asyncPBparent=&ctl00%24contentPlaceHolder%24asyncPBstory=&ctl00%24contentPlaceHolder%24asyncPBtitle=&ctl00%24contentPlaceHolder%24mainCat={2}&ctl00%24contentPlaceHolder%24nowPlaying=&ctl00%24contentPlaceHolder%24subCat=&ctl00%24scriptManager1=ctl00%24contentPlaceHolder%24loopPanel%7Cctl00%24contentPlaceHolder%24asyncPBTrigger_loop_ms{3}&ctl00%24ucTop%24userSearch="; }
        }
       
        public static string MOST_WATCHED_VIEWSTATE
        {
            get { return "%2FwEPDwULLTIxMzkzNTA3MjUPZBYCZg9kFgICARBkZBYCAgcPZBYEAgUPZBYCAgIPZBYEAgEPFgIeB1Zpc2libGVnZAIDD2QWAgIBD2QWAmYPZBYCAgEPEGRkFgFmZAIXDxYCHghJbnRlcnZhbAKg9zZkZEWTMLTSwpx%2B2iwZfWAa8UkN0Uml&ctl00%24"; }
        }

        public static string NRK_BETA_FEEDS_URL
        {
            get { return "http://video.nrkbeta.no/feeds/"; }
        }

        public static string NRK_BETA_FEEDS_KATEGORI_URL
        {
            get { return NRK_BETA_FEEDS_URL + "kategori/"; }
        }

        public static string NRK_BETA_FEEDS_HD_CLIPS_URL
        {
            get { return NRK_BETA_FEEDS_URL + "hd/"; }
        }

        public static string NRK_BETA_FEEDS_LATEST_CLIPS_URL
        {
            get { return NRK_BETA_FEEDS_URL + "siste/"; }
        }

        public static string NRK_BETA_FEEDS_SOK_URL
        {
            get { return NRK_BETA_FEEDS_URL + "sok/{0}/"; }
        }

        public static string NRK_BETA_SECTION_TV_SERIES
        {
            get { return "tv-serier"; }
        }

        public static string NRK_BETA_SECTION_DIVERSE
        {
            get { return "diverse"; }
        }

        public static string NRK_BETA_SECTION_PRESENTASJONER
        {
            get { return "presentasjoner"; }
        }

        public static string NRK_BETA_SECTION_KONFERANSER_OG_MESSER
        {
            get { return "konferanser-og-messer"; }
        }

        public static string NRK_BETA_SECTION_FRA_TV
        {
            get { return "fra-tv"; }
        }

        public static string NRK_BETA_SECTION_LYDFILER
        {
            get { return "lydfiler"; }
        }

        public static string NRK_BETA_THUMBNAIL_URL
        {
            get { return "http://video.nrkbeta.no/media/filer/videofiler/thumbnails/{0}.jpg"; }
        }
        public const string LIBRARY_NAME = "NrkParser";

    }
}
