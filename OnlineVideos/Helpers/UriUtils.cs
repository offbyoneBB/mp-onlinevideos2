using System;
using System.Collections.Generic;

namespace OnlineVideos.Helpers
{
    public static class UriUtils
    {
        public static bool IsValidUri(string url)
        {
            Uri temp = null;
            return Uri.TryCreate(url, UriKind.Absolute, out temp);
        }

        /// <summary>
        /// Remove all items from a List that are not a valid Url
        /// </summary>
        /// <param name="urls"></param>
        public static void RemoveInvalidUrls(List<string> urls)
        {
            if (urls != null)
            {
                int i = 0;
                while (i < urls.Count)
                {
                    if (string.IsNullOrEmpty(urls[i]) ||
                        !IsValidUri((urls[i].IndexOf(MPUrlSourceFilter.SimpleUrl.ParameterSeparator) > 0) ? urls[i].Substring(0, urls[i].IndexOf(MPUrlSourceFilter.SimpleUrl.ParameterSeparator)) : urls[i]))
                    {
                        Log.Debug("Removed invalid url: '{0}'", urls[i]);
                        urls.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }
    }
}
