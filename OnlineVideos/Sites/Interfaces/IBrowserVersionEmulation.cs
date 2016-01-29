namespace OnlineVideos.Sites.Interfaces
{
    /// <summary>
    /// Interface for SiteUtils to expose a custom emulation level which will be configured for the used WebBrowser instance.
    /// By implementing this interface, a SiteUtil can force a spefic emulation level, i.e. to force usage of Silverlight,
    /// or modern standard HTML5 players (depending on site).
    /// </summary>
    public interface IBrowserVersionEmulation : IBrowserSiteUtil
    {
        /// <summary>
        /// Gets an emulation version number, <see cref="http://msdn.microsoft.com/en-us/library/ee330730%28v=VS.85%29.aspx"/> for valid numbers.
        /// Few examples:
        /// <c>12000</c> forces "EDGE" rendering mode
        /// <c>11000</c> forces IE11 rendering mode
        /// <c>10000</c> forces IE10 rendering mode
        /// </summary>
        int EmulatedVersion { get; }
    }
}
