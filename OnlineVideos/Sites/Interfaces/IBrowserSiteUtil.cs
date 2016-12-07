namespace OnlineVideos.Sites
{
    /// <summary>
    /// All browser site utils must implement this interface
    /// </summary>
    public interface IBrowserSiteUtil
    {
        string ConnectorEntityTypeName { get; }
        string UserName { get; }
        string Password { get; }
    }
}
