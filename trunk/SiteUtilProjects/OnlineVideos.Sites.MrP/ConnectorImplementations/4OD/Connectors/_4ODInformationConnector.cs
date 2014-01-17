using OnlineVideos.Sites.WebAutomation.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using OnlineVideos.Sites.WebAutomation.Extensions;
using OnlineVideos.Sites.WebAutomation.ConnectorImplementations._4OD.Extensions;
using System.Net;

namespace OnlineVideos.Sites.WebAutomation.ConnectorImplementations._4OD.Connectors
{
    /// <summary>
    /// 4OD information connector - based on OnlineVideos.Sites.Brownard FourodUtil, but replaced the regexs with a htmlagilitypack implementation
    /// </summary>
    public class _4ODInformationConnector : IInformationConnector
    {
        string defaultLogo;
        SiteUtilBase _siteUtil;

        public _4ODInformationConnector(SiteUtilBase siteUtil)
        {
            _siteUtil = siteUtil;
            defaultLogo = string.Format(@"{0}\Icons\{1}.png", OnlineVideoSettings.Instance.ThumbsDir, siteUtil.Settings.Name);
        }

        public List<Category> LoadCategories(Category parentCategory = null)
        {
            if (parentCategory == null)
            {
                var categories = new List<Category>();

                var subCategs = new List<Category>();

                subCategs.Add(new Category { Name = "Animals", HasSubCategories = true, Other = "G~animals" });
                subCategs.Add(new Category { Name = "Animation", HasSubCategories = true, Other = "G~animation" });
                subCategs.Add(new Category { Name = "Art, Design and Literature", HasSubCategories = true, Other = "G~art-design-and-literature" });
                subCategs.Add(new Category { Name = "Business and Money", HasSubCategories = true, Other = "G~business-and-money" });
                subCategs.Add(new Category { Name = "Chat Shows", HasSubCategories = true, Other = "G~chat-shows" });
                subCategs.Add(new Category { Name = "Children's Shows", HasSubCategories = true, Other = "G~childrens-shows" });
                subCategs.Add(new Category { Name = "Comedy", HasSubCategories = true, Other = "G~comedy" });
                subCategs.Add(new Category { Name = "Documentaries", HasSubCategories = true, Other = "G~documentaries" });
                subCategs.Add(new Category { Name = "Drama", HasSubCategories = true, Other = "G~drama" });
                subCategs.Add(new Category { Name = "Education and Learning", HasSubCategories = true, Other = "G~education-and-learning" });
                subCategs.Add(new Category { Name = "Entertainment", HasSubCategories = true, Other = "G~entertainment" });
                subCategs.Add(new Category { Name = "Family and Parenting", HasSubCategories = true, Other = "G~family-and-parenting" });
                subCategs.Add(new Category { Name = "Fashion and Beauty", HasSubCategories = true, Other = "G~fashion-and-beauty" });
                subCategs.Add(new Category { Name = "Film", HasSubCategories = true, Other = "G~film" });
                subCategs.Add(new Category { Name = "Food", HasSubCategories = true, Other = "G~food" });
                subCategs.Add(new Category { Name = "Health and Wellbeing", HasSubCategories = true, Other = "G~health-and-wellbeing" });
                subCategs.Add(new Category { Name = "History", HasSubCategories = true, Other = "G~history" });
                subCategs.Add(new Category { Name = "Homes and Gardens", HasSubCategories = true, Other = "G~homes-and-gardens" });
                subCategs.Add(new Category { Name = "Lifestyle", HasSubCategories = true, Other = "G~lifestyle" });
                subCategs.Add(new Category { Name = "News, Current Affairs and Politics", HasSubCategories = true, Other = "G~news-current-affairs-and-politics" });
                subCategs.Add(new Category { Name = "Quizzes and Gameshows", HasSubCategories = true, Other = "G~quizzes-and-gameshows" });
                subCategs.Add(new Category { Name = "Reality Shows", HasSubCategories = true, Other = "G~reality-shows" });
                subCategs.Add(new Category { Name = "Religion and Belief", HasSubCategories = true, Other = "G~religion-and-belief" });
                subCategs.Add(new Category { Name = "Science, Nature and the Environment", HasSubCategories = true, Other = "G~science-nature-and-the-environment" });
                subCategs.Add(new Category { Name = "Sex and Relationships", HasSubCategories = true, Other = "G~sex-and-relationships" });
                subCategs.Add(new Category { Name = "Society and Culture", HasSubCategories = true, Other = "G~society-and-culture" });
                subCategs.Add(new Category { Name = "Sports and Games", HasSubCategories = true, Other = "G~sports-and-games" });
                subCategs.Add(new Category { Name = "US Shows", HasSubCategories = true, Other = "G~us-shows" });
                
                var allChannels = new Category { Name = "All Channels", HasSubCategories = true, SubCategoriesDiscovered = true, SubCategories = subCategs };
                allChannels.SubCategories.ForEach(x => x.ParentCategory = allChannels);
                categories.Add(allChannels);

                categories.Add(new Category { Name = "4Music", HasSubCategories = true, Other = "G~music", Thumb = "http://d8si6upl43lp1.cloudfront.net/static/4nav/2.0.1/images/header-4music.png" });
                categories.Add(new Category { Name = "4Seven", HasSubCategories = true, Other = "G~seven", Thumb = "http://d8si6upl43lp1.cloudfront.net/static/4nav/2.0.1/images/header-47.png" });
                categories.Add(new Category { Name = "Channel 4", HasSubCategories = true, Other = "G~c4", Thumb = "http://d8si6upl43lp1.cloudfront.net/static/4nav/2.0.1/images/header-c4.png" });
                categories.Add(new Category { Name = "E4", HasSubCategories = true, Other = "G~e4", Thumb = "http://d8si6upl43lp1.cloudfront.net/static/4nav/2.0.1/images/header-e4.png" });
                categories.Add(new Category { Name = "More4", HasSubCategories = true, Other = "G~more4", Thumb = "http://d8si6upl43lp1.cloudfront.net/static/4nav/2.0.1/images/header-more4.png" });
                
              
                categories.Add(new Category { Name = "Collections", HasSubCategories = true, Other="C~"});

                // Add the catch up section - it'll be "Catch Up"->{Channel}->Day->Programmes
                subCategs = new List<Category>();
                subCategs.Add(new Category { Name = "4Music", HasSubCategories = true, Other = "U~4M", Thumb = "http://d8si6upl43lp1.cloudfront.net/static/4nav/2.0.1/images/header-4music.png" });
                subCategs.Add(new Category { Name = "4Seven", HasSubCategories = true, Other = "U~4S", Thumb = "http://d8si6upl43lp1.cloudfront.net/static/4nav/2.0.1/images/header-47.png" });
                subCategs.Add(new Category { Name = "Channel 4", HasSubCategories = true, Other = "U~C4", Thumb = "http://d8si6upl43lp1.cloudfront.net/static/4nav/2.0.1/images/header-c4.png" });
                subCategs.Add(new Category { Name = "E4", HasSubCategories = true, Other = "U~E4", Thumb = "http://d8si6upl43lp1.cloudfront.net/static/4nav/2.0.1/images/header-e4.png" });
                subCategs.Add(new Category { Name = "More4", HasSubCategories = true, Other = "U~M4", Thumb = "http://d8si6upl43lp1.cloudfront.net/static/4nav/2.0.1/images/header-more4.png" });
                var catchUp = new Category { Name = "Catch Up", HasSubCategories = true, SubCategoriesDiscovered = true, SubCategories = subCategs };

                categories.Add(catchUp);
                catchUp.SubCategories.ForEach(x => x.ParentCategory = catchUp);

                return categories;
            }
            else
                return DiscoverSubCategories(parentCategory);
        }

        /// <summary>
        /// Video list
        /// </summary>
        /// <param name="parentCategory"></param>
        /// <returns></returns>
        public List<VideoInfo> LoadVideos(Category parentCategory)
        {
            switch (parentCategory.Type())
            {
                case _4ODCategoryData.CategoryType.Programme:
                    return _4ODVideoParser.LoadGeneralVideos(parentCategory);
                case _4ODCategoryData.CategoryType.Collection:
                    return _4ODVideoParser.LoadCollectionVideos(parentCategory);
                case  _4ODCategoryData.CategoryType.CatchUp:
                    return _4ODVideoParser.LoadCatchUpVideos(parentCategory);
            }

            return null;
        }

        /// <summary>
        /// The name of the BrowserUtilConnector for 4OD
        /// </summary>
        public string ConnectorEntityTypeName
        {
            get { return "OnlineVideos.Sites.WebAutomation.ConnectorImplementations._4OD.Connectors._4ODConnector"; }
        }
        
        /// <summary>
        /// Build the sub category list
        /// </summary>
        /// <param name="parentCategory"></param>
        /// <returns></returns>
        public List<Category> DiscoverSubCategories(Category parentCategory)
        {
           switch(parentCategory.Type())
           {
               case _4ODCategoryData.CategoryType.GeneralCategory:
               case _4ODCategoryData.CategoryType.Collection:
                   parentCategory.SubCategories = LoadGeneralCategory(parentCategory);
                   break;
               case _4ODCategoryData.CategoryType.Programme:
                   parentCategory.SubCategories = _4ODCategoryParser.LoadProgrammeInfo(parentCategory);
                   break;
               case _4ODCategoryData.CategoryType.CatchUp:
                   parentCategory.SubCategories = _4ODCategoryParser.LoadCatchUpDays(parentCategory);
                   break;
           }
           return parentCategory.SubCategories;
        }

        /// <summary>
        /// Load general categories from the 4OD api page
        /// </summary>
        /// <param name="parentCategory"></param>
        /// <returns></returns>
        private List<Category> LoadGeneralCategory(Category parentCategory)
        {
            var url = Properties.Resources._4OD_CategoryListUrl.Replace("{CATEGORY}", (string.IsNullOrEmpty(parentCategory.CategoryId()) ? string.Empty : parentCategory.CategoryId() + "/"));
            if (parentCategory.Type() == _4ODCategoryData.CategoryType.Collection)
                url = Properties.Resources._4OD_CollectionListUrl;
            var result = LoadGeneralCategory(url, parentCategory);
            return result;
        }

        /// <summary>
        /// Load general categories from the 4OD api page
        /// </summary>
        /// <param name="parentCategory"></param>
        /// <returns></returns>
        private List<Category> LoadGeneralCategory(string url, Category parentCategory)
        {
            var doc = new HtmlDocument();
            var results = new List<Category>();
            var webRequest = (HttpWebRequest)WebRequest.Create(url);
            var webResponse = (HttpWebResponse)webRequest.GetResponse();

            if (webResponse.StatusCode != HttpStatusCode.OK)
                throw new OnlineVideosException("Unable to retrieve response for 4OD from " + url + ", received " + webResponse.StatusCode.ToString());

            doc.Load(webResponse.GetResponseStream());

            // Load all the list items from the page into categories
            results.AddRange(doc.GetElementsByTagName("li").Where(x => !string.IsNullOrEmpty(x.GetAttribute("class"))).Select(x => x.LoadGeneralCategoryFromListItem(parentCategory)));

            // See if there's a next page of results to load
            var nextPage = doc.GetElementsByTagName("ol").First().GetAttribute("data-nexturl");

            if (nextPage != "endofresults")
                results.AddRange(LoadGeneralCategory(Properties.Resources._4OD_RootUrl + nextPage, parentCategory));

            return results;
        }
    }
}
