using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites.WebAutomation.Extensions;
using System.Xml;
using System.Net;
using OnlineVideos.Sites.WebAutomation.Entities;

namespace OnlineVideos.Sites.WebAutomation.ConnectorImplementations._4OD.Extensions
{
    /// <summary>
    /// Helpers for parsing 4OD categories
    /// </summary>
    public static class _4ODCategoryParser
    {
        /// <summary>
        /// Parse a list item node as a category
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static Category LoadGeneralCategoryFromListItem(this HtmlNode node, Category parentCategory)
        {
            var result = new Category();

            result.Name = node.GetNodeByClass("title").InnerText.Replace("&amp;", "&");
            var categoryInfoUrl = Properties.Resources._4OD_RootUrl + node.GetNodeByClass("promo-link").GetAttribute("href");

            var series = node.GetNodeByClass("series-info");
            if (series == null)
                series = node.GetNodeByClass("programme-count");

            result.Description = node.GetNodeByClass("synopsis").InnerText + (series == null ? string.Empty : " (" + series.InnerText + ")");
            result.ParentCategory = parentCategory;

            var image = node.GetNodeByClass("main-image");
            if (image == null)
                image = node.DescendantNodes().Where(x => !string.IsNullOrEmpty(x.GetAttribute("src"))).FirstOrDefault();
            if (image != null)
                result.Thumb = image.GetAttribute("src"); //Properties.Resources._4OD_RootUrl + 
            
            // Collections don't have sub categories
            if (parentCategory.Type() == _4ODCategoryData.CategoryType.GeneralCategory)
            {
                result.HasSubCategories = true;
                var id = node.GetAttribute("data-brandwst");
                result.Other = "P~" + node.GetAttribute("data-brandwst") + "~" + categoryInfoUrl.Replace("collections", "collection") + ".xml";
            }
            else
            {
                result.HasSubCategories = false;
                result.Other = "C~~" + categoryInfoUrl.Replace("collections", "collection") + ".xml";
            }


            return result;
        }

        /// <summary>
        /// Load the information about a program from its xml (basically we will load the series as new categories if there are any)
        /// If there are no series we'll basically just show the parent category again
        /// </summary>
        /// <param name="parentCategory"></param>
        /// <returns></returns>
        public static List<Category> LoadProgrammeInfo(Category parentCategory)
        {
            var doc = new XmlDocument();
            var result = new List<Category>();
            doc.Load(parentCategory.CategoryInformationPage());

            var seriesCount = doc.SelectSingleNode("/brandLongFormInfo/seriesCount");
            if (seriesCount == null) throw new OnlineVideosException("Unable to load 4OD series information from url " + parentCategory.CategoryInformationPage());

            var seriesNodes = doc.SelectNodes("/brandLongFormInfo/allSeries/longFormSeriesInfo/seriesNumber");

            if (seriesNodes == null || seriesNodes.Count == 0)
            {
                var categ = new Category();
                categ.Name = parentCategory.Name;
                categ.Description = parentCategory.Description;
                categ.Thumb = parentCategory.Thumb;
                categ.HasSubCategories = false;
                categ.Other = parentCategory.Other;
                categ.ParentCategory = parentCategory;
                result.Add(categ);
            }
            else
            {
                foreach (XmlNode series in seriesNodes) 
                {
                    var categ = new Category();
                    categ.Name = parentCategory.Name + " Series " + series.InnerText;
                    categ.Description = doc.SelectSingleNodeText("/brandLongFormInfo/synopsis", parentCategory.Description);
                    categ.Thumb = doc.SelectSingleNodeText("/brandLongFormInfo/imagePath", parentCategory.Thumb);//Properties.Resources._4OD_RootUrl + 
                    categ.HasSubCategories = false;
                    categ.Other = parentCategory.Other + "~" + series.InnerText;
                    categ.ParentCategory = parentCategory;
                    result.Add(categ);
                }
            }
            return result;
        }

        /// <summary>
        /// Load the days for catch up
        /// </summary>
        /// <param name="parentCategory"></param>
        /// <returns></returns>
        public static List<Category> LoadCatchUpDays(Category parentCategory)
        {
            var doc = new HtmlDocument();
            var results = new List<Category>();
            var webRequest = (HttpWebRequest)WebRequest.Create(Properties.Resources._4OD_CatchUpUrl);
            var webResponse = (HttpWebResponse)webRequest.GetResponse();

            if (webResponse.StatusCode != HttpStatusCode.OK)
                throw new OnlineVideosException("Unable to retrieve response for 4OD Catch Up Category from " + Properties.Resources._4OD_CatchUpUrl + ", received " + webResponse.StatusCode.ToString());

            doc.Load(webResponse.GetResponseStream());

            // Load all the days from the page

            foreach(HtmlNode node in doc.GetElementsByTagName("a").Where(x=>x.GetAttribute("href").StartsWith("/programmes/4od/catchup/date/")))
            {
                var result = new ExtendedCategory();
                var dateParts = node.GetAttribute("href").Replace("/programmes/4od/catchup/date/", "").Split('/');
                var videoDate = new DateTime(Convert.ToInt32(dateParts[0]), Convert.ToInt32(dateParts[1]), Convert.ToInt32(dateParts[2]));
                result.Name = parentCategory.Name + " - " + videoDate.ToString("dd MMMM yyyy");
                result.SortValue = videoDate.ToString("yyyyMMdd");
                result.Other = "U~" + parentCategory.CategoryId() + "~" + Properties.Resources._4OD_RootUrl + node.GetAttribute("href");
                result.Thumb = parentCategory.Thumb;
                result.HasSubCategories = false;
                result.ParentCategory = parentCategory;
                results.Add(result);
            }
  
            return results;
        }

    }
}
