using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;

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

        public static string EncodeNonAsciiCharacters(string value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in value)
            {
                if (c > 127)
                {
                    // This character is too big for ASCII
                    string encodedValue = "\\u" + ((int)c).ToString("x4");
                    sb.Append(encodedValue);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static string DecodeEncodedNonAsciiCharacters(string value)
        {
            return Regex.Replace(
                value,
                @"\\u(?<Value>[a-zA-Z0-9]{4})",
                m =>
                {
                    return ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString();
                });
        }
    }
}
