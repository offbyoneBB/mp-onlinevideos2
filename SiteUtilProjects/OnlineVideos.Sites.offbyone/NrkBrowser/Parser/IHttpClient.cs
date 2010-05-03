namespace Vattenmelon.Nrk.Parser.Http
{
    public interface IHttpClient
    {
        /// <summary>
        /// Fetches the url by using the HTTP GET method
        /// </summary>
        /// <param name="url">The url to retrieve</param>
        /// <returns></returns>
        string GetUrl(string url);

        string PostUrl(string url, string postData);
    }
}