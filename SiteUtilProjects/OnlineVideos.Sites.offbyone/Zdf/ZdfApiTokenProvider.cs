using System;
using System.Linq;

using HtmlAgilityPack;

using OnlineVideos.Sites.Ard;

namespace OnlineVideos.Sites.Zdf
{
    internal class ZdfApiTokenProvider
    {
        private static readonly string API_TOKEN_URL = "https://www.zdf.de";
        private static readonly string JSON_API_TOKEN = "apiToken";
        private Lazy<BearerToken> _token;

        public ZdfApiTokenProvider(WebCache webClient)
        {
            _token = new Lazy<BearerToken>(() => Initialize(webClient));
        }

        public string SearchBearer => _token.Value.SearchBearer;

        public string VideoBearer => _token.Value.VideoBearer;

        private static BearerToken Initialize(WebCache webClient)
        {
            var document = webClient.GetWebData<HtmlDocument>(API_TOKEN_URL);

            var searchBearer = ParseBearerIndexPage(document.DocumentNode.Descendants("head").Single(), "script", "'");
            var videoBearer = ParseBearerIndexPage(document.DocumentNode.Descendants("body").Single(), "script", "\"");

            return new BearerToken(searchBearer, videoBearer);
        }

        private static string ParseBearerIndexPage(HtmlNode aDocumentNode, string aQuery, string aStringQuote)
        {

            var scriptElements = aDocumentNode.Descendants(aQuery);
            foreach (var scriptElement in scriptElements)
            {
                var script = scriptElement.InnerHtml;

                var value = ParseBearer(script, aStringQuote);
                if (!value.IsNullOrEmpty())
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static string ParseBearer(string aJson, string aStringQuote)
        {
            var bearer = "";

            var indexToken = aJson.IndexOf(JSON_API_TOKEN);

            if (indexToken <= 0)
            {
                return bearer;
            }
            var indexStart = aJson.IndexOf(aStringQuote, indexToken + JSON_API_TOKEN.Length + 1) + 1;
            var indexEnd = aJson.IndexOf(aStringQuote, indexStart);

            if (indexStart > 0)
            {
                bearer = aJson.Substring(indexStart, indexEnd - indexStart);
            }

            return bearer;
        }

        private class BearerToken
        {
            public BearerToken(string searchBearer, string videoBearer)
            {
                SearchBearer = searchBearer;
                VideoBearer = videoBearer;
            }

            public string SearchBearer { get; }
            public string VideoBearer { get; }
        }
    }
}

