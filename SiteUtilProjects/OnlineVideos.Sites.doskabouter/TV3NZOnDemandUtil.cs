using System.Collections.Generic;
using System.Linq;
using System.Xml;
using OnlineVideos.MPUrlSourceFilter;

namespace OnlineVideos.Sites
{
    public class TV3NZOnDemandUtil : GenericSiteUtil
    {
        public override int DiscoverDynamicCategories()
        {
            int res = base.DiscoverDynamicCategories();
            foreach (Category cat in Settings.Categories)
            {
                if (cat.Name != "A-Z")
                    cat.HasSubCategories = false;
            }

            return res;
        }

        public override string getUrl(VideoInfo video)
        {
            string res = base.getUrl(video);
            XmlDocument doc = new XmlDocument();
            string[] urlParts = video.VideoUrl.Split('.');

            if (urlParts[1] == "four")
                res = res.Replace("@@", "c4");
            else
                res = res.Replace("@@", "tv3");

            doc.Load(res);

            video.PlaybackOptions = new Dictionary<string, string>();

            string meta_base = doc.SelectSingleNode("//smil/head/meta").Attributes["base"].Value;

            foreach (XmlNode node in doc.SelectNodes("//smil/body/switch/video"))
            {
                int bitRate = int.Parse(node.Attributes["system-bitrate"].Value) / 1024;

                RtmpUrl rtmpUrl = new RtmpUrl(meta_base + "/" + node.Attributes["src"].Value)
                {
                    SwfVerify = true,
                    SwfUrl = @"http://static.mediaworks.co.nz/video/jw/6.50/jwplayer.flash.swf"
                };

                video.PlaybackOptions.Add(bitRate.ToString() + "K", rtmpUrl.ToString());
            }

            return video.PlaybackOptions.Last().Value;
        }
    }
}
