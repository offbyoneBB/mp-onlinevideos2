using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OnlineVideos.Sites
{
    public class BrUtil : GenericSiteUtil
    {
        public override int DiscoverDynamicCategories()
        {
            var result = base.DiscoverDynamicCategories();
            foreach (RssLink cat in Settings.Categories)
                cat.Url = "http://www.br.de/mediathek/video/suche/suche-104.html?broadcast=" + HttpUtility.UrlEncode(cat.Name) + "&period=year";
            return result;
        }

        public override Dictionary<string, string> GetPlaybackOptions(string playlistUrl)
        {
            var result = new Dictionary<string, string>();
            var xdoc = GetWebData<System.Xml.Linq.XDocument>(playlistUrl);
            foreach (var asset in xdoc.Descendants("asset"))
            {
                if (!asset.Attribute("type").Value.StartsWith("MOBILE"))
                {
                    var info = string.Format("{0} ({1})", asset.Descendants("dimensions").First().Value, asset.Descendants("readableSize").First().Value);
                    var url = asset.Descendants("downloadUrl").First().Value;
                    result.Add(info, url);
                }
            }
            return result;
        }

    }
}