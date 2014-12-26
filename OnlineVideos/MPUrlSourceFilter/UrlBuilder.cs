using System;

namespace OnlineVideos.MPUrlSourceFilter
{
    public static class UrlBuilder
    {
        /// <summary>
        /// Gets an Url for the MediaPortal IPTV filter with user settings of the site applied.
        /// </summary>
        /// <param name="siteUtil">The <see cref="Sites.SiteUtilBase"/> instance with url settings.</param>
        /// <param name="url">A string containing the base64 encoded binary serialized data of a supported <see cref="SimpleUrl"/> inheriting class.</param>
        /// <returns></returns>
        public static String GetFilterUrl(Sites.SiteUtilBase siteUtil, String url)
        {
            int index = url.IndexOf(SimpleUrl.ParameterSeparator);
            SimpleUrl simpleUrl = null;
            
            if (index != -1)
            {
                String encodedContent = url.Substring(index + SimpleUrl.ParameterSeparator.Length);
                simpleUrl = SimpleUrl.FromString(encodedContent);
                if (simpleUrl == null)
                    throw new OnlineVideosException(Translation.Instance.UnableToPlayVideo);
            }
            else
            {
                simpleUrl = UrlFactory.CreateUrl(url);
                if (simpleUrl == null)
                    return url;
            }

            //simpleUrl.CacheFolder =; simpleUrl.MaximumLogSize =; simpleUrl.MaximumPlugins =; simpleUrl.Verbosity =;
            simpleUrl.ApplySettings(siteUtil);
            return simpleUrl.ToFilterString();
        }
    }
}