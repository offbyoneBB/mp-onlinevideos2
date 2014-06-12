using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites.WebAutomation.Extensions;

namespace OnlineVideos.Sites.WebAutomation.ConnectorImplementations.SkyGo.Extensions
{
    /// <summary>
    /// Parse the categories from a given html page
    /// </summary>
    public static class SkyGoCategoryParser
    {
        /// <summary>
        /// Load the root categories from the menu on the left
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public static List<Category> LoadRootCategoriesFromDocument(this HtmlDocument document)
        {
            var listItems = document.GetElementsByTagName("a");
            var results = new List<Category>();

            foreach (HtmlNode item in listItems)
            {
                if (item.OuterHtml.Contains("ATI_menuItemLink") && item.OuterHtml.Contains("_LEFTNAV"))
                {
                    if (item.InnerText.ToLower() != "showcase" && item.InnerText.ToLower() != "sky store" && item.InnerText.ToLower() != "international")
                    {
                        var tmpItem = new Category();
                        tmpItem.Other = "R~" + item.InnerText.ToUpper().Replace("SKY ", "");
                        tmpItem.Description = item.InnerText;
                        tmpItem.Name = item.InnerText;
                        tmpItem.HasSubCategories = true;
                        tmpItem.SubCategoriesDiscovered = false;
                        results.Add(tmpItem);
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// Load the series episodes from the page
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public static List<Category> LoadSeriesItemsFromDocument(this HtmlDocument document, Category parentCategory)
        {
            var listItems = document.GetElementsByTagName("a");
            var results = new List<Category>();

            foreach (HtmlNode item in listItems)
            {
                if (item.OuterHtml.Contains("teaserImageLnk"))
                {
                    var tmpItem = new Category();
                    tmpItem.Other = "V~" + item.OuterHtml.ReadIdFromUrl("videoId");
                    tmpItem.Thumb = item.ChildNodes[0].GetAttribute("src").ToString();
                    tmpItem.ParentCategory = parentCategory;
                    tmpItem.Name = item.ChildNodes[0].GetAttribute("title").ToString();
                   
                    tmpItem.HasSubCategories = false;
                    tmpItem.SubCategoriesDiscovered = true;
                    results.Add(tmpItem);
                }
            }
            return results;
        }


        /// <summary>
        /// Load child categories from the list on the A to Z page
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public static void LoadChildCategoriesFromDocument(this HtmlDocument document, Category parentCategory)
        {
            var seriesLinksTmp = document.GetElementsByTagName("tr");
            List<HtmlNode> seriesLinks;
            if (seriesLinksTmp == null)
                seriesLinks = document.GetElementsByTagName("div").Where(x => x.Attributes["class"] != null && x.Attributes["class"].Value.Contains("promoItem ")).ToList();
            else
                seriesLinks = seriesLinksTmp.Where(x => x.Attributes["class"] != null && x.Attributes["class"].Value.Contains("ATI_azContentRow")).ToList();

            var categPos = -1;
            var channelPos = -1;
            var actorPos = -1;
            document.ReadGenreChannelAndActorPositions(ref categPos, ref channelPos, ref actorPos);
            parentCategory.HasSubCategories = true;

            foreach (HtmlNode item in seriesLinks)
            {
                /*if (item.OuterHtml.Contains("ATI_azContentRow"))
                {*/
                    var link = item.ChildNodes[0];

                    if (item.OuterHtml.Contains("ATI_azContentRow"))
                        link = link.ChildNodes[0];
                    
                    var tmpCategs = string.Empty;
                    var tmpCategItem = new Category();
                   
                    tmpCategItem = link.ParseAsCategory(categPos, channelPos, actorPos, ref tmpCategs);
                    tmpCategItem.ParentCategory = parentCategory;   
                      
                    // Add this to the parent category we navigate down (unless there are any other categories we're associated to (we'll associate/parent into each genre))
                    var added = false;
                    if (tmpCategs != null)
                    {
                        foreach (var thisCateg in tmpCategs.Split(','))
                        {
                            var categ = thisCateg.Trim();
                            var tmpCateg = parentCategory.SubCategories.Where(x => x.Name == categ).FirstOrDefault();

                            // Add the category if it doesn't exist
                            if (tmpCateg == null)
                            {
                                tmpCateg = new Category();
                                tmpCateg.Other = categ;
                                //tmpCateg.Description = categ;
                                tmpCateg.Name = categ;
                                tmpCateg.ParentCategory = parentCategory;
                                parentCategory.HasSubCategories = true;
                                parentCategory.SubCategoriesDiscovered = true;
                                parentCategory.SubCategories.Add(tmpCateg);
                            }
                           
                            if (tmpCateg.SubCategories == null) tmpCateg.SubCategories = new List<Category>();
                            tmpCategItem.ParentCategory = tmpCateg;
                           
                            // Don't add a duplicate item
                            if (tmpCateg.SubCategories.Where(x => x.Name == tmpCategItem.Name).Count() == 0)
                            {
                                tmpCateg.SubCategories.Add(tmpCategItem);
                                tmpCateg.HasSubCategories = true;
                                tmpCateg.SubCategoriesDiscovered = true;
                                tmpCateg.SubCategories = tmpCateg.SubCategories.OrderBy(x => x.Name).ToList();
                            }
                            
                            added = true;
                        }
                    }

                    if (!added)
                        parentCategory.SubCategories.Add(tmpCategItem);
               // }
            }
            parentCategory.SubCategories = parentCategory.SubCategories.OrderBy(x => x.Name).ToList();
        }

        /// <summary>
        /// Will read the id from the supplied string, expects format:
        /// /{idToLookFor}/{id}/
        /// </summary>
        /// <param name="url"></param>
        /// <param name="idToLookFor"></param>
        /// <returns></returns>
        public static string ReadIdFromUrl(this string url, string idToLookFor)
        {
            var idPos = url.IndexOf("/" + idToLookFor + "/");

            if (idPos == -1)
                return string.Empty;

            var idStart = url.Substring(idPos + idToLookFor.Length + 2);

            var idEnd = idStart.IndexOf("/");

            if (idEnd == -1)
                return string.Empty;

            return idStart.Substring(0, idEnd);
        }
        
        /// <summary>
        /// Parse the html element as a video category
        /// </summary>
        /// <param name="itemToParse"></param>
        /// <param name="categPos"></param>
        /// <param name="channelPos"></param>
        /// <param name="categories"></param>
        /// <returns></returns>
        private static Category ParseAsCategory(this HtmlNode itemToParse, int categPos, int channelPos, int actorPos, ref string categories)
        {
            var tmpItem = new Category();
            var seriesId = itemToParse.GetAttribute("href").ReadIdFromUrl("seriesId");
            var videoId = itemToParse.GetAttribute("href").ReadIdFromUrl("videoId");
            tmpItem.Other = string.IsNullOrEmpty(seriesId) ? "V~" + videoId : "S~" + seriesId;
            //tmpItem.Description = itemToParse.InnerText;
            tmpItem.Name = string.IsNullOrEmpty(itemToParse.InnerText) ? itemToParse.NextSibling.InnerText : itemToParse.InnerText;
            //tmpItem.Url = itemToParse.GetAttribute("href");
            tmpItem.HasSubCategories = (tmpItem.Type() == SkyGoCategoryData.CategoryType.Series); // Series items will have a sub category per episode

            var parent = itemToParse.ParentNode.ParentNode;

            if (channelPos > -1)
                //tmpItem.Channel = parent.Children[channelPos].InnerText;
                tmpItem.Other += "~" + parent.ChildNodes.Where(x=>x.Name == "td").ToArray()[channelPos].InnerText;
            if (actorPos > -1)
                tmpItem.Name += " (" + parent.ChildNodes.Where(x => x.Name == "td").ToArray()[actorPos].InnerText + ")";

            if (parent.ChildNodes != null && categPos > -1 && parent.ChildNodes.Count >= categPos)
                categories = parent.ChildNodes.Where(x => x.Name == "td").ToArray()[categPos].InnerText;
            return tmpItem;
        }

        /// <summary>
        /// Get the column positions for the genre and channel
        /// </summary>
        /// <param name="document"></param>
        /// <param name="genrePos"></param>
        /// <param name="channelPos"></param>
        private static void ReadGenreChannelAndActorPositions(this HtmlDocument document, ref int genrePos, ref int channelPos, ref int actorPos)
        {
            var tableHeadersTmp = document.GetElementsByTagName("thead");
            var tableHeaders = new List<HtmlNode>();
            // Some pages use div layout instead of tables!
            if (tableHeadersTmp == null)
                tableHeaders = document.GetElementsByTagName("div").Where(x => x.Attributes["class"] != null && x.Attributes["class"].Value.Contains("ATI_listHeadings")).ToList();
            else
                tableHeaders = tableHeadersTmp.ToList();

            foreach (HtmlNode item in tableHeaders)
            {
                if (item.ChildNodes[0] != null)
                {
                    var tr = item.ChildNodes[0];
                    for (var pos = 0; pos < tr.ChildNodes.Count; pos++)
                    {
                        if (DoesNodeMatchHeading(tr.ChildNodes[pos], "Genre") || DoesNodeMatchHeading(tr.ChildNodes[pos], "Category"))
                            genrePos = pos;
                        if (DoesNodeMatchHeading(tr.ChildNodes[pos], "Channel"))
                            channelPos = pos;
                        if (DoesNodeMatchHeading(tr.ChildNodes[pos], "Starring"))
                            actorPos = pos;
                    }
                }
            }
        }

        /// <summary>
        /// There are a couple of places within the object tree where a heading text could match, we'll try and find them here
        /// </summary>
        /// <param name="node"></param>
        /// <param name="headingToMatch"></param>
        /// <returns></returns>
        private static bool DoesNodeMatchHeading(HtmlNode node, string headingToMatch)
        {
            if (node.ChildNodes.Count > 0 && node.ChildNodes[0] != null && node.ChildNodes[0].InnerText == headingToMatch) return true;
            return false;
        }
    }
}
