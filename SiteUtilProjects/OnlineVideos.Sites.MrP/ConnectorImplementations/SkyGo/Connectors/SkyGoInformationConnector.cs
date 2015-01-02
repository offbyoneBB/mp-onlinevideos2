using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites.WebAutomation.Interfaces;
using OnlineVideos.Sites.WebAutomation.ConnectorImplementations.SkyGo.Extensions;
using System.Xml;
using HtmlAgilityPack;
using OnlineVideos.Sites.WebAutomation.Extensions;
using System.Threading;

namespace OnlineVideos.Sites.WebAutomation.ConnectorImplementations.SkyGo.Connectors
{
    public class SkyGoInformationConnector : IInformationConnector
    {
        /// <summary>
        /// The parameters to use when loading the sub category on a separate thread
        /// </summary>
        private class LoadSubCategParams
        {
            public Category ParentCategory { get; set; }
            public string CurrentChar { get; set; }
            public int Index { get; set; }
        }

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
        private const int NumThreads = 5;
        private static ManualResetEvent[] resetEvents = new ManualResetEvent[NumThreads];

        public SkyGoInformationConnector(SiteUtilBase siteUtil)
        {
            _siteUtil = siteUtil;
        }


        /// <summary>
        /// Let the util sort the results
        /// </summary>
        public bool ShouldSortResults
        {
            get { return true; }
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
                result.Add(new Category { Name = "Live TV", SubCategoriesDiscovered = false, HasSubCategories = false, Other = "L~Live_TV" });
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
            var currThreadHandle = 0;

            // Loop through the whole alphabet
            while ((currentAToZPos + 64) <= 90)
            {
                var tmpParams = new LoadSubCategParams { CurrentChar = tmpchar, ParentCategory = parentCategory, Index = currThreadHandle };
                resetEvents[currThreadHandle] = new ManualResetEvent(false);
                ThreadPool.QueueUserWorkItem(new WaitCallback(LoadCharacterSubCateg), (object)tmpParams);
                
                currentAToZPos++;

                // Move to the next character
                tmpchar = ((char)(currentAToZPos + 64)).ToString();
                currThreadHandle++;

                // Wait for all threads to complete if the array is fully loaded
                if (currThreadHandle >= NumThreads)
                {
                    //WaitHandle.WaitAll(resetEvents, 10000);
                    foreach (var e in resetEvents)
                        e.WaitOne(10000); 
                    currThreadHandle = 0;
                }
            }
        }

        /// <summary>
        /// Load the category page for the specified character in a separate thread - this will pull out all pages for the specified character 
        /// </summary>
        /// <param name="parametersObject"></param>
        private void LoadCharacterSubCateg(object parametersObject)
        {
            var pages = -1;
            var currentPagePos = 0;
            var parameters = (LoadSubCategParams)parametersObject;

            while (pages > -2)
            {
                LoadThisCategoryPage(parameters.ParentCategory, parameters.CurrentChar, currentPagePos, out pages);

                // Handle multiple pages per char
                if (currentPagePos < pages)
                    currentPagePos++;
                else
                    pages = -2;
            }

            resetEvents[parameters.Index].Set();
        }

        /// <summary>
        /// Load all videos for the specified category - for SkyGo we're representing a series episode as a category, so LoadVideos here is only ever going to return 1 item
        /// </summary>
        /// <param name="parentCategory"></param>
        /// <returns></returns>
        public List<VideoInfo> LoadVideos(Category parentCategory)
        {
            var results = new List<VideoInfo>();
            if (parentCategory.Type() != SkyGoCategoryData.CategoryType.Video && parentCategory.Type() != SkyGoCategoryData.CategoryType.LiveTv)
                throw new ApplicationException("Cannot retrieve videos for non-video category");


            if (parentCategory.Type() != SkyGoCategoryData.CategoryType.LiveTv)
            {
                var doc = Properties.Resources.SkyGo_VideoDetailsUrl(parentCategory.CategoryId()).LoadSkyGoContentFromUrl();
                var result = doc.LoadVideoFromDocument(parentCategory.CategoryId());

                results.Add(result);
            }
            else
            {

                var channels = Properties.Resources.SkyGo_LiveTvListingUrl.LoadSkyGoLiveTvChannelsFromUrl();
                results = Properties.Resources.SkyGo_LiveTvGetNowNextUrl(String.Join(",", channels.Select(x => x.ChannelId).ToArray())).LoadSkyGoLiveTvNowNextVideosFromUrl(channels);
            }
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
            var doc = Properties.Resources.SkyGo_CategoryAToZUrl(parentCategory.CategoryId(), currentChar, pageNo.ToString()).LoadSkyGoContentFromUrl();

            lock (parentCategory)
            {
                doc.LoadChildCategoriesFromDocument(parentCategory);
            }
            pages = (TotalResults(doc) - 1) / 50;
        }

        /// <summary>
        /// Retrieve series information for the specified category
        /// </summary>
        /// <param name="parentCategory"></param>
        private void LoadSeriesInformation(Category parentCategory)
        {
            var doc = Properties.Resources.SkyGo_SeriesDetailsUrl(parentCategory.CategoryId()).LoadSkyGoContentFromUrl();
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
