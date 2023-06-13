using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineVideos.Sites.Amazon
{
    // Created by online helper: https://json2csharp.com/
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

    public class Attrs
    {
    }

    public class BaseOutput
    {
        public List<Container> containers { get; set; }
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
    }

    public class Container
    {
        public bool compressGrid { get; set; }
        public string containerType { get; set; }
        public List<Entity> entities { get; set; }
    }

    public class Content
    {
        public string __type { get; set; }
        public BaseOutput baseOutput { get; set; }
    }

    public class ContentMaturityRating
    {
        public string locale { get; set; }
        public string rating { get; set; }
        public string title { get; set; }
    }

    public class Cover
    {
        public string url { get; set; }
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
    }

    public class Entity
    {
        public CardBundle cardBundle { get; set; }
        public CategorizedGenres categorizedGenres { get; set; }
        public ContentMaturityRating contentMaturityRating { get; set; }
        public CustomerReviews customerReviews { get; set; }
        public List<object> degradations { get; set; }
        public string displayTitle { get; set; }
        public EntitlementCues entitlementCues { get; set; }
        public List<string> entitlements { get; set; }
        public string entityType { get; set; }
        public bool filterEntitled { get; set; }
        public bool hasSubtitles { get; set; }
        public HoverInfo hoverInfo { get; set; }
        public Images images { get; set; }
        public ItemAnalytics itemAnalytics { get; set; }
        public Link link { get; set; }
        public Messages messages { get; set; }
        public OverflowMenu overflowMenu { get; set; }
        public PlaybackAction playbackAction { get; set; }
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
    }

    public class FocusMessage
    {
        public string icon { get; set; }
        public string message { get; set; }
    }

    public class GlanceMessage
    {
        public string message { get; set; }
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
        public Cover cover { get; set; }
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

    public class OverflowMenu
    {
        public List<Item> items { get; set; }
        public string title { get; set; }
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
    }

    public class Properties
    {
        public bool isIdPlayable { get; set; }
    }

    public class Props
    {
        public Content content { get; set; }
    }

    public class Query
    {
        public string returnUrl { get; set; }
        public string tag { get; set; }
        public string titleType { get; set; }
        public string titleID { get; set; }
        public string token { get; set; }
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
    }

    public class Text
    {
        public Attrs attrs { get; set; }
        public string @string { get; set; }
    }

    public class Tooltip
    {
        public string image { get; set; }
        public string text { get; set; }
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

}
