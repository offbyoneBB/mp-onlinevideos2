using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites.WebAutomation.Interfaces;
using OnlineVideos.Sites.WebAutomation.ConnectorImplementations.SkyGo.Extensions;
using System.Xml;
using HtmlAgilityPack;
using OnlineVideos.Sites.WebAutomation.Extensions;

namespace OnlineVideos.Sites.WebAutomation.ConnectorImplementations.SkyGo.Connectors
{
    public class SkyGoInformationConnector: IInformationConnector
    {
        private enum State
        { 
            None,
            LoadRootCategory,
            LoadChildCategory,
            LoadSeries,
            LoadVideos
        }

        private List<string> _lastLoadedPages = new List<string>();
        SiteUtilBase _siteUtil;

        public SkyGoInformationConnector(SiteUtilBase siteUtil)
        {
            _siteUtil = siteUtil;
        }

        /// <summary>
        /// Request that the categories get loaded - we do this and then populate the LoadedCategories property
        /// </summary>
        /// <returns></returns>
        public List<Category> LoadCategories(Category parentCategory = null)
        {
            var result = new List<Category>();
            if (parentCategory == null)
            {
                // The site has changed slightly, so we'll hard-code the parent categories for now
                var catchUpCategory = new Category { Name = "Catch up", SubCategoriesDiscovered = true, HasSubCategories = true };

                result.Add(catchUpCategory);
                result.Add(new Category { Name = "Sky Movies", SubCategoriesDiscovered = false, HasSubCategories = true, Other = "R~Sky_Movies" });
                result.Add(new Category { Name = "TV Box Sets", SubCategoriesDiscovered = false, HasSubCategories = true, Other = "R~TV_Box_Sets" });
                catchUpCategory.SubCategories = new List<Category>();
                catchUpCategory.SubCategories.Add(new Category { Name = "Alibi", SubCategoriesDiscovered = false, HasSubCategories = true, Other = "R~Catch_Up/ALIBI", ParentCategory = catchUpCategory });
                catchUpCategory.SubCategories.Add(new Category { Name = "CI", SubCategoriesDiscovered = false, HasSubCategories = true, Other = "R~Catch_Up/CI", ParentCategory = catchUpCategory });
                catchUpCategory.SubCategories.Add(new Category { Name = "Comedy Central", SubCategoriesDiscovered = false, HasSubCategories = true, Other = "R~Catch_Up/Comedy_Central", ParentCategory = catchUpCategory });
                catchUpCategory.SubCategories.Add(new Category { Name = "Dave", SubCategoriesDiscovered = false, HasSubCategories = true, Other = "R~Catch_Up/Dave", ParentCategory = catchUpCategory });
                catchUpCategory.SubCategories.Add(new Category { Name = "Discovery", SubCategoriesDiscovered = false, HasSubCategories = true, Other = "R~Catch_Up/Discovery", ParentCategory = catchUpCategory });
                catchUpCategory.SubCategories.Add(new Category { Name = "Disney", SubCategoriesDiscovered = false, HasSubCategories = true, Other = "R~Catch_Up/Disney", ParentCategory = catchUpCategory });
                catchUpCategory.SubCategories.Add(new Category { Name = "Fox", SubCategoriesDiscovered = false, HasSubCategories = true, Other = "R~Catch_Up/FOX", ParentCategory = catchUpCategory });
                catchUpCategory.SubCategories.Add(new Category { Name = "Gold", SubCategoriesDiscovered = false, HasSubCategories = true, Other = "R~Catch_Up/Gold", ParentCategory = catchUpCategory });
                catchUpCategory.SubCategories.Add(new Category { Name = "History", SubCategoriesDiscovered = false, HasSubCategories = true, Other = "R~Catch_Up/History", ParentCategory = catchUpCategory });
                catchUpCategory.SubCategories.Add(new Category { Name = "MTV", SubCategoriesDiscovered = false, HasSubCategories = true, Other = "R~Catch_Up/MTV", ParentCategory = catchUpCategory });
                catchUpCategory.SubCategories.Add(new Category { Name = "Nat Geo", SubCategoriesDiscovered = false, HasSubCategories = true, Other = "R~Catch_Up/Nat_Geo", ParentCategory = catchUpCategory });
                catchUpCategory.SubCategories.Add(new Category { Name = "Sky Sports", SubCategoriesDiscovered = false, HasSubCategories = true, Other = "R~Catch_Up/Sky_Sports", ParentCategory = catchUpCategory });
                catchUpCategory.SubCategories.Add(new Category { Name = "Sky TV", SubCategoriesDiscovered = false, HasSubCategories = true, Other = "R~Catch_Up/Sky_TV", ParentCategory = catchUpCategory });
                catchUpCategory.SubCategories.Add(new Category { Name = "TLC", SubCategoriesDiscovered = false, HasSubCategories = true, Other = "R~Catch_Up/TLC", ParentCategory = catchUpCategory });
                catchUpCategory.SubCategories.Add(new Category { Name = "Watch", SubCategoriesDiscovered = false, HasSubCategories = true, Other = "R~Catch_Up/Watch", ParentCategory = catchUpCategory });
            }
            else
            {
                if (parentCategory.Type() == SkyGoCategoryData.CategoryType.Series)
                    LoadSeriesInformation(parentCategory);
                else
                    LoadSubCategories(parentCategory);
            }

            return result;
        }

        /// <summary>
        /// Use the api version of the Sky Go pages to load categories
        /// </summary>
        /// <param name="parentCategory"></param>
        private void LoadSubCategories(Category parentCategory)
        {
            var tmpchar = "%23";
            var currentAToZPos = 0;            
            var currentPagePos = 0;
            
            _lastLoadedPages = new List<string>();

            // Loop through the whole alphabet
            while ((currentAToZPos + 64) <= 90)
            {
                var pages = -1;
                LoadThisCategoryPage(parentCategory, tmpchar, currentPagePos, out pages);
                
                // Handle multiple pages per char
                if (currentPagePos < pages)
                {
                    currentPagePos++;
                }
                else
                {
                    currentPagePos = 0;
                    currentAToZPos++;

                    // Bit of a basic check, but if we've loaded the same page more than 4 times we'll stop loading them 
                    //  This happens if the page doesn't have multiple alphabetic pages
                    if (_lastLoadedPages != null && _lastLoadedPages.Count() >= 4)
                    {
                        if (_lastLoadedPages.Distinct().Count() == 1)
                            break;
                        else
                            _lastLoadedPages = null; // We've got 4 distinct pages, stop logging them
                    }

                    // Move to the next character
                    tmpchar = ((char)(currentAToZPos + 64)).ToString();
                }
            }
        }

        /// <summary>
        /// Load all videos for the specified category - for SkyGo we're representing a series episode as a category, so LoadVideos here is only ever going to return 1 item
        /// </summary>
        /// <param name="parentCategory"></param>
        /// <returns></returns>
        public List<VideoInfo> LoadVideos(Category parentCategory)
        {
            var results = new List<VideoInfo>();
            if (parentCategory.Type() != SkyGoCategoryData.CategoryType.Video)
                throw new ApplicationException("Cannot retrieve videos for non-video category");
            
            var doc = Properties.Resources.SkyGo_VideoDetailsUrl.Replace("{VIDEO_ID}", parentCategory.CategoryId()).LoadSkyGoContentFromUrl();
            var result = doc.LoadVideoFromDocument(parentCategory.CategoryId());

            results.Add(result);
            return results;
        }

        /// <summary>
        /// The name of the BrowserUtilConnector
        /// </summary>
        public string ConnectorEntityTypeName
        {
            get { return "OnlineVideos.Sites.WebAutomation.ConnectorImplementations.SkyGo.Connectors.SkyGoConnector"; }
        }

        /// <summary>
        /// Load a specific category page into the parentCategory
        /// </summary>
        /// <param name="parentCategory"></param>
        /// <param name="currentChar"></param>
        /// <param name="pageNo"></param>
        /// <param name="pages"></param>
        private void LoadThisCategoryPage(Category parentCategory, string currentChar, int pageNo, out int pages)
        {
            var doc = Properties.Resources.SkyGo_CategoryAToZUrl.Replace("{CATEGORY}", parentCategory.CategoryId()).Replace("{CHARACTER}", currentChar).Replace("{PAGE}", pageNo.ToString()).LoadSkyGoContentFromUrl();
            if (_lastLoadedPages != null) _lastLoadedPages.Add(doc.DocumentNode.InnerText);

            doc.LoadChildCategoriesFromDocument(parentCategory);

            pages = (TotalResults(doc) - 1) / 50;
        }

        /// <summary>
        /// Retrieve series information for the specified category
        /// </summary>
        /// <param name="parentCategory"></param>
        private void LoadSeriesInformation(Category parentCategory)
        {
            var doc = Properties.Resources.SkyGo_SeriesDetailsUrl.Replace("{SERIES_ID}", parentCategory.CategoryId()).LoadSkyGoContentFromUrl();
            var result = doc.LoadSeriesItemsFromDocument(parentCategory);
            parentCategory.SubCategories.AddRange(result);
            parentCategory.SubCategoriesDiscovered = true;
        }

        /// <summary>
        /// Retrieve the total results for the specified page
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private int TotalResults(HtmlDocument document)
        {
            try
            {
                var resultsDiv = document.GetElementById("searchResults");

                // Some pages take ages to load, so wait for the whole table of results
                if (resultsDiv != null)
                {
                    // First child is a div
                    if (resultsDiv.ChildNodes.Count > 0)
                    {
                        // First child of the first child is a p with the text we need
                        if (resultsDiv.ChildNodes[0].ChildNodes.Count > 0)
                        {
                            if (resultsDiv.ChildNodes[0].ChildNodes[0].OuterHtml.Contains("ATI_noResultsFound")) return 0;

                            // Find out how many results
                            var text = resultsDiv.ChildNodes[0].ChildNodes[0].InnerText;
                            if (text.Contains("&nbsp;"))
                                text = text.Replace("&nbsp;", " ");//text.Substring(text.IndexOf("&nbsp;"));
                            text = text.Replace("Previous Page", "").Trim();
                            return int.Parse(text.Split(' ')[3].Trim());
                        }
                    }
                }
            }
            catch 
            {
            }
            return -1;
        }
    }
}
