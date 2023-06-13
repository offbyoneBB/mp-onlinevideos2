using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineVideos.Sites.Amazon.HomeContent
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Action
    {
        public bool ajaxEnabled { get; set; }
        public Endpoint endpoint { get; set; }
        public string formatCode { get; set; }
        public string postActionOverlayMessage { get; set; }
        public string tag { get; set; }
        public Text text { get; set; }
        public string undoMessage { get; set; }
    }

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
        public bool isCrow { get; set; }
        public string gvlString { get; set; }
        public bool isBK4C { get; set; }
        public RequestFeatureSwitches requestFeatureSwitches { get; set; }
        public bool isCookieConsentApplicable { get; set; }
        public bool isDiscoverActive { get; set; }
        public string resiliencyConfiguration { get; set; }
        public bool useNodePlayer { get; set; }
        public bool hasAmazonAdvertisingPublisherConsent { get; set; }
        public bool isLivePageActive { get; set; }
        public string node { get; set; }
        public string pageType { get; set; }
        public bool isElcano { get; set; }
        public SiteWideWeblabs siteWideWeblabs { get; set; }
        public Context context { get; set; }
        public string avlString { get; set; }
        public string subPageType { get; set; }
        public Render render { get; set; }
        public bool enableVerticalPerformantRender { get; set; }
        public Metric metric { get; set; }
    }

    public class Attrs
    {
    }

    public class Availability
    {
        public string description { get; set; }
        public string severity { get; set; }
    }

    public class CardBundle
    {
        public EntitlementCue entitlementCue { get; set; }
        public MaturityRating maturityRating { get; set; }
        public Messages messages { get; set; }
        public Reviews reviews { get; set; }
    }

    public class CategorizedGenres
    {
        public string primaryGenre { get; set; }
        public List<string> secondaryGenres { get; set; }
    }

    public class Close
    {
    }

    public class Container
    {
        public string containerType { get; set; }
        public List<Entity> entities { get; set; }
        public EntitlementCues entitlementCues { get; set; }
        public int estimatedTotal { get; set; }
        public string impressionData { get; set; }
        public string journeyIngressContext { get; set; }
        public string text { get; set; }
        public string paginationServiceToken { get; set; }
        public int? paginationStartIndex { get; set; }
        public string paginationTargetId { get; set; }
        public string title { get; set; }
        public string facetText { get; set; }
        public string seeMoreDescription { get; set; }
        public SeeMoreLink seeMoreLink { get; set; }
        public int? firstEntityGroupSize { get; set; }
        public string unentitledText { get; set; }
        public bool? notExpandable { get; set; }
    }

    public class ContentMaturityRating
    {
        public string locale { get; set; }
        public string rating { get; set; }
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
        public string url { get; set; }
        public string alternateText { get; set; }
    }

    public class CustomerReviews
    {
        public int count { get; set; }
        public string countFormatted { get; set; }
        public CustomerReviewsText customerReviewsText { get; set; }
        public string link { get; set; }
        public double value { get; set; }
    }

    public class CustomerReviewsText
    {
        public Attrs attrs { get; set; }
        public string @string { get; set; }
    }

    public class Edit
    {
        public string label { get; set; }
        public string listType { get; set; }
        public string titleID { get; set; }
        public string token { get; set; }
        public string url { get; set; }
    }

    public class Endpoint
    {
        public string partialURL { get; set; }
        public Query query { get; set; }
    }

    public class EntitlementCue
    {
        public string accessibilityText { get; set; }
        public string colourScheme { get; set; }
        public string cueImage { get; set; }
        public Tooltip tooltip { get; set; }
    }

    public class EntitlementCues
    {
        public string entitlementType { get; set; }
        public FocusMessage focusMessage { get; set; }
        public GlanceMessage glanceMessage { get; set; }
        public HighValueMessage highValueMessage { get; set; }
        public string entitledCarousel { get; set; }
        public string offerType { get; set; }
    }

    public class Entity
    {
        public CategorizedGenres categorizedGenres { get; set; }
        public ContentMaturityRating contentMaturityRating { get; set; }
        public List<object> degradations { get; set; }
        public string displayTitle { get; set; }
        public EntitlementCues entitlementCues { get; set; }
        public List<string> entitlements { get; set; }
        public string entityType { get; set; }
        public bool filterEntitled { get; set; }
        public bool hasSubtitles { get; set; }
        public HideThisAction hideThisAction { get; set; }
        public HoverInfo hoverInfo { get; set; }
        public Images images { get; set; }
        public ItemAnalytics itemAnalytics { get; set; }
        public Link link { get; set; }
        public Messages messages { get; set; }
        public OverflowMenu overflowMenu { get; set; }
        public List<PlaybackAction> playbackActions { get; set; }
        public Properties properties { get; set; }
        public string refMarker { get; set; }
        public string releaseYear { get; set; }
        public string runtime { get; set; }
        public string synopsis { get; set; }
        public string title { get; set; }
        public string titleID { get; set; }
        public WatchlistAction watchlistAction { get; set; }
        public string widgetType { get; set; }
        public string alternateText { get; set; }
        public CardBundle cardBundle { get; set; }
        public CustomerReviews customerReviews { get; set; }
        public Edit edit { get; set; }
        public PlaybackAction playbackAction { get; set; }
        public Progress progress { get; set; }
    }

    public class FeatureSwitches
    {
        public bool epgIngressHasExtraPadding { get; set; }
        public bool requiresMinimumPageWidth { get; set; }
        public bool disableHover { get; set; }
        public bool isWatchlistMigrationOn { get; set; }
        public bool delayItemsRendering { get; set; }
        public bool hasPrimeSash { get; set; }
        public bool ajaxSuperhero { get; set; }
        public bool vwEnabled { get; set; }
        public bool isHoverSsmOn { get; set; }
        public bool isIEorLegacyEdge { get; set; }
        public bool halloweenTheme { get; set; }
        public bool hoverEager { get; set; }
        public bool horizontalPagination { get; set; }
        public bool isCleanSlate { get; set; }
        public bool disableEnrichItemMetadata { get; set; }
        public bool jicEnrichment { get; set; }
        public bool disableStorefrontEIM { get; set; }
        public bool hover2019 { get; set; }
        public bool delayCarouselRendering { get; set; }
    }

    public class FocusMessage
    {
        public string icon { get; set; }
        public string message { get; set; }
    }

    public class GlanceMessage
    {
        public string message { get; set; }
        public string icon { get; set; }
    }

    public class Hero
    {
        public string alternateText { get; set; }
        public string url { get; set; }
    }

    public class HideThisAction
    {
        public Endpoint endpoint { get; set; }
        public bool hasTimer { get; set; }
        public string tag { get; set; }
        public string text { get; set; }
    }

    public class HighValueMessage
    {
        public string icon { get; set; }
        public string message { get; set; }
    }

    public class HoverInfo
    {
        public bool canHover { get; set; }
    }

    public class Images
    {
        public Hero hero { get; set; }
        public TitleLogo titleLogo { get; set; }
        public Cover cover { get; set; }
        public ProviderLogo providerLogo { get; set; }
        public Poster2x3 poster2x3 { get; set; }
    }

    public class IncrementCounter
    {
    }

    public class Item
    {
        public string __type { get; set; }
        public Action action { get; set; }
        public string itemType { get; set; }
        public string text { get; set; }
    }

    public class ItemAnalytics
    {
        public string refMarker { get; set; }
        public string itemProducerID { get; set; }
    }

    public class Link
    {
        public string url { get; set; }
    }

    public class LogoImage
    {
        public bool persistLogoImageOnPage { get; set; }
    }

    public class MaturityRating
    {
        public string locale { get; set; }
        public string rating { get; set; }
        public string title { get; set; }
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

    public class OverflowMenu
    {
        public List<Item> items { get; set; }
        public string title { get; set; }
    }

    public class PageMetadata
    {
        public LogoImage logoImage { get; set; }
    }

    public class Pagination
    {
        public string apiUrl { get; set; }
        public string ariaLabel { get; set; }
        public string contentId { get; set; }
        public string contentType { get; set; }
        public string label { get; set; }
        public int page { get; set; }
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        public QueryParameters queryParameters { get; set; }
        public string requestMethod { get; set; }
        public string serviceToken { get; set; }
        public int startIndex { get; set; }
        public string targetId { get; set; }
        public string token { get; set; }
        public string url { get; set; }
    }

    public class PlaybackAction
    {
        public string appFallbackUrl { get; set; }
        public bool disableJs { get; set; }
        public string fallbackUrl { get; set; }
        public string label { get; set; }
        public string refMarker { get; set; }
        public int resumeTime { get; set; }
        public string sessionID { get; set; }
        public string titleID { get; set; }
        public string videoMaterialType { get; set; }
    }

    public class PlaybackAction2
    {
        public string appFallbackUrl { get; set; }
        public bool disableJs { get; set; }
        public string fallbackUrl { get; set; }
        public string label { get; set; }
        public string refMarker { get; set; }
        public int resumeTime { get; set; }
        public string sessionID { get; set; }
        public string titleID { get; set; }
        public string videoMaterialType { get; set; }
    }

    public class Poster2x3
    {
        public string url { get; set; }
    }

    public class Progress
    {
        public bool isLive { get; set; }
        public double percentage { get; set; }
    }

    public class Properties
    {
        public bool? isIdPlayable { get; set; }
        public bool? leavingSoonOnWatchNext { get; set; }
    }

    public class Props
    {
        public List<object> collections { get; set; }
        public List<Container> containers { get; set; }
        public FeatureSwitches featureSwitches { get; set; }
        public string formFactor { get; set; }
        public bool hasFailed { get; set; }
        public string hoverClass { get; set; }
        public bool isTrailerAutoplayEnabled { get; set; }
        public Metadata metadata { get; set; }
        public PageMetadata pageMetadata { get; set; }
        public string pageType { get; set; }
        public Pagination pagination { get; set; }
        public string playbackLaunchType { get; set; }
        public Strings strings { get; set; }
        public SwiftPageParameters swiftPageParameters { get; set; }
        public bool useFallbacksIfAvailable { get; set; }
        public object requestID { get; set; }
        public string hzPageType { get; set; }
        public string hzSubPageType { get; set; }
        public string nodeID { get; set; }
        public bool isDiscoverActive { get; set; }
        public string homeRegion { get; set; }
    }

    public class ProviderLogo
    {
        public string url { get; set; }
    }

    public class Query
    {
        public string tag { get; set; }
        public string titleId { get; set; }
        public string token { get; set; }
        public string titleType { get; set; }
        public string returnUrl { get; set; }
        public string titleID { get; set; }
    }

    public class QueryParameters
    {
        public string actionScheme { get; set; }
        public string contentId { get; set; }
        public string contentType { get; set; }
        public string decorationScheme { get; set; }
        public List<string> dynamicFeatures { get; set; }
        public string featureScheme { get; set; }
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        public string serviceToken { get; set; }
        public string startIndex { get; set; }
        public string targetId { get; set; }
        public string widgetScheme { get; set; }
    }

    public class Render
    {
        public bool isRTL { get; set; }
    }

    public class RequestFeatureSwitches
    {
        public bool HorizontalPagination { get; set; }
        public bool Tier1ChannelsWidgetSchemeUpdate { get; set; }
        public bool CSLivePage { get; set; }
        public bool DV_WEB_CLEAN_SLATE_GATE_376011 { get; set; }
        public bool DV_WEB_XPL_MATURITY_RATING_HOVER_476015 { get; set; }
    }

    public class Reviews
    {
        public int count { get; set; }
        public string countFormatted { get; set; }
        public CustomerReviewsText customerReviewsText { get; set; }
        public string link { get; set; }
        public double value { get; set; }
    }

    public class Root
    {
        public Props props { get; set; }
        public Args args { get; set; }
    }

    public class SeeMoreLink
    {
        public string label { get; set; }
        public string url { get; set; }
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
        public string DV_WEB_MORE_DETAILS { get; set; }
        public string DV_WEB_ARIA_PREVIOUS_N_TITLES { get; set; }
        public string DV_WEB_OVERFLOW_MENU_TOOLTIP { get; set; }
        public string DV_WEB_DETAILS_TOOLTIP { get; set; }
        public string DV_WEB_ARIA_NEXT_TITLE { get; set; }
    }

    public class SwiftPageParameters
    {
        public string pageId { get; set; }
        public string pageType { get; set; }
    }

    public class Text
    {
        public Attrs attrs { get; set; }
        public string @string { get; set; }
    }

    public class TitleLogo
    {
        public string url { get; set; }
    }

    public class Tooltip
    {
        public string image { get; set; }
        public string text { get; set; }
    }

    public class Values
    {
    }

    public class WatchlistAction
    {
        public bool ajaxEnabled { get; set; }
        public Endpoint endpoint { get; set; }
        public string formatCode { get; set; }
        public string postActionOverlayMessage { get; set; }
        public string tag { get; set; }
        public Text text { get; set; }
        public string undoMessage { get; set; }
    }

    public class WeblabOverrides
    {
    }

}
