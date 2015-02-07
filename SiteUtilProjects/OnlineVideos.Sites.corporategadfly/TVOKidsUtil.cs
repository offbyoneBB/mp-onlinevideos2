using System;
using System.Collections.Generic;
using System.Xml;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Site Utility for tvokids.com
    /// </summary>
    public class TVOKidsUtil : CanadaBrightCoveUtilBase
    {
        // following were found by looking at AMF POST requests using Firebug/Flashbug
        protected override string hashValue { get { return @"466faf0229239e70a6df8fe66fc04f25f50e6fa7"; } }
        protected override string playerId { get { return @"48543011001"; } }
        protected override string publisherId { get { return @"15364602001"; } }
        
        protected override BrightCoveType RequestType { get { return BrightCoveType.FindMediaById; } }
        protected override string brightcoveUrl { get { return string.Format(@"http://c.brightcove.com/services/messagebroker/amf?playerId={0}", playerId); } }

        private static string baseUrlPrefix = @"http://www.tvokids.com";
        private static string mainCategoriesUrl = baseUrlPrefix + "/feeds/all/{0}/shows";
        private static string videoListUrl = baseUrlPrefix + @"/feeds/{0}/all/videos_list.xml?random={1}";        
                
        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            Settings.Categories.Add(
                new RssLink() { Name = "Ages 2 to 5", Url = String.Format(mainCategoriesUrl, "97"), HasSubCategories = true }
               );
            Settings.Categories.Add(
                new RssLink() { Name = "Ages 11 and under", Url = String.Format(mainCategoriesUrl, "98"), HasSubCategories = true }
               );
            
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }
        
        public override int DiscoverSubCategories(Category parentCategory)
        {
            long epoch = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
            parentCategory.SubCategories = new List<Category>();

            string url = ((RssLink) parentCategory).Url;
            XmlDocument xml = GetWebData<XmlDocument>(url);
            
            foreach (XmlNode node in xml.SelectNodes(@"//node"))
            {
                RssLink cat = new RssLink();
                cat.ParentCategory = parentCategory;
                // TODO: remove HTML markup
                cat.Name = node.SelectSingleNode("./node_title").InnerText;
                cat.Url = string.Format(videoListUrl, node.SelectSingleNode("./node_id").InnerText, epoch);
                cat.Description = node.SelectSingleNode("./node_short_description").InnerText;
                cat.HasSubCategories = false;
                Log.Debug("cat: {0}", cat);
                parentCategory.SubCategories.Add(cat);
            }
            
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }
        
        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            XmlDocument xml = GetWebData<XmlDocument>(((RssLink ) category).Url);
            
            foreach (XmlNode node in xml.SelectNodes(@"//node"))
            {
                result.Add(new VideoInfo() {
                               VideoUrl = node.SelectSingleNode("./node_bc_id").InnerText,
                               Thumb = node.SelectSingleNode("./node_thumbnail").InnerText,
                               Title = node.SelectSingleNode("./node_title").InnerText,
                               Description = node.SelectSingleNode("./node_short_description").InnerText
                           });
            }
            
            return result;
        }
    }
}
