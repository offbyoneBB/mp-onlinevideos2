using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.georgius
{
    public static class Utils
    {
        public static String FormatAbsoluteUrl(String relativeUrl, String baseUrl)
        {
            if (relativeUrl.Contains("http"))
            {
                return relativeUrl;
            }

            int askIndex = baseUrl.IndexOf('?');
            if (askIndex >= 0)
            {
                baseUrl = baseUrl.Substring(0, askIndex);
            }

            if (relativeUrl.StartsWith("/"))
            {
                Uri baseUri = new Uri(baseUrl);
                return String.Format("{0}{1}", baseUri.GetLeftPart(UriPartial.Authority), relativeUrl);
            }
            else
            {
                if (!baseUrl.EndsWith("/"))
                {
                    baseUrl = String.Format("{0}/", baseUrl);
                }

                Uri baseUri = new Uri(baseUrl);
                return String.Format("{0}{1}", baseUri.GetLeftPart(UriPartial.Path), relativeUrl);
            }            
        }
    }
}
