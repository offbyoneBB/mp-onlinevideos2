using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineVideos.Sites.Amazon.Watchlist;

namespace OnlineVideos.Sites.Amazon.EpisodeList
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class AcquisitionActions
    {
    }

    public class Action
    {
        public Atf atf { get; set; }
        public bool ajaxEnabled { get; set; }
        public Endpoint endpoint { get; set; }
        public string formatCode { get; set; }
        public string tag { get; set; }
        public Text text { get; set; }
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

    public class AmazonRating
    {
        public int count { get; set; }
        public string countFormatted { get; set; }
        public double value { get; set; }
    }

    public class EpisodeDetail
    {
        public string title { get; set; }
        public string synopsis { get; set; }
        public AmazonRating amazonRating { get; set; }
        public string asin { get; set; }
        public List<string> audioTracks { get; set; }
        public string compactGti { get; set; }
        public int duration { get; set; }
        public string entityType { get; set; }
        public int episodeNumber { get; set; }
        public bool isAd { get; set; }
        public bool isClosedCaption { get; set; }
        public bool isHdr { get; set; }
        public bool isPrime { get; set; }
        public bool isUhd { get; set; }
        public bool isXRay { get; set; }
        public string parentTitle { get; set; }
        public List<object> playbackTracks { get; set; }
        public RatingBadge ratingBadge { get; set; }
        public string releaseDate { get; set; }
        public string releaseYear { get; set; }
        public string runtime { get; set; }
        public int seasonNumber { get; set; }
        public List<string> subtitles { get; set; }
        public string titleType { get; set; }
        public Images images { get; set; }
        public AcquisitionActions acquisitionActions { get; set; }
        public DownloadActions downloadActions { get; set; }
        public Messages messages { get; set; }
        //public PlaybackActions playbackActions { get; set; }
        public string viewRefMarker { get; set; }
        public WatchPartyAction watchPartyAction { get; set; }
        public List<string> asins { get; set; }
        public string compactGTI { get; set; }
        public string gti { get; set; }
        public bool isLaunched { get; set; }
        public string link { get; set; }
        public int sequenceNumber { get; set; }
        public List<string> contentDescriptors { get; set; }
        public MaturityRating maturityRating { get; set; }
    }

    public class Args
    {
        public string gvlString { get; set; }
        public bool isCookieConsentApplicable { get; set; }
        public SiteWideWeblabs siteWideWeblabs { get; set; }
        public string titleID { get; set; }
        public Context context { get; set; }
        public bool isMarinTrackingActivated { get; set; }
        public string avlString { get; set; }
        public Render render { get; set; }
        public CsGating csGating { get; set; }
        public bool hasAmazonAdvertisingPublisherConsent { get; set; }
        public Metric metric { get; set; }
    }

    public class Atf
    {
    }

    public class Attrs
    {
        public Url url { get; set; }
    }

    public class AutoplayHero
    {
    }


    public class Banner
    {
        public Crow crow { get; set; }
        public object ui { get; set; }
    }

    public class BottomMenu
    {
        public string feedbackSignInUrl { get; set; }
        public HelpText helpText { get; set; }
    }

    public class BundleCarousel
    {
    }

    public class BuyboxTitleId
    {
    }

    public class CardBundle
    {
        public EntitlementCue entitlementCue { get; set; }
        public MaturityRating maturityRating { get; set; }
        public Messages messages { get; set; }
        public Reviews reviews { get; set; }
    }

    public class Carousel
    {
    }

    public class Cast
    {
        public string name { get; set; }
    }

    public class CastAndCrew
    {
        //public B0B8NX6FHZ B0B8NX6FHZ { get; set; }
    }

    public class CastAndCrew2
    {
        public Cast cast { get; set; }
        public List<string> titles { get; set; }
        public Container container { get; set; }
    }

    public class CategorizedGenres
    {
        public string primaryGenre { get; set; }
        public List<string> secondaryGenres { get; set; }
    }

    public class Child
    {
        public string downloadURL { get; set; }
        public string entitlementType { get; set; }
        public string fallbackURL { get; set; }
        public bool isRentalClockStarted { get; set; }
        public string label { get; set; }
        public string refMarker { get; set; }
        public int rentalTermHoursToPlaybackDuration { get; set; }
        public int rentalTermHoursToStart { get; set; }
        public string titleID { get; set; }
        public string benefitId { get; set; }
        public int minutesRemaining { get; set; }
        public string playbackID { get; set; }
        public string playbackStatus { get; set; }
        public string playbackURL { get; set; }
        public string playerRefMarker { get; set; }
        public double progress { get; set; }
        public int resumeTime { get; set; }
        public int runTime { get; set; }
        public string subscriptionName { get; set; }
        public string videoMaterialType { get; set; }
        public string heading { get; set; }
        public string logo { get; set; }
    }

    public class Close
    {
    }

    public class Collections
    {
        //public List<B0B8NX6FHZ> B0B8NX6FHZ { get; set; }
    }

    public class ComingSoon
    {
    }

    public class Container
    {
        public string containerType { get; set; }
        public List<Entity> entities { get; set; }
        public string text { get; set; }
    }

    public class Containers
    {
        //public List<B0B8NX6FHZ> B0B8NX6FHZ { get; set; }
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

    public class Contributors
    {
        public List<Director> directors { get; set; }
        public List<Producer> producers { get; set; }
        public List<StarringActor> starringActors { get; set; }
        public List<object> supportingActors { get; set; }
    }

    public class Cover
    {
        public string url { get; set; }
    }

    public class Creative
    {
    }

    public class Crow
    {
    }

    public class CsGating
    {
        public bool isCSDetailPageEnabled { get; set; }
        public bool isCSLiveDetailPageEnabled { get; set; }
        public bool isCSSearchBrowseEnabled { get; set; }
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

    public class DetailContainer
    {
        public Detail detail { get; set; }
    }

    public class Detail : Dictionary<string, EpisodeDetail>
    {
        //public Detail detail { get; set; }
        public HeaderDetail headerDetail { get; set; }
        //public BtfMoreDetails btfMoreDetails { get; set; }
    }

    public class Director
    {
        public string name { get; set; }
        public string searchLink { get; set; }
    }

    public class DownloadActions
    {
        public Main main { get; set; }
    }

    public class DvMessage
    {
        public Attrs attrs { get; set; }
        public string @string { get; set; }
    }

    public class Endpoint
    {
        public string partialURL { get; set; }
        public Query query { get; set; }
    }

    public class EnhancedSubtitle
    {
        public string text { get; set; }
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
        public List<object> degradations { get; set; }
        public List<object> entitlements { get; set; }
        public bool filterEntitled { get; set; }
        public bool hasSubtitles { get; set; }
        public string itemType { get; set; }
        public List<object> playbackActions { get; set; }
        public Properties properties { get; set; }
        public string displayTitle { get; set; }
        public EntitlementCues entitlementCues { get; set; }
        public HoverInfo hoverInfo { get; set; }
        public Images images { get; set; }
        public Link link { get; set; }
        public MaturityRatingBadge maturityRatingBadge { get; set; }
        public string refMarker { get; set; }
        public string releaseYear { get; set; }
        public string synopsis { get; set; }
        public string title { get; set; }
        public string titleID { get; set; }
        public WatchlistAction watchlistAction { get; set; }
        public string widgetType { get; set; }
        public string runtime { get; set; }
        public CardBundle cardBundle { get; set; }
        public CategorizedGenres categorizedGenres { get; set; }
        public ContentMaturityRating contentMaturityRating { get; set; }
        public CustomerReviews customerReviews { get; set; }
        public string entityType { get; set; }
        public ItemAnalytics itemAnalytics { get; set; }
        public Messages messages { get; set; }
        public OverflowMenu overflowMenu { get; set; }
        public PlaybackAction playbackAction { get; set; }
    }

    public class Features
    {
        public bool isElcano { get; set; }
        public string activateAutoPlayingInHovers { get; set; }
        public string offerClarityEnabled { get; set; }
        public string isJicEnrichmentEnabled { get; set; }
        public string isPassingIsAVODToWebPlayer { get; set; }
        public string isUpdatedPage { get; set; }
        public string isReviewsSubmissionEnabled { get; set; }
        public string disableHover { get; set; }
        public string disableStickyBtfTab { get; set; }
        public string isRecordSeasonEnabled { get; set; }
        public string isNewSeasonSelectorEnabled { get; set; }
        public string hideStarRatings { get; set; }
        public string isBroadcastSubscriptionEnabled { get; set; }
        public string disableEnrichItemMetadata { get; set; }
        public string isDownloadUpsellEnabled { get; set; }
        public string disableMarinTracking { get; set; }
        public string isStreamSelectorModalEnabled { get; set; }
        public string disableExploreTab { get; set; }
    }

    public class FiveStar
    {
        public string hoverText { get; set; }
        public int percentage { get; set; }
        public string percentageDisplay { get; set; }
        public string ratingDisplayLabel { get; set; }
        public string url { get; set; }
    }

    public class FocusMessage
    {
        public DvMessage dvMessage { get; set; }
        public string icon { get; set; }
        public string message { get; set; }
    }

    public class FourStar
    {
        public string hoverText { get; set; }
        public int percentage { get; set; }
        public string percentageDisplay { get; set; }
        public string ratingDisplayLabel { get; set; }
        public string url { get; set; }
    }

    public class Fragments
    {
    }

    public class Genre
    {
        public string id { get; set; }
        public string searchLink { get; set; }
        public string text { get; set; }
    }

    public class GlanceMessage
    {
        public string message { get; set; }
        public string icon { get; set; }
    }

    public class HeaderDetail
    {
    }

    public class HelpText
    {
        public Attrs attrs { get; set; }
        public string @string { get; set; }
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
        public string packshot { get; set; }
        public string covershot { get; set; }
        public string heroshot { get; set; }
        public string providerLogo { get; set; }
        public string titleLogo { get; set; }
        public string titleshot { get; set; }
        public Cover cover { get; set; }
    }

    public class Imdb
    {
    }

    public class ImpressionAnalytics
    {
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
    }

    public class Link
    {
        public string url { get; set; }
    }

    public class Main
    {
        public string __type { get; set; }
        public List<Child> children { get; set; }
        public Metadata metadata { get; set; }
    }

    public class MaturityRating
    {
        public string locale { get; set; }
        public string rating { get; set; }
        public string title { get; set; }
        public string __type { get; set; }
        public string countryCode { get; set; }
        public string description { get; set; }
        public string displayText { get; set; }
        public string id { get; set; }
        public string simplifiedId { get; set; }
    }

    public class MaturityRatingBadge
    {
        public string __type { get; set; }
        public string countryCode { get; set; }
        public string description { get; set; }
        public string displayText { get; set; }
        public string id { get; set; }
        public string simplifiedId { get; set; }
    }

    public class Message
    {
        public Attrs attrs { get; set; }
        public string @string { get; set; }
        public string entitlementType { get; set; }
        public FocusMessage focusMessage { get; set; }
        public List<object> infoboxes { get; set; }
        public List<object> offers { get; set; }
    }

    public class Metadata : Dictionary<string, EpisodeDetail>
    {
        public List<object> messages { get; set; }
        public List<object> secondaryMessages { get; set; }
        public string behaviour { get; set; }
        public string imageLink { get; set; }
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

    public class Notification
    {
    }

    public class OneStar
    {
        public string hoverText { get; set; }
        public int percentage { get; set; }
        public string percentageDisplay { get; set; }
        public string ratingDisplayLabel { get; set; }
        public string url { get; set; }
    }

    public class OtherFormats
    {
    }

    public class OverflowMenu
    {
        public List<Item> items { get; set; }
        public string title { get; set; }
    }

    public class PageContext
    {
        public string app { get; set; }
        public string downloadLaunchType { get; set; }
        public bool enableHover { get; set; }
        public Features features { get; set; }
        public string formFactor { get; set; }
        public bool isCerberusChild { get; set; }
        public string os { get; set; }
        public string pageTitleId { get; set; }
        public string pageType { get; set; }
        public string playbackLaunchType { get; set; }
        public string playbackTrailerLaunchType { get; set; }
        public string purchaseLaunchType { get; set; }
        public bool purchaseRestricted { get; set; }
        public string subPageType { get; set; }
    }

    public class PageLink
    {
        [JsonProperty("amzn1.dv.gti.ae2451ef-6d56-4ae2-a23f-14bc01bd3699")]
        public string amzn1dvgtiae2451ef6d564ae2a23f14bc01bd3699 { get; set; }

        [JsonProperty("amzn1.dv.gti.f5c9f82e-8fd6-4100-990b-1bb32aa79b11")]
        public string amzn1dvgtif5c9f82e8fd64100990b1bb32aa79b11 { get; set; }

        [JsonProperty("amzn1.dv.gti.7e98a482-68d9-4f88-a749-ef361508596f")]
        public string amzn1dvgti7e98a48268d94f88a749ef361508596f { get; set; }

        [JsonProperty("amzn1.dv.gti.cd15e138-c4b4-43a9-8015-b70107e2c856")]
        public string amzn1dvgticd15e138c4b443a98015b70107e2c856 { get; set; }

        [JsonProperty("amzn1.dv.gti.14973716-834b-4265-8abc-6a93c27f7e56")]
        public string amzn1dvgti14973716834b42658abc6a93c27f7e56 { get; set; }

        [JsonProperty("amzn1.dv.gti.28d58f2d-0394-45c7-a2d4-fc20807fed8e")]
        public string amzn1dvgti28d58f2d039445c7a2d4fc20807fed8e { get; set; }

        [JsonProperty("amzn1.dv.gti.2bf8e583-c1e0-4961-a554-0b2d54b8c61c")]
        public string amzn1dvgti2bf8e583c1e04961a5540b2d54b8c61c { get; set; }

        [JsonProperty("amzn1.dv.gti.a0428844-ff84-4bdb-b180-f786d195b3fd")]
        public string amzn1dvgtia0428844ff844bdbb180f786d195b3fd { get; set; }

        [JsonProperty("amzn1.dv.gti.a26a0c3a-6de1-467c-92be-cb7df71ce52d")]
        public string amzn1dvgtia26a0c3a6de1467c92becb7df71ce52d { get; set; }

        [JsonProperty("amzn1.dv.gti.aa81d0c8-9e58-478c-9602-18f0cf05ffce")]
        public string amzn1dvgtiaa81d0c89e58478c960218f0cf05ffce { get; set; }

        [JsonProperty("amzn1.dv.gti.ea97169b-80f4-4200-99f6-ad9d63a91a94")]
        public string amzn1dvgtiea97169b80f4420099f6ad9d63a91a94 { get; set; }

        [JsonProperty("amzn1.dv.gti.1a305633-e93d-4099-b0b8-c2881e7be1ce")]
        public string amzn1dvgti1a305633e93d4099b0b8c2881e7be1ce { get; set; }

        [JsonProperty("amzn1.dv.gti.d8db1e1b-84f2-4eb8-acb9-8be176599c68")]
        public string amzn1dvgtid8db1e1b84f24eb8acb98be176599c68 { get; set; }

        [JsonProperty("amzn1.dv.gti.e545f18b-eed6-47d4-8e25-df2a2ce3808a")]
        public string amzn1dvgtie545f18beed647d48e25df2a2ce3808a { get; set; }

        [JsonProperty("amzn1.dv.gti.b291df15-4760-4525-b505-9d87640e4b9f")]
        public string amzn1dvgtib291df1547604525b5059d87640e4b9f { get; set; }

        [JsonProperty("amzn1.dv.gti.db217857-319b-4d72-b75d-b9f56a4a3617")]
        public string amzn1dvgtidb217857319b4d72b75db9f56a4a3617 { get; set; }

        [JsonProperty("amzn1.dv.gti.22544e36-8f00-4fea-9fb7-c7bfc416bbb6")]
        public string amzn1dvgti22544e368f004fea9fb7c7bfc416bbb6 { get; set; }

        [JsonProperty("amzn1.dv.gti.2aa9f6da-8c6d-4b14-f2c3-c7889cff00a2")]
        public string amzn1dvgti2aa9f6da8c6d4b14f2c3c7889cff00a2 { get; set; }

        [JsonProperty("amzn1.dv.gti.3eb1fea0-36dd-67a4-24d6-b219f5d5dccb")]
        public string amzn1dvgti3eb1fea036dd67a424d6b219f5d5dccb { get; set; }

        [JsonProperty("amzn1.dv.gti.5aafdc6b-3953-3da5-f10d-779d19409a1e")]
        public string amzn1dvgti5aafdc6b39533da5f10d779d19409a1e { get; set; }

        [JsonProperty("amzn1.dv.gti.56a9f6b1-fc82-88bb-fa61-c10c5b3f434b")]
        public string amzn1dvgti56a9f6b1fc8288bbfa61c10c5b3f434b { get; set; }

        [JsonProperty("amzn1.dv.gti.24b21b9c-8a2a-7f90-316a-f166968e54e3")]
        public string amzn1dvgti24b21b9c8a2a7f90316af166968e54e3 { get; set; }

        [JsonProperty("amzn1.dv.gti.58bc4afa-2c72-1275-7ddc-4bc887696f2a")]
        public string amzn1dvgti58bc4afa2c7212757ddc4bc887696f2a { get; set; }

        [JsonProperty("amzn1.dv.gti.8ebb344a-0e99-6683-a9a7-24862e67ec62")]
        public string amzn1dvgti8ebb344a0e996683a9a724862e67ec62 { get; set; }

        [JsonProperty("amzn1.dv.gti.e2aa4ff0-6052-6baa-82b6-e9e57827c7be")]
        public string amzn1dvgtie2aa4ff060526baa82b6e9e57827c7be { get; set; }

        [JsonProperty("amzn1.dv.gti.32b9410e-38fe-51ad-8c34-be40010f33b8")]
        public string amzn1dvgti32b9410e38fe51ad8c34be40010f33b8 { get; set; }

        [JsonProperty("amzn1.dv.gti.12a9f6c2-baf0-fd5c-b264-f1de336f9a56")]
        public string amzn1dvgti12a9f6c2baf0fd5cb264f1de336f9a56 { get; set; }

        [JsonProperty("amzn1.dv.gti.9cb77df7-1358-b44d-2a78-589f7b448015")]
        public string amzn1dvgti9cb77df71358b44d2a78589f7b448015 { get; set; }

        [JsonProperty("amzn1.dv.gti.78a9f6e0-4d33-9671-b74e-534a00ff9691")]
        public string amzn1dvgti78a9f6e04d339671b74e534a00ff9691 { get; set; }

        [JsonProperty("amzn1.dv.gti.50af7660-838b-7932-4a2e-b94bd47c6476")]
        public string amzn1dvgti50af7660838b79324a2eb94bd47c6476 { get; set; }

        [JsonProperty("amzn1.dv.gti.21a2d6b6-bf11-41ff-921a-0957c309d3d1")]
        public string amzn1dvgti21a2d6b6bf1141ff921a0957c309d3d1 { get; set; }

        [JsonProperty("amzn1.dv.gti.b46b517b-074c-4430-813a-e2f272455f24")]
        public string amzn1dvgtib46b517b074c4430813ae2f272455f24 { get; set; }

        [JsonProperty("amzn1.dv.gti.92a9f6c2-6961-887b-7a15-cded47c3429f")]
        public string amzn1dvgti92a9f6c26961887b7a15cded47c3429f { get; set; }

        [JsonProperty("amzn1.dv.gti.eeac4614-64d4-66d3-9567-a56b0aff9433")]
        public string amzn1dvgtieeac461464d466d39567a56b0aff9433 { get; set; }

        [JsonProperty("amzn1.dv.gti.03b8f559-5b80-440c-8333-bd445fd158a5")]
        public string amzn1dvgti03b8f5595b80440c8333bd445fd158a5 { get; set; }

        [JsonProperty("amzn1.dv.gti.e388c7fd-7ca9-484d-91cd-a3a7c309d95a")]
        public string amzn1dvgtie388c7fd7ca9484d91cda3a7c309d95a { get; set; }

        [JsonProperty("amzn1.dv.gti.b0ac26b5-c2dd-56a8-9fc9-1ff991fcb001")]
        public string amzn1dvgtib0ac26b5c2dd56a89fc91ff991fcb001 { get; set; }

        [JsonProperty("amzn1.dv.gti.22a9f6d4-24e5-88b8-ddb5-2fa5bf658f35")]
        public string amzn1dvgti22a9f6d424e588b8ddb52fa5bf658f35 { get; set; }

        [JsonProperty("amzn1.dv.gti.a0a9f6ca-222c-b37d-503a-324c8ccf9416")]
        public string amzn1dvgtia0a9f6ca222cb37d503a324c8ccf9416 { get; set; }

        [JsonProperty("amzn1.dv.gti.c6a9f6e2-cadf-e989-57c0-4b6df6da52b3")]
        public string amzn1dvgtic6a9f6e2cadfe98957c04b6df6da52b3 { get; set; }

        [JsonProperty("amzn1.dv.gti.e4a9f6e3-782d-6d65-b207-cbcca2417256")]
        public string amzn1dvgtie4a9f6e3782d6d65b207cbcca2417256 { get; set; }

        [JsonProperty("amzn1.dv.gti.e0a9f6bc-7501-0fe4-f9e0-9f5569c441ef")]
        public string amzn1dvgtie0a9f6bc75010fe4f9e09f5569c441ef { get; set; }

        [JsonProperty("amzn1.dv.gti.40a9f6bb-a2de-a526-85ff-e5762efb131c")]
        public string amzn1dvgti40a9f6bba2dea52685ffe5762efb131c { get; set; }

        [JsonProperty("amzn1.dv.gti.34a9f6db-bb4a-4434-a4fd-a7fd9bc7c9b0")]
        public string amzn1dvgti34a9f6dbbb4a4434a4fda7fd9bc7c9b0 { get; set; }

        [JsonProperty("amzn1.dv.gti.d6a9f6da-0911-b4f2-f12d-7f82fe48ea3c")]
        public string amzn1dvgtid6a9f6da0911b4f2f12d7f82fe48ea3c { get; set; }

        [JsonProperty("amzn1.dv.gti.b4b46db4-3d20-de39-52e8-aefd8af331d2")]
        public string amzn1dvgtib4b46db43d20de3952e8aefd8af331d2 { get; set; }

        [JsonProperty("amzn1.dv.gti.b8b88a37-c30c-e214-acd8-630e347848a9")]
        public string amzn1dvgtib8b88a37c30ce214acd8630e347848a9 { get; set; }

        [JsonProperty("amzn1.dv.gti.c0a9f6c2-63cf-608e-2fde-94499fa23d82")]
        public string amzn1dvgtic0a9f6c263cf608e2fde94499fa23d82 { get; set; }

        [JsonProperty("amzn1.dv.gti.d6ba1dd7-148e-41af-f885-1ab0eb4fb781")]
        public string amzn1dvgtid6ba1dd7148e41aff8851ab0eb4fb781 { get; set; }

        [JsonProperty("amzn1.dv.gti.06ae5268-4381-4d11-8f54-6fabdb2532bc")]
        public string amzn1dvgti06ae526843814d118f546fabdb2532bc { get; set; }

        [JsonProperty("amzn1.dv.gti.90afbf2c-0f32-4f87-1b29-b63e83f4b48b")]
        public string amzn1dvgti90afbf2c0f324f871b29b63e83f4b48b { get; set; }

        [JsonProperty("amzn1.dv.gti.f8a9f6b3-4835-39f4-41dc-605c3e425292")]
        public string amzn1dvgtif8a9f6b3483539f441dc605c3e425292 { get; set; }

        [JsonProperty("amzn1.dv.gti.42a9f6e3-a658-d61e-b885-81a608f85dec")]
        public string amzn1dvgti42a9f6e3a658d61eb88581a608f85dec { get; set; }

        [JsonProperty("amzn1.dv.gti.8eacfdf1-8c98-dc5b-1ee7-cfaef02c0887")]
        public string amzn1dvgti8eacfdf18c98dc5b1ee7cfaef02c0887 { get; set; }

        [JsonProperty("amzn1.dv.gti.c3e1377d-958f-4b77-8b6a-81def58589be")]
        public string amzn1dvgtic3e1377d958f4b778b6a81def58589be { get; set; }

        [JsonProperty("amzn1.dv.gti.e0a9f6c8-ac16-09f3-152d-fbb6ecaf8ebd")]
        public string amzn1dvgtie0a9f6c8ac1609f3152dfbb6ecaf8ebd { get; set; }

        [JsonProperty("amzn1.dv.gti.b4b8032e-5963-35e0-39fd-03bdaf09d04f")]
        public string amzn1dvgtib4b8032e596335e039fd03bdaf09d04f { get; set; }

        [JsonProperty("amzn1.dv.gti.7ea9f6d8-3d5e-a4b6-32ba-b0a3951abf0b")]
        public string amzn1dvgti7ea9f6d83d5ea4b632bab0a3951abf0b { get; set; }

        [JsonProperty("amzn1.dv.gti.aaa9f6c2-4298-6417-93e3-b3e7621bb4bf")]
        public string amzn1dvgtiaaa9f6c24298641793e3b3e7621bb4bf { get; set; }

        [JsonProperty("amzn1.dv.gti.a2a9f6d8-36f5-aa18-97c5-f2d9dc944488")]
        public string amzn1dvgtia2a9f6d836f5aa1897c5f2d9dc944488 { get; set; }

        [JsonProperty("amzn1.dv.gti.32a9f6b8-ee37-d1c8-babc-774012984e24")]
        public string amzn1dvgti32a9f6b8ee37d1c8babc774012984e24 { get; set; }

        [JsonProperty("amzn1.dv.gti.4eabb955-f549-4c51-3a27-15c36221cbdf")]
        public string amzn1dvgti4eabb955f5494c513a2715c36221cbdf { get; set; }

        [JsonProperty("amzn1.dv.gti.74a9f6b3-b774-85ce-8cad-f6ec4bde998f")]
        public string amzn1dvgti74a9f6b3b77485ce8cadf6ec4bde998f { get; set; }

        [JsonProperty("amzn1.dv.gti.ce77c4e2-6d89-4f7f-8331-6d5f09585cb8")]
        public string amzn1dvgtice77c4e26d894f7f83316d5f09585cb8 { get; set; }

        [JsonProperty("amzn1.dv.gti.08a9f6d1-d923-001c-43db-9bd04b622143")]
        public string amzn1dvgti08a9f6d1d923001c43db9bd04b622143 { get; set; }

        [JsonProperty("amzn1.dv.gti.deaa6e4d-302d-ec1a-dbe8-8820933f464f")]
        public string amzn1dvgtideaa6e4d302dec1adbe88820933f464f { get; set; }

        [JsonProperty("amzn1.dv.gti.c4aacc70-743c-77f5-8b26-8ad15dc6348a")]
        public string amzn1dvgtic4aacc70743c77f58b268ad15dc6348a { get; set; }

        [JsonProperty("amzn1.dv.gti.62a9f6d6-cf2f-1203-4f20-36e4bd0cb6e4")]
        public string amzn1dvgti62a9f6d6cf2f12034f2036e4bd0cb6e4 { get; set; }

        [JsonProperty("amzn1.dv.gti.d0b9be80-ef4d-13df-059d-ad34dbfabd5a")]
        public string amzn1dvgtid0b9be80ef4d13df059dad34dbfabd5a { get; set; }

        [JsonProperty("amzn1.dv.gti.06a9f6c4-6085-19f4-f7ce-180ee52244e8")]
        public string amzn1dvgti06a9f6c4608519f4f7ce180ee52244e8 { get; set; }

        [JsonProperty("amzn1.dv.gti.58a9f6bc-4d82-533c-32cf-40f8c9990dda")]
        public string amzn1dvgti58a9f6bc4d82533c32cf40f8c9990dda { get; set; }

        [JsonProperty("amzn1.dv.gti.48bc6bd0-ac2b-4dce-5ab2-0e605aeacae7")]
        public string amzn1dvgti48bc6bd0ac2b4dce5ab20e605aeacae7 { get; set; }

        [JsonProperty("amzn1.dv.gti.4cbaab3b-0e3c-7a8e-90f9-5f98df939d6a")]
        public string amzn1dvgti4cbaab3b0e3c7a8e90f95f98df939d6a { get; set; }

        [JsonProperty("amzn1.dv.gti.5d52c6d5-d97e-4568-8a6d-a0690354743e")]
        public string amzn1dvgti5d52c6d5d97e45688a6da0690354743e { get; set; }

        [JsonProperty("amzn1.dv.gti.94b84ea6-fdc9-77f5-6fdc-b0d0be115351")]
        public string amzn1dvgti94b84ea6fdc977f56fdcb0d0be115351 { get; set; }

        [JsonProperty("amzn1.dv.gti.a376f563-4f87-40cf-904f-3e1d174f2585")]
        public string amzn1dvgtia376f5634f8740cf904f3e1d174f2585 { get; set; }

        [JsonProperty("amzn1.dv.gti.84a9f6d0-c23f-b070-3515-25c9162c4e63")]
        public string amzn1dvgti84a9f6d0c23fb070351525c9162c4e63 { get; set; }

        [JsonProperty("amzn1.dv.gti.3aa9f6d8-0f0f-e588-d12a-3326d4fbf782")]
        public string amzn1dvgti3aa9f6d80f0fe588d12a3326d4fbf782 { get; set; }

        [JsonProperty("amzn1.dv.gti.faaa98cf-b149-dd75-f1f0-03ad6e1db0a3")]
        public string amzn1dvgtifaaa98cfb149dd75f1f003ad6e1db0a3 { get; set; }

        [JsonProperty("amzn1.dv.gti.6eb528b4-2f87-eb4e-be18-72c1f229a168")]
        public string amzn1dvgti6eb528b42f87eb4ebe1872c1f229a168 { get; set; }

        [JsonProperty("amzn1.dv.gti.4ffac7db-a330-4211-b98d-5aa5aebd8ee5")]
        public string amzn1dvgti4ffac7dba3304211b98d5aa5aebd8ee5 { get; set; }

        [JsonProperty("amzn1.dv.gti.88a9f6cc-1644-4b58-e21c-b28843594e04")]
        public string amzn1dvgti88a9f6cc16444b58e21cb28843594e04 { get; set; }

        [JsonProperty("amzn1.dv.gti.971e43c6-451d-4141-acd8-bdb64a3b08dc")]
        public string amzn1dvgti971e43c6451d4141acd8bdb64a3b08dc { get; set; }

        [JsonProperty("amzn1.dv.gti.88ab5e27-5e19-392c-ef44-75f376d8e171")]
        public string amzn1dvgti88ab5e275e19392cef4475f376d8e171 { get; set; }
    }

    public class PlaybackAction
    {
        public string appFallbackUrl { get; set; }
        public bool disableJs { get; set; }
        public string fallbackUrl { get; set; }
        public string label { get; set; }
        public string refMarker { get; set; }
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
        public string sessionID { get; set; }
        public string titleID { get; set; }
        public string videoMaterialType { get; set; }
        public Main main { get; set; }
    }

    public class PlaybackIntegration
    {
    }

    public class Producer
    {
        public string name { get; set; }
        public string searchLink { get; set; }
    }

    public class Properties
    {
        public bool isIdPlayable { get; set; }
    }

    public class Props
    {
        public string homeRegion { get; set; }
        public State state { get; set; }
        public Strings strings { get; set; }
    }

    public class ProviderLogo
    {
        public string url { get; set; }
    }

    public class Query
    {
        public string titleId { get; set; }
        public string offerType { get; set; }
        public string tag { get; set; }
        public string titleID { get; set; }
        public string token { get; set; }
        public string returnUrl { get; set; }
        public string titleType { get; set; }
    }

    public class QueryParameters
    {
    }

    public class RatingBadge
    {
        public string __type { get; set; }
        public string countryCode { get; set; }
        public string description { get; set; }
        public string displayText { get; set; }
        public string id { get; set; }
        public string simplifiedId { get; set; }
    }

    public class RatingsHistogram
    {
        public FiveStar fiveStar { get; set; }
        public FourStar fourStar { get; set; }
        public OneStar oneStar { get; set; }
        public ThreeStar threeStar { get; set; }
        public TwoStar twoStar { get; set; }
    }

    public class Refund
    {
        public Fragments fragments { get; set; }
        public object refunding { get; set; }
    }

    public class Render
    {
        public bool isRTL { get; set; }
    }

    public class Restriction
    {
    }

    public class ReviewRatingInfo
    {
        public string averageRatingLabel { get; set; }
        public bool hasHalfStar { get; set; }
        public int starCount { get; set; }
        public int totalReviewCount { get; set; }
        public string totalReviewCountText { get; set; }
    }

    public class Reviews
    {
        public int count { get; set; }
        public string countFormatted { get; set; }
        public CustomerReviewsText customerReviewsText { get; set; }
        public string link { get; set; }
        public double value { get; set; }
    }

    public class ReviewsAnalysisModel
    {
        public RatingsHistogram ratingsHistogram { get; set; }
        public ReviewRatingInfo reviewRatingInfo { get; set; }
    }


    public class Root
    {
        public Props props { get; set; }
        public Args args { get; set; }
    }

    public class Seasons
    {
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
    }

    public class StarringActor
    {
        public string name { get; set; }
        public string searchLink { get; set; }
    }

    public class State
    {
        public Features features { get; set; }
        public string pageTitleId { get; set; }
        public DetailContainer detail { get; set; }
        public Action action { get; set; }
        public Refund refund { get; set; }
        public Imdb imdb { get; set; }
        public BundleCarousel bundleCarousel { get; set; }
        public BuyboxTitleId buyboxTitleId { get; set; }
        public Creative creative { get; set; }
        public Banner banner { get; set; }
        public Notification notification { get; set; }
        public Seasons seasons { get; set; }
        //public Self self { get; set; }
        public Trailer trailer { get; set; }
        public Watchlist watchlist { get; set; }
        //public HoverWatchlist hoverWatchlist { get; set; }
        public Restriction restriction { get; set; }
        public Collections collections { get; set; }
        //public Extras extras { get; set; }
        public Carousel carousel { get; set; }
        public PageLink pageLink { get; set; }
        public CastAndCrew castAndCrew { get; set; }
        public Containers containers { get; set; }
        public OtherFormats otherFormats { get; set; }
        public PageContext pageContext { get; set; }
        public EntitlementCue entitlementCue { get; set; }
        public TapsExperimentToken tapsExperimentToken { get; set; }
        public AutoplayHero autoplayHero { get; set; }
        public PlaybackIntegration playbackIntegration { get; set; }
        public ComingSoon comingSoon { get; set; }
        public ImpressionAnalytics impressionAnalytics { get; set; }
        public Metadata metadata { get; set; }
        public Widgets widgets { get; set; }
        public TermsText termsText { get; set; }
        public BottomMenu bottomMenu { get; set; }
        public Reviews reviews { get; set; }
        //public ReviewSubmission reviewSubmission { get; set; }
    }

    public class Strings
    {
        public string DV_DP_more_details { get; set; }
        public string DV_CR_review_submission_failure { get; set; }
        public string DV_CR_submission_max_chars { get; set; }
        public string DV_WEB_ARIA_PREVIOUS_TITLE { get; set; }
        public string DV_WEB_WATCHLIST_TOOLTIP { get; set; }
        public string DV_comma_separator { get; set; }
        public string DV_DP_TAB_related { get; set; }
        public string DV_WEB_SETTINGS_HEAD_YOUR_DEVICES { get; set; }
        public string DV_WEB_PAGINATION_PREVIOUS { get; set; }
        public string DV_DP_ARIA_audio_description { get; set; }
        public string DV_DP_DV_GCPC_window_title { get; set; }
        public string DV_DP_powered_by { get; set; }
        public string DV_CR_vote_unsuccessful { get; set; }
        public string DV_CR_reviews_explanation_header { get; set; }
        public string DV_common_edit { get; set; }
        public string DV_DP_you_multiple_orders_for_this_title { get; set; }
        public string DV_CR_submission_min_words { get; set; }
        public string DV_CR_review_deleting { get; set; }
        public string DV_CR_helpful_button_label { get; set; }
        public string DV_CR_fallback_with_ratings { get; set; }
        public string DV_AB_CANCEL_ACCIDENTAL_PURCHASE { get; set; }
        public string DV_DP_WATCH_PARTY_FTUE_HEADER { get; set; }
        public string DV_TW_title_seasonyear { get; set; }
        public string DV_WEB_FEEDBACK_select_option_dropdown_menu { get; set; }
        public string DV_DP_ARIA_learn_more_amr { get; set; }
        public string DV_CR_write_review_label_other { get; set; }
        public string DV_CR_first_to_review { get; set; }
        public string DV_DP_GC_balance_update_failed { get; set; }
        public string AVOD_DP_GC_promotion_message { get; set; }
        public string DV_DP_Help_Support { get; set; }
        public string DV_CR_sorted_by { get; set; }
        public string DV_DP_WATCH_PARTY_TOOLTIP { get; set; }
        public string DV_CR_delete_review { get; set; }
        public string DV_TW_title_genres { get; set; }
        public string DV_DP_prioritize_chat_header { get; set; }
        public string DV_DP_learn_more { get; set; }
        public string DV_TW_title_starring { get; set; }
        public string DV_TW_title_network { get; set; }
        public string DV_DP_EL_bonus_title_template { get; set; }
        public string DV_CR_submission_add_headline { get; set; }
        public string UK_DV_CR_sorting_header { get; set; }
        public string DV_DP_none_available { get; set; }
        public string DV_WEB_WATCHPARTY_LABEL { get; set; }
        public string DV_DP_CL_other_formats_title { get; set; }
        public string DV_CR_translation_error { get; set; }
        public string DV_DP_ARIA_release_year { get; set; }
        public string DV_CR_verified_purchase { get; set; }
        public string DV_CR_sorting_top_reviews { get; set; }
        public string DV_CR_translate_single_review { get; set; }
        public string AVOD_DP_E_error_ok { get; set; }
        public string DV_TW_title_producers { get; set; }
        public string DV_WEB_FEEDBACK_dropdown_prompt { get; set; }
        public string DV_TW_data_format_streaming { get; set; }
        public string DV_DP_UB_GC_popup_apply { get; set; }
        public string DV_WEB_ARIA_NEXT_N_TITLES { get; set; }
        public string DV_CR_submission_submit { get; set; }
        public string DV_DP_EPISODE_SORT_BY { get; set; }
        public string DV_DP_CAST_CREW_HEADING { get; set; }
        public string DV_common_cancel { get; set; }
        public string DV_CR_translate_all_reviews { get; set; }
        public string DV_CR_fallback_no_ratings { get; set; }
        public string DV_DP_CREATE_WATCH_PARTY { get; set; }
        public string DV_DP_ARIA_watch_title { get; set; }
        public string DV_CR_edit_review { get; set; }
        public string DV_CR_submission_min_chars { get; set; }
        public string DV_CR_read_reviews_label { get; set; }
        public string DV_WEB_MORE_DETAILS { get; set; }
        public string DV_TW_title_format { get; set; }
        public string DV_DP_EL_episode_title_template { get; set; }
        public string DV_WEB_DP_popover_purchase_rights { get; set; }
        public string DV_TW_title_studio { get; set; }
        public string DV_DP_TAB_details { get; set; }
        public string DV_DP_ARIA_amazon_rating { get; set; }
        public string DV_WEB_ARIA_PREVIOUS_N_TITLES { get; set; }
        public string DV_TW_data_purchaserights_streaming { get; set; }
        public string DV_DP_GC_widget_heading { get; set; }
        public string DV_TW_title_languages { get; set; }
        public string AVOD_DP_E_error_text { get; set; }
        public string DV_WEB_OVERFLOW_MENU_TOOLTIP { get; set; }
        public string DV_RBB_CANCEL_PURCH_MODAL_SUBMIT { get; set; }
        public string DV_common_no { get; set; }
        public string DV_DP_EPISODE_AVAILABLE { get; set; }
        public string DV_TW_title_content_descriptors { get; set; }
        public string DV_WEB_FEEDBACK_submit_button { get; set; }
        public string DV_common_delete { get; set; }
        public string DV_CR_submission_body_placeholder { get; set; }
        public string DV_CR_reviews_explanation_text { get; set; }
        public string DV_MWTW_TITLE_MAIN { get; set; }
        public string DV_TW_title_subtitles { get; set; }
        public string DV_DP_ARIA_star_rating { get; set; }
        public string DV_CR_public_name_rating_as { get; set; }
        public string DV_CR_translating_review { get; set; }
        public string DV_CR_reviews_header { get; set; }
        public string DV_TW_title_directors { get; set; }
        public string DV_CR_confirm_label { get; set; }
        public string DV_CR_report_abuse_label { get; set; }
        public string DV_DP_ARIA_next_tab { get; set; }
        public string DV_CR_submission_add_written_review { get; set; }
        public string DV_CR_show_original_review { get; set; }
        public string DV_DP_minutes_remaining { get; set; }
        public string DV_MWTW_TITLE { get; set; }
        public string DV_CR_public_name_change { get; set; }
        public string DV_RBB_CANCEL_PURCH_MODAL_HEADER { get; set; }
        public string DV_TW_Detail_text { get; set; }
        public string AVOD_DP_GC_toc_learn_more { get; set; }
        public string DV_DP_GC_balance_type_heading { get; set; }
        public string DV_CR_submission_overall_rating { get; set; }
        public string DV_DP_EPISODE_NUM_ASCENDING { get; set; }
        public string DV_CR_report_abuse_message { get; set; }
        public string DV_CR_write_review_label { get; set; }
        public string DV_CR_sorting_most_recent { get; set; }
        public string DV_CR_other_countries_header { get; set; }
        public string DV_DP_WATCH_PARTY_FTUE_BODY { get; set; }
        public string DV_WEB_SETTINGS_HEAD_SUBTITLES { get; set; }
        public string DV_WEB_PAGINATION_NEXT { get; set; }
        public string DV_WEB_FEEDBACK_select_related_device { get; set; }
        public string DV_CR_approval_pending { get; set; }
        public string DV_CR_submission_max_words { get; set; }
        public string DE_DV_CR_sorting_header { get; set; }
        public string DV_CR_submission_headline_placeholder { get; set; }
        public string DV_CR_submission_min_words_one { get; set; }
        public string DV_WEB_DETAILS_TOOLTIP { get; set; }
        public string DV_CR_submission_rating_saved { get; set; }
        public string DV_TW_title_purchaserights { get; set; }
        public string DV_CR_translated_review { get; set; }
        public string DV_CR_rating_missing { get; set; }
        public string DV_DP_ARIA_imdb_rating { get; set; }
        public string DV_DP_EPISODE_SORT { get; set; }
        public string DV_CR_delete_review_warning { get; set; }
        public string DV_CR_first_to_rate { get; set; }
        public string US_DV_CR_sorting_header { get; set; }
        public string AVOD_DP_redeem_gift_card_or_promotion { get; set; }
        public string DV_TW_title_actors { get; set; }
        public string DV_DP_UB_GC_success_message { get; set; }
        public string DV_DP_more_items { get; set; }
        public string DV_CR_submission_optional { get; set; }
        public string DV_DP_ARIA_located { get; set; }
        public string DV_DP_ARIA_suitable_for { get; set; }
        public string DV_TW_title_fskrating { get; set; }
        public string DV_TW_title_bbfcrating { get; set; }
        public string DV_DP_live_badge_text { get; set; }
        public string DV_common_yes { get; set; }
        public string DV_TW_title_mpaarating { get; set; }
        public string DV_CR_sending_feedback { get; set; }
        public string DV_WEB_PAGINATION_PAGE { get; set; }
        public string DV_DP_GC_balances_explanation { get; set; }
        public string DV_CR_submission_add_more { get; set; }
        public string DV_CR_write_a_review_label { get; set; }
        public string DV_DP_EPISODE_NUM_DESCENDING { get; set; }
        public string DV_DP_GC_wrong_code { get; set; }
        public string DV_WEB_FEEDBACK_feedback { get; set; }
        public string DV_DP_choose_order_to_cancel { get; set; }
        public string DV_CR_create_review { get; set; }
        public string DV_DP_UB_GC_enter_code { get; set; }
        public string DV_DP_TAB_explore { get; set; }
        public string DV_TW_title_devices { get; set; }
        public string DV_CR_review_deleted { get; set; }
        public string DV_DP_EPISODE_EXPANDER { get; set; }
        public string DV_CR_submission_min_chars_one { get; set; }
        public string DV_DP_TAB_extras { get; set; }
        public string DV_CR_see_all_reviews { get; set; }
        public string DV_DP_first_review { get; set; }
        public string DV_TW_title_eirinrating { get; set; }
        public string DV_DP_GC_code_input_placeholder { get; set; }
        public string DV_DP_GC_balance_amount_heading { get; set; }
        public string DV_WEB_FEEDBACK_no_device_website { get; set; }
        public string DV_DP_EL_episode_title { get; set; }
        public string DV_DP_ARIA_watch_episode { get; set; }
        public string DV_CR_profile_aria_label { get; set; }
        public string DV_TW_amr_nr_text { get; set; }
        public string DV_CR_public_name_failure { get; set; }
        public string DV_WEB_FEEDBACK__send_us_feedback { get; set; }
        public string DV_brand_av { get; set; }
        public string DV_DP_TAB_episodes { get; set; }
        public string DV_DP_ARIA_runtime { get; set; }
        public string DV_WEB_ARIA_NEXT_TITLE { get; set; }
        public string DV_DP_more_info { get; set; }
        public string DV_TW_data_available_to_watch_devices { get; set; }
        public string DV_TW_title_amr { get; set; }
        public string DV_DP_ARIA_scheduled { get; set; }
        public string DV_DP_unavailable_episode_list_message { get; set; }
        public string DV_CR_thank_you_feedback { get; set; }
        public string JP_DV_CR_sorting_header { get; set; }
    }

    public class TapsExperimentToken
    {
    }

    public class TermsText
    {
        public Attrs attrs { get; set; }
        public string @string { get; set; }
    }

    public class Text
    {
        public Attrs attrs { get; set; }
        public string @string { get; set; }
    }

    public class ThreeStar
    {
        public string hoverText { get; set; }
        public int percentage { get; set; }
        public string percentageDisplay { get; set; }
        public string ratingDisplayLabel { get; set; }
        public string url { get; set; }
    }

    public class Tooltip
    {
        public string image { get; set; }
        public string text { get; set; }
    }

    public class Trailer
    {
    }

    public class TwoStar
    {
        public string hoverText { get; set; }
        public int percentage { get; set; }
        public string percentageDisplay { get; set; }
        public string ratingDisplayLabel { get; set; }
        public string url { get; set; }
    }

    public class Url
    {
        public string href { get; set; }
    }

    public class Values
    {
    }

    public class Watchlist
    {
    }

    public class WatchlistAction
    {
        public bool ajaxEnabled { get; set; }
        public Endpoint endpoint { get; set; }
        public string tag { get; set; }
        public Text text { get; set; }
        public string formatCode { get; set; }
    }

    public class WatchPartyAction
    {
        public Endpoint endpoint { get; set; }
        public string label { get; set; }
        public string message { get; set; }
        public string watchPartyOfferType { get; set; }
    }

    public class WeblabOverrides
    {
    }

    public class Widgets
    {
        //public B0B8NX6FHZ B0B8NX6FHZ { get; set; }
    }


}
