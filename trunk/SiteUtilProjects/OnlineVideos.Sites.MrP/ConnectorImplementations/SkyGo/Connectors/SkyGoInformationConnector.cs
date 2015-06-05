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
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

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
        // Sometimes we load the category info from the videos directly, so we might as well cache the video info for later use
        private Dictionary<VideoInfo, string> _cachedVideos = new Dictionary<VideoInfo, string>();

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
                result.Add(new Category { Name = "Catch up", SubCategoriesDiscovered = false, HasSubCategories = true, Other = "C~a27eef2528673410VgnVCM100000255212ac____" });
                //result.Add(new Category { Name = "Live TV", SubCategoriesDiscovered = true, HasSubCategories = false, Other = "L~Live_TV" });
                result.Add(new Category { Name = "Sky Movies", SubCategoriesDiscovered = false, HasSubCategories = true, Other = "R~7fc1acce88d77410VgnVCM1000000b43150a____" });
                result.Add(new Category { Name = "TV Box Sets", SubCategoriesDiscovered = false, HasSubCategories = true, Other = "B~9bb07a0acc5a7410VgnVCM1000000b43150a____" });
            }
            else
            {
                switch (parentCategory.Type())
                { 
                    case SkyGoCategoryData.CategoryType.CatchUp:
                        LoadCatchupInformation(parentCategory);
                        break;
                    default:
                        LoadSubCategories(parentCategory, parentCategory.Type() != SkyGoCategoryData.CategoryType.CatchUpSubCategory);
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// Use the api version of the Sky Go pages to load categories - we'll multi thread this to load the different alphabet characters simultaneously
        /// </summary>
        /// <param name="parentCategory"></param>
        private void LoadSubCategories(Category parentCategory, bool shouldRunThroughAllChars)
        {
            var tmpchar = "%23";
            var currentAToZPos = 0;
            var currThreadHandle = 0;
            try
            {
                if (shouldRunThroughAllChars)
                {
                    var pool = new List<Task>();

                    // Loop through the whole alphabet
                    while ((currentAToZPos + 64) <= 90)
                    {
                        var tmpParams = new LoadSubCategParams { CurrentChar = tmpchar, ParentCategory = parentCategory, Index = currThreadHandle };

                        pool.Add(Task.Factory.StartNew(() => LoadCharacterSubCateg(tmpParams)));

                        currentAToZPos++;

                        // Move to the next character
                        tmpchar = ((char)(currentAToZPos + 64)).ToString();

                    }
                    var timeout = OnlineVideoSettings.Instance.UtilTimeout <= 0 ? 30000 : OnlineVideoSettings.Instance.UtilTimeout * 1000;
                    Task.WaitAll(pool.ToArray(), timeout);
                }
                else
                {
                    var tmpParams = new LoadSubCategParams { CurrentChar = "", ParentCategory = parentCategory, Index = 0 };

                    LoadCharacterSubCateg(tmpParams);
                }
            }
            catch (Exception ex)
            {
                OnlineVideos.Log.Error(ex);
            }
        }

        /// <summary>
        /// Load the category page for the specified character in a separate thread - this will pull out all pages for the specified character 
        /// </summary>
        /// <param name="parametersObject"></param>
        private void LoadCharacterSubCateg(LoadSubCategParams parameters)
        {
            try
            {
                LoadThisCategory(parameters.ParentCategory, parameters.CurrentChar);
            }
            catch (Exception ex)
            {
                OnlineVideos.Log.Error(ex);
            }

        }
        
        /// <summary>
        /// Load the videos for this category - either from the cache, or loaded from the video info page in the site
        /// </summary>
        /// <param name="parentCategory"></param>
        /// <returns></returns>
        public List<VideoInfo> LoadVideos(Category parentCategory)
        {
            if (_cachedVideos.Where(x => x.Value.Contains("*" + parentCategory.Other + parentCategory.Name + "*")).Count() > 0)
                return _cachedVideos.Where(x => x.Value.Contains("*" + parentCategory.Other + parentCategory.Name + "*")).Select(x => x.Key).ToList();
            return LoadGeneralVideos(parentCategory);
        }

        /// <summary>
        /// The name of the BrowserUtilConnector
        /// </summary>
        public string ConnectorEntityTypeName
        {
            get { return "OnlineVideos.Sites.WebAutomation.ConnectorImplementations.SkyGo.Connectors.SkyGoConnector"; }
        }


        /// <summary>
        /// Load a specific category into the parentCategory
        /// </summary>
        /// <param name="parentCategory"></param>
        /// <param name="currentChar"></param>
        private void LoadThisCategory(Category parentCategory, string currentChar)
        {
            lock (parentCategory)
            {
                VideoInfo video = null;

                var tmpObj = (currentChar == "" ? Properties.Resources.SkyGo_CatchUpSubItemsUrl(parentCategory.CategoryId()) : Properties.Resources.SkyGo_AllListUrl(parentCategory.CategoryId(), currentChar)).GetLinksTokensFromUrl(parentCategory.Type());
                if (tmpObj == null) return;

                foreach (var item in tmpObj)
                {
                    Category thisItem = null;
                    if (item["title"] != null)
                    {   
                        var contentType = item.GetValue("contentType");
                        if (contentType == "MOVIE" || contentType == "STANDARD_VIDEO")
                        {
                            if (contentType == "MOVIE")
                                video = item.VideoInfoFromToken("movies");
                            else
                                video = item.VideoInfoFromToken();
                            _cachedVideos.Add(video, "");
                        }
                        else
                        {
                            thisItem = new Category();
                            thisItem.Description = item.GetValue("synopsis") + "\r\n" + item.GetStarring();
                            thisItem.Name = item.GetValue("title");
                            thisItem.Other = "C~" + item.GetValue("id");
                            thisItem.SubCategoriesDiscovered = false;
                            thisItem.HasSubCategories = item.GetValue("contentType") != "SERIES";
                            thisItem.Thumb = item.GetImage();
                        }

                        if (item["categories"] != null && parentCategory.Type() != SkyGoCategoryData.CategoryType.CatchUpSubCategory)
                        {

                            foreach (var categ in item["categories"])
                            {
                                var tmpCateg = parentCategory.SubCategories.Where(x => x.Name == categ.ToString()).FirstOrDefault();

                                if (tmpCateg == null)
                                {
                                    tmpCateg = new Category();
                                    parentCategory.SubCategories.Add(tmpCateg);
                                }

                                tmpCateg.Other = parentCategory.Other;
                                tmpCateg.Name = categ.ToString();
                                if (video != null) _cachedVideos[video] += "*" + tmpCateg.Other + tmpCateg.Name + "*"; 
                                tmpCateg.SubCategoriesDiscovered = true;
                                tmpCateg.HasSubCategories = (thisItem != null);
                                if (tmpCateg.SubCategories == null) tmpCateg.SubCategories = new List<Category>();
                                if (thisItem != null)
                                {
                                    if (thisItem.ParentCategory == null)
                                        thisItem.ParentCategory = tmpCateg;
                                    tmpCateg.SubCategories.Add(thisItem);
                                }
                            }
                        }
                        else
                        {
                            parentCategory.SubCategories.Add(thisItem);
                            thisItem.ParentCategory = parentCategory;
                        }
                            
                    }
                }

            }
        }

        /// <summary>
        /// Load information for catch up category - it's structured differently from other areas
        /// </summary>
        /// <param name="parentCategory"></param>
        private void LoadCatchupInformation(Category parentCategory)
        {
            var tmpObj = Properties.Resources.SkyGo_CatchUpCategoriesUrl.GetLinksTokensFromUrl(SkyGoCategoryData.CategoryType.CatchUp);

            foreach (var item in tmpObj)
            {
                if (item.GetValue("_rel") == "child/node")
                {
                    var categoryType = "CS~";

                    var catchUpItem = new Category();
                    catchUpItem.Name = item.GetValue("_title");
                    if (catchUpItem.Name == "Featured") continue;

                    var id = item.GetIdFromHrefValue();

                    if (!item.GetValue("_attributes").Contains("\"classifier\": \"page\"")) categoryType = "C1~";

                    catchUpItem.Other = categoryType + id;

                    if (catchUpItem.Type() != SkyGoCategoryData.CategoryType.CatchUpSubCategory)
                    {
                        // We have to do an extra lookup for the "All" sub category
                        var tmpObj2 = Properties.Resources.SkyGo_CatchUpSubItemsUrl(catchUpItem.CategoryId()).GetLinksTokensFromUrl(SkyGoCategoryData.CategoryType.CatchUp);
                        foreach (var thisLink in tmpObj2)
                        {
                            if (thisLink.GetValue("_title") == "All")
                            {

                                if (catchUpItem.Name == "Demand 5") categoryType = "CS~";
                                catchUpItem.Other = categoryType + thisLink.GetIdFromHrefValue();

                                break;
                            }
                        }
                    }
                    catchUpItem.HasSubCategories = true;
                    catchUpItem.SubCategoriesDiscovered = false;
                    catchUpItem.ParentCategory = parentCategory;
                    parentCategory.SubCategoriesDiscovered = true;
                    parentCategory.HasSubCategories = true;
                    parentCategory.SubCategories.Add(catchUpItem);
                }
            }
        }

        /// <summary>
        /// Load general video info (videos we haven't cached)
        /// </summary>
        /// <param name="series"></param>
        /// <returns></returns>
        private List<VideoInfo> LoadGeneralVideos(Category series)
        {
            var result = new List<VideoInfo>();
            var tmpObj = Properties.Resources.SkyGo_SeriesInfoUrl(series.CategoryId()).GetLinksTokensFromUrl(SkyGoCategoryData.CategoryType.Video);
            foreach (var item in tmpObj)
            {
                if (item.GetValue("_rel") == "episode/episode")
                {
                    result.Add(item.VideoInfoFromToken());
                }
                   
            }

            return result;
        }

        public bool CanSearch { get { return false; } }

        public List<SearchResultItem> DoSearch(string query) { return null; }
    }
}
