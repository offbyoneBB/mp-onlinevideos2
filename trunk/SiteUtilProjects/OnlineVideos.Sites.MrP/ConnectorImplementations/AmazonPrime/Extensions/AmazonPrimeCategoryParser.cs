using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites.WebAutomation.Extensions;
using System.Threading;

namespace OnlineVideos.Sites.WebAutomation.ConnectorImplementations.AmazonPrime.Extensions
{
    public static class AmazonPrimeCategoryParser
    {

        /// <summary>
        /// Get the categories from the supplied Url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static List<Category> LoadAmazonPrimeCategoriesFromUrl(this string url, Category parent)
        {
            HtmlDocument doc;
            var result = new List<Category>();
            List<HtmlNode> nodes = null;
            var variedUserAgent = string.Empty;
            var tmpWeb = new HtmlWeb();

            // Attempt the URL up to 10 times as amazon wants us to use the api!
            for (int i = 0; i <= 10; i++)
            { 
                doc = tmpWeb.Load(url.Replace("{RANDOMNUMBER}", new Random().Next(1000000, 2000000).ToString()));
                nodes = doc.DocumentNode.GetNodesByClass("column");

                if (nodes == null)
                    Thread.Sleep(200);
                else 
                    break;
            }

            if (nodes != null)
            {
                // The <UL> elements with class "column" will have li elements representing the categories 
                foreach (var node in nodes)
                {
                    foreach (HtmlNode childNode in node.ChildNodes)
                    {
                        var tmpCateg = new Category { HasSubCategories = false, SubCategoriesDiscovered = true };
                        if (childNode.NodeType == HtmlNodeType.Element && childNode.Name == "li")
                        {
                            
                            tmpCateg.Other = "V~" + childNode.FindFirstChildElement().Attributes["href"].Value;
                            tmpCateg.Name = childNode.FindFirstChildElement().FindFirstChildElement().InnerText.Replace("&amp;","&");
                            tmpCateg.ParentCategory = parent;
                            tmpCateg.HasSubCategories = true;
                            tmpCateg.SubCategoriesDiscovered = false;
                            result.Add(tmpCateg);
                        }
                    }
                }
            }
            parent.SubCategories = result;
            parent.SubCategoriesDiscovered = true;
            return result;
        }

        /// <summary>
        /// Load the prime videos as categories because we can't get the description until we drill in to the video itself
        /// </summary>
        /// <param name="url"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static List<Category> LoadAmazonPrimeVideosAsCategoriesFromUrl(this string url, Category parent)
        {
            var results = new List<Category>();
            var nextPage = string.Empty;
            HtmlDocument doc = null;
            var tmpWeb = new HtmlWeb();
            List<HtmlNode> listItems = null;
            
            // Attempt the URL up to 10 times as amazon wants us to use the api!
            for (int i = 0; i <= 10; i++)
            {
                doc = tmpWeb.Load(url);
                listItems = doc.DocumentNode.GetNodesByClass("ilo2");

                if (listItems == null)
                    Thread.Sleep(200);
                else
                    break;
            }

            if (listItems != null)
            {
                listItems = listItems.Where(x => x.OriginalName == "li" && x.Id.StartsWith("result_")).ToList();

                // These are the movies - parse them into categories
                foreach (var item in listItems)
                {
                    var tmpCateg = new Category();
                    tmpCateg.ParentCategory = parent;
                    tmpCateg.HasSubCategories = false;
                    tmpCateg.Name = item.FindAllChildElements()[2].InnerText + " (" + item.GetNodeByClass("bdge").GetInnerText() + ")";
                    tmpCateg.Other = item.FindFirstChildElement().FindFirstChildElement().Attributes["href"].Value;
                    tmpCateg.Thumb = item.FindFirstChildElement().FindFirstChildElement().FindFirstChildElement().FindFirstChildElement().Attributes["src"].Value;
                    var released = (item.GetNodeByClass("reg subt") == null ? string.Empty : item.GetNodeByClass("reg subt").FirstChild.GetInnerText());
                    var score = (item.GetNodeByClass("asinReviewsSummaryNoPopover") == null? string.Empty: (item.GetNodeByClass("asinReviewsSummaryNoPopover").FindFirstChildElement() == null ? string.Empty : item.GetNodeByClass("asinReviewsSummaryNoPopover").FindFirstChildElement ().Attributes["alt"].Value));
                    tmpCateg.Description = "Released: " + released + "\r\nReview Score: " + score;
                    results.Add(tmpCateg);
                }

                var nextPageCtrl = doc.GetElementById("pagnNextLink");

                if (nextPageCtrl != null)
                {
                    nextPage = Properties.Resources.AmazonRootUrl + nextPageCtrl.Attributes["href"].Value.Replace("&amp;","&");
                    if (!string.IsNullOrEmpty(nextPage))
                        results.Add(new NextPageCategory() { ParentCategory = parent, Url = nextPage, SubCategories = new List<Category>() });
                }
            }

            return results;
        }

    }
}
