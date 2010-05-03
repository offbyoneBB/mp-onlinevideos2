/*
 * Created by: 
 * Created: 7. november 2009
 */

namespace Vattenmelon.Nrk.Browser
{
    public class NrkBrowserConstants
    {
        public static string MENU_ITEM_ID_NRKBETA
        {
            get { return "nrkbeta"; }
        }

        public static string MENU_ITEM_ID_NRKBETA_SEARCH
        {
            get { return "nrkbetasearch"; }
        }

        public static string MENU_ITEM_ID_NRKBETA_SISTE_KLIPP
        {
            get { return "nrkbetasisteklipp"; }
        }

        public static string MENU_ITEM_ID_NRKBETA_HD_KLIPP
        {
            get { return "nrkbetahdklipp"; }
        }

        public static string MENU_ITEM_ID_NRKBETA_FRA_TV
        {
            get { return "nrkbetafratv"; }
        }

        public static string MENU_ITEM_ID_NRKBETA_KONFERANSER_OG_MESSER
        {
            get { return "nrkbetakonferogmesser"; }
        }

        public static string MENU_ITEM_ID_NRKBETA_PRESENTASJONER
        {
            get { return "nrkbetapresentasjoner"; }
        }

        public static string MENU_ITEM_ID_NRKBETA_TVSERIER
        {
            get { return "nrkbetatvserier"; }
        }
        public static string MENU_ITEM_ID_NRKBETA_DIVERSE
        {
            get { return "nrkbetadiverse"; }
        }
        public static string MENU_ITEM_ID_MEST_SETTE_UKE
        {
            get { return "mestSettUke"; }
        }

        public static string MENU_ITEM_ID_MEST_SETTE_MAANED
        {
            get { return "mestSettMaaned"; }
        }

        public static string MENU_ITEM_ID_MEST_SETTE_TOTALT
        {
            get { return "mestSettTotalt"; }
        }

        public static string GUI_PROPERTY_PROGRAM_PICTURE
        {
            get { return "#picture"; }
        }

        public static string GUI_PROPERTY_PROGRAM_DESCRIPTION
        {
            get { return "#description"; }
        }

        public static string GUI_PROPERTY_CLIP_COUNT
        {
            get { return "#clipcount"; }
        }

        public static string ABOUT_DESCRIPTION
        {
            get { return "Plugin for watching NRK nett-tv"; }
        }

        public static string ABOUT_AUTHOR
        {
            get { return "Vattenmelon"; }
        }

        public static string CONFIG_FILE
        {
            get { return "NrkBrowserSettings.xml"; }
        }

        public static string CONFIG_SECTION
        {
            get { return "NrkBrowser"; }
        }

        public static string CONFIG_ENTRY_PLUGIN_NAME
        {
            get { return "pluginName"; }
        }

        public static string CONFIG_ENTRY_SPEED
        {
            get { return "speed"; }
        }

        public static string CONFIG_ENTRY_LIVE_STREAM_QUALITY
        {
            get { return "liveStreamQuality"; }
        }

        public static string SKIN_FILENAME
        {
            get { return "NrkBrowser.xml"; }
        }

        public static string MEDIAPORTAL_CONFIG_FILE
        {
            get { return "MediaPortal.xml"; }
        }

        public static string LANGUAGE_DIR
        {
            get { return CONFIG_SECTION; }
        }

        public static string CONTEXTMENU_HEADER_TEXT
        {
            get { return "NRK Browser"; }
        }

        public static string MESSAGE_BOX_HEADER_TEXT
        {
            get { return "NRK Browser"; }
        }

        public static string MENU_ITEM_ID_FAVOURITES
        {
            get { return "favoritter"; }
        }

        public static string SEARCH_NEXTPAGE_ID
        {
            get { return "nextPage"; }
        }

        /// <summary>
        /// The minimum time start-time of the clip should be before we wants to seek.
        /// </summary>
        public static double MINIMUM_TIME_BEFORE_SEEK
        {
            get { return 4; }
        }

        public static string MENU_ITEM_PICTURE_NYHETER
        {
            get { return "nrknyheter.jpg"; }
        }

        public static string MENU_ITEM_PICTURE_SPORT
        {
            get { return "nrksport.jpg"; }
        }

        public static string MENU_ITEM_PICTURE_NATURE
        {
            get { return "nrknatur.jpg"; }
        }

        public static string MENU_ITEM_PICTURE_SUPER
        {
            get { return "nrksuper.jpg"; }
        }

        public static string NRK_LOGO_PICTURE
        {
            get { return "nrklogo.jpg"; }
        }

        public static string MENU_ITEM_PICTURE_NRKBETA
        {
            get { return "nrkbeta.png"; }
        }

        public static string DEFAULT_PICTURE
        {
            get { return "http://fil.nrk.no/contentfile/web/bgimages/special/nettv/bakgrunn_nett_tv.jpg"; }
        }

        public static string MENU_ITEM_ID_CHOOSE_STREAM_MANUALLY
        {
            get { return "liveall"; }
        }

        public static string MENU_ITEM_ID_LIVE_ALTERNATE
        {
            get { return "liveAlternate"; }
        }

        public static string MENU_ITEM_ID_SEARCH
        {
            get { return "sok"; }
        }

        public static string MENU_ITEM_ID_ALPHABETICAL_LIST
        {
            get { return "all"; }
        }

        public static string MENU_ITEM_ID_CATEGORIES
        {
            get { return "categories"; }
        }

        public static string MENU_ITEM_ID_LIVE
        {
            get { return "live"; }
        }

        public static string MENU_ITEM_ID_RECOMMENDED_PROGRAMS
        {
            get { return "anbefalte"; }
        }

        public static string MENU_ITEM_ID_MOST_WATCHED
        {
            get { return "mestSett"; }
        }

        public static int CONFIG_DEFAULT_SPEED
        {
            get { return 2048; }
        }

        public static string CONFIG_DEFAULT_LIVE_STREAM_QUALITY
        {
            get { return QUALITY_LOW; }
        }

        public static string HOVER_IMAGE
        {
            get { return @"\media\hover_my tv.png"; }
        }

        public static string QUALITY_LOW
        {
            get { return "Low"; }
        }

        public static string QUALITY_MEDIUM
        {
            get { return "Medium"; }
        }

        public static string QUALITY_HIGH
        {
            get { return "High"; }
        }

        public static string QUALITY_LOW_SUFFIX
        {
            get { return "_l"; }
        }

        public static string QUALITY_MEDIUM_SUFFIX
        {
            get { return "_m"; }
        }

        public static string QUALITY_HIGH_SUFFIX
        {
            get { return "_h"; }
        }

        public static string MENU_ITEM_LIVE_ALTERNATE_NRK1
        {
            get { return "NRK 1"; }
        }

        public static string MENU_ITEM_LIVE_ALTERNATE_NRK2
        {
            get { return "NRK 2"; }
        }

        public static string MENU_ITEM_LIVE_ALTERNATE_3
        {
            get { return "NRK Alltid Nyheter"; }
        }

        public static string MENU_ITEM_LIVE_ALTERNATE_4
        {
            get { return "Testkanal (innhold varierer)"; }
        }

        public static int PREDEFINED_LOW_SPEED
        {
            get { return 400; }
        }

        public static int PREDEFINED_MEDIUM_SPEED
        {
            get { return 1000; }
        }

        public static int PREDEFINED_HIGH_SPEED
        {
            get { return 10000; }
        }

        public static string MENU_ITEM_ID_PODCASTS
        {
            get { return "menuItemPodcasts"; }
        }

        public static string MENU_ITEM_ID_PODCASTS_AUDIO
        {
            get { return "menuItemPodcastsAudio"; }
        }

        public static string MENU_ITEM_ID_PODCASTS_VIDEO
        {
            get { return "menuItemPodcastsVideo"; }
        }

        public static string MENU_ITEM_ID_LATEST_CLIPS
        {
            get { return "menuItemLatestClips"; }
        }

        public const string PLUGIN_NAME = "NRK Browser";

        public const string MENU_ITEM_ID_NYHETER = "nyheter";


        public const string MENU_ITEM_ID_SPORT = "sport";


        public const string MENU_ITEM_ID_NATUR = "natur";


        public const string MENU_ITEM_ID_SUPER = "super";
    }
}