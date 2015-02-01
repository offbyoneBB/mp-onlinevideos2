using System;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;

namespace OnlineVideos.Hoster
{
    public class EcoStream : HosterBase
    {
        public override string getHosterUrl()
        {
            return "ecostream.tv";
        }

        public override string getVideoUrls(string url)
        {
            string webData = SiteUtilBase.GetWebData(url);
            string slotId = Regex.Match(webData, @"var\sadslotid='(?<value>[^']*)'").Groups["value"].Value;
            string footerhash = Regex.Match(webData, @"var\sfooterhash='(?<value>[^']*)'").Groups["value"].Value;
            string dataid = Regex.Match(webData, @"data-id=""(?<value>[^""]*)""").Groups["value"].Value;

            NameValueCollection headers = new NameValueCollection();
            headers.Add("Accept", "*/*"); // accept any content type
            headers.Add("User-Agent", OnlineVideoSettings.Instance.UserAgent);
            headers.Add("X-Requested-With", "XMLHttpRequest");

            string page = SiteUtilBase.GetWebData(@"http://www.ecostream.tv/xhr/video/vidurl",
                String.Format("id={0}&tpm={1}{2}", dataid, footerhash, slotId),
                headers: headers);


            Match m = Regex.Match(page, @"""url"":""(?<url>[^""]*)""");
            if (m.Success)
            {
                string newUrl = m.Groups["url"].Value;

                if (!Uri.IsWellFormedUriString(newUrl, UriKind.Absolute))
                {
                    Uri uri = null;
                    if (Uri.TryCreate(new Uri(url), newUrl, out uri))
                        newUrl = uri.ToString();
                }
                return SiteUtilBase.GetRedirectedUrl(newUrl);
            }
            return String.Empty;
        }
    }
}
