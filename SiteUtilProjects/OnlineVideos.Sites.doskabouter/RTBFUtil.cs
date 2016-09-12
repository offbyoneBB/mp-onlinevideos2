using System.Collections.Generic;
using System.Linq;
using System.Web;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
namespace OnlineVideos.Sites
{
    public class RTBFUtil : GenericSiteUtil
    {
        public override int DiscoverDynamicCategories()
        {
            int res = base.DiscoverDynamicCategories();
            foreach (Category cat in Settings.Categories)
                cat.HasSubCategories = true;
            return res;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string webdata = GetWebData(((RssLink)parentCategory).Url);
            var doc = new HtmlDocument();
            doc.LoadHtml(webdata);

            parentCategory.SubCategories = new List<Category>();
            parentCategory.SubCategoriesDiscovered = true;
            var subcatnodes = doc.DocumentNode.SelectNodes(@"//section[@class='rtbf-media-grid rtbf-az__items']");
            foreach (var subcatnode in subcatnodes)
            {
                RssLink cat = new RssLink();
                cat.Name = subcatnode.SelectSingleNode(@"./header/h3").InnerText;
                cat.ParentCategory = parentCategory;
                cat.HasSubCategories = true;
                parentCategory.SubCategories.Add(cat);

                cat.SubCategories = new List<Category>();
                cat.SubCategoriesDiscovered = true;

                var subsubnodes = subcatnode.SelectNodes(@".//article");
                foreach (var subsubnode in subsubnodes)
                {
                    RssLink subsub = new RssLink();
                    subsub.Name = subsubnode.SelectSingleNode(@"./header/a/h4").InnerText;
                    subsub.Description = subsubnode.SelectSingleNode(@"./header/span").InnerText;
                    subsub.Url = subsubnode.SelectSingleNode(@"./a").Attributes["href"].Value;
                    string img = subsubnode.SelectSingleNode(@".//img").Attributes["data-srcset"].Value;
                    var arr1 = img.Split(',');
                    var arr2 = arr1.Last<string>().Split(' ');
                    subsub.Thumb = arr2[0];

                    cat.SubCategories.Add(subsub);
                    subsub.ParentCategory = cat;
                }
            }
            return base.DiscoverSubCategories(parentCategory);
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            string webdata = GetWebData(((RssLink)category).Url);
            var doc = new HtmlDocument();
            doc.LoadHtml(webdata);

            List<VideoInfo> res = new List<VideoInfo>();
            var vidnodes = doc.DocumentNode.SelectNodes(@".//section[@class='js-item-container']//article");
            foreach (var vidnode in vidnodes)
                if (vidnode.Attributes["data-id"] != null)
                {
                    VideoInfo video = new VideoInfo();
                    video.Title = vidnode.SelectSingleNode(@"./header/h3/a").InnerText;
                    var tmNode = vidnode.SelectSingleNode(@".//time");
                    if (tmNode != null)
                        video.Airdate = tmNode.InnerText;
                    video.VideoUrl = @"http://www.rtbf.be/auvio/embed/media?id=" + vidnode.Attributes["data-id"].Value;
                    string img = vidnode.SelectSingleNode(@".//img").Attributes["data-srcset"].Value;
                    var arr1 = img.Split(',');
                    var arr2 = arr1.Last<string>().Split(' ');
                    video.Thumb = arr2[0];

                    res.Add(video);
                }
            return res;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string webdata = GetWebData(video.VideoUrl);
            var doc = new HtmlDocument();
            doc.LoadHtml(webdata);
            var node = doc.DocumentNode.SelectSingleNode(@"//div[@data-media]");
            string js = HttpUtility.HtmlDecode(node.Attributes["data-media"].Value);
            JToken data = JObject.Parse(js) as JToken;
            JToken sources = data["sources"];
            video.PlaybackOptions = new Dictionary<string, string>();
            AddOption("high", sources, video.PlaybackOptions);
            AddOption("web", sources, video.PlaybackOptions);
            AddOption("mobile", sources, video.PlaybackOptions);

            if (video.PlaybackOptions.Count == 0) return null;
            else
                if (video.PlaybackOptions.Count == 1)
                {
                    string resultUrl = video.PlaybackOptions.First().Value;
                    video.PlaybackOptions = null;// only one url found, PlaybackOptions not needed
                    return resultUrl;
                }
                else
                {
                    return video.PlaybackOptions.First().Value;
                }
        }

        private void AddOption(string option, JToken node, Dictionary<string, string> options)
        {
            var o = node[option];
            if (o != null)
                options.Add(option, o.Value<string>());
        }
    }
}
