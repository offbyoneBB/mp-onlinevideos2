using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineVideos.Sites.Amazon.GenreList
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class AddCounter
    {
    }

    public class AddError
    {
    }

    public class AddFault
    {
    }

    public class AddMetric
    {
    }

    public class AddParameter
    {
    }

    public class AddTime
    {
    }

    public class Args
    {
        public bool isLivePageActive { get; set; }
        public string gvlString { get; set; }
        public bool isCookieConsentApplicable { get; set; }
        public SiteWideWeblabs siteWideWeblabs { get; set; }
        public Context context { get; set; }
        public object serviceToken { get; set; }
        public string avlString { get; set; }
        public Render render { get; set; }
        public bool hasAmazonAdvertisingPublisherConsent { get; set; }
        public Metric metric { get; set; }
    }

    public class Availability
    {
        public string description { get; set; }
        public string severity { get; set; }
    }

    public class Close
    {
    }

    public class Container
    {
        public bool compressGrid { get; set; }
        public string containerType { get; set; }
        public List<Entity> entities { get; set; }
        public EntitlementCues entitlementCues { get; set; }
        public int estimatedTotal { get; set; }
        public string impressionData { get; set; }
        public string journeyIngressContext { get; set; }
        public string text { get; set; }
        public string title { get; set; }
    }

    public class Context
    {
        public string customerID { get; set; }
        public string userAgent { get; set; }
        public bool isInternal { get; set; }
        public string path { get; set; }
        public QueryParameters queryParameters { get; set; }
        public string requestID { get; set; }
        public string sessionID { get; set; }
        public string trafficPolicies { get; set; }
        public string domain { get; set; }
        public string marketplaceID { get; set; }
        public string customerIPAddress { get; set; }
        public string originalURI { get; set; }
        public string osLocale { get; set; }
        public string recordTerritory { get; set; }
        public string currentTerritory { get; set; }
        public string geoToken { get; set; }
        public string cookieTimezone { get; set; }
        public object appName { get; set; }
        public object deviceID { get; set; }
        public Contingencies contingencies { get; set; }
        public bool isTest { get; set; }
        public object mocks { get; set; }
        public object serviceOverrides { get; set; }
        public WeblabOverrides weblabOverrides { get; set; }
        public string identityContext { get; set; }
        public string locale { get; set; }
    }

    public class Contingencies
    {
        public bool isTesting { get; set; }
        public Values values { get; set; }
    }

    public class Cover
    {
        public string alternateText { get; set; }
        public string url { get; set; }
    }

    public class EntitlementCues
    {
        public string entitledCarousel { get; set; }
        public string offerType { get; set; }
    }

    public class Entity
    {
        public string alternateText { get; set; }
        public List<string> degradations { get; set; }
        public string displayTitle { get; set; }
        public string entityType { get; set; }
        public List<string> entitlements { get; set; }
        public bool filterEntitled { get; set; }
        public bool hasSubtitles { get; set; }
        public HoverInfo hoverInfo { get; set; }
        public Images images { get; set; }
        public ItemAnalytics itemAnalytics { get; set; }
        public Link link { get; set; }
        public Messages messages { get; set; }
        public string overlayTextPosition { get; set; }
        public List<object> playbackActions { get; set; }
        public Properties properties { get; set; }
        public string refMarker { get; set; }
        public string widgetType { get; set; }
    }

    public class FeatureSwitches
    {
        public bool disableHover { get; set; }
        public bool isWatchlistMigrationOn { get; set; }
        public bool isHoverSsmOn { get; set; }
        public bool disableEnrichItemMetadata { get; set; }
        public bool jicEnrichment { get; set; }
    }

    public class HoverInfo
    {
        public bool canHover { get; set; }
    }

    public class Images
    {
        public Cover cover { get; set; }
    }

    public class IncrementCounter
    {
    }

    public class ItemAnalytics
    {
        public string refMarker { get; set; }
    }

    public class Link
    {
        public string url { get; set; }
    }

    public class Messages
    {
        public List<object> infoboxes { get; set; }
        public List<object> offers { get; set; }
    }

    public class Metadata
    {
        public Availability availability { get; set; }
    }

    public class Metric
    {
        public Close close { get; set; }
        public SetMarketplace setMarketplace { get; set; }
        public SetOperation setOperation { get; set; }
        public SetClientProgram setClientProgram { get; set; }
        public AddCounter addCounter { get; set; }
        public IncrementCounter incrementCounter { get; set; }
        public AddTime addTime { get; set; }
        public AddMetric addMetric { get; set; }
        public AddParameter addParameter { get; set; }
        public AddError addError { get; set; }
        public AddFault addFault { get; set; }
    }

    public class PageMetadata
    {
        public string title { get; set; }
    }

    public class Properties
    {
    }

    public class Props
    {
        public List<Container> containers { get; set; }
        public FeatureSwitches featureSwitches { get; set; }
        public bool hasFailed { get; set; }
        public bool isTrailerAutoplayEnabled { get; set; }
        public Metadata metadata { get; set; }
        public PageMetadata pageMetadata { get; set; }
        public string playbackLaunchType { get; set; }
        public Strings strings { get; set; }
        public SwiftPageParameters swiftPageParameters { get; set; }
        public string title { get; set; }
        public object requestID { get; set; }
    }

    public class QueryParameters
    {
    }

    public class Render
    {
        public bool isRTL { get; set; }
    }

    public class Root
    {
        public Props props { get; set; }
        public Args args { get; set; }
    }

    public class SetClientProgram
    {
    }

    public class SetMarketplace
    {
    }

    public class SetOperation
    {
    }

    public class SiteWideWeblabs
    {
        public string DV_WEB_FABLE_BREAKPOINT_MIGRATION_646916 { get; set; }
        public string DV_WEB_FABLE_ICON_MIGRATION_651317 { get; set; }
        public string DV_WEB_FABLE_BUTTON_MIGRATION_651324 { get; set; }
        public string DV_WEB_KIRUP_LOGGER_METRICAGENT_699661 { get; set; }
    }

    public class Strings
    {
        public string DV_WEB_ARIA_PREVIOUS_TITLE { get; set; }
        public string DV_WEB_WATCHLIST_TOOLTIP { get; set; }
        public string DV_WEB_ARIA_NEXT_N_TITLES { get; set; }
        public string DV_WEB_ARIA_PREVIOUS_N_TITLES { get; set; }
        public string DV_WEB_OVERFLOW_MENU_TOOLTIP { get; set; }
        public string DV_WEB_DETAILS_TOOLTIP { get; set; }
        public string DV_WEB_DISCOVER_NO_MATCH { get; set; }
        public string DV_WEB_DISCOVER_FILTERING_FAILED { get; set; }
        public string DV_WEB_ARIA_NEXT_TITLE { get; set; }
    }

    public class SwiftPageParameters
    {
        public string pageId { get; set; }
        public string pageType { get; set; }
    }

    public class Values
    {
    }

    public class WeblabOverrides
    {
    }

}
