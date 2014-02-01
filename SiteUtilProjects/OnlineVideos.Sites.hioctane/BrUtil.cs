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
					var downloadUrlNode = asset.Descendants("downloadUrl").FirstOrDefault();
					if (downloadUrlNode != null)
					{
						var url = downloadUrlNode.Value;
						if (!string.IsNullOrEmpty(url))
						{
							var dimNode = asset.Descendants("dimensions").FirstOrDefault();
							var sizeNode = asset.Descendants("readableSize").FirstOrDefault();
							var info = dimNode != null && sizeNode != null ?
								string.Format("{0} ({1})", dimNode.Value, sizeNode.Value) : asset.Attribute("type").Value;
							result[info] = url;
						}
					}
                }
            }
            return result;
        }

    }
}