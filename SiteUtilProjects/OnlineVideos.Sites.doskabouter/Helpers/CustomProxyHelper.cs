namespace OnlineVideos.Sites.doskabouter.Helpers
{
    internal class CustomProxyHelper
    {
        public static string GetProxyUrl(string url, string customProxy)
        {
            if (!string.IsNullOrEmpty(customProxy))
            {
                url = customProxy + url.Replace("://", "/");
            }
            return url;
        }
    }
}
