using OnlineVideos.Sites.WebAutomation.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites.WebAutomation.ConnectorImplementations.AmazonPrime.Extensions;
using System.Globalization;
using OnlineVideos.Sites.WebAutomation.Properties;

namespace OnlineVideos.Sites.WebAutomation.ConnectorImplementations.AmazonPrime.Connectors
{
    public class AmazonPrimeInformationConnector : IInformationConnector
    {
        SiteUtilBase _siteUtil;

        public AmazonPrimeInformationConnector(SiteUtilBase siteUtil)
        {
            _siteUtil = siteUtil;
        }

        /// <summary>
        /// Don't want the results sorted
        /// </summary>
        public bool ShouldSortResults
        {
            get { return false; }
        }

        /// <summary>
        /// The player class name
        /// </summary>
        public string ConnectorEntityTypeName
        {
            get { return "OnlineVideos.Sites.WebAutomation.ConnectorImplementations.AmazonPrime.Connectors.AmazonPrimeConnector"; }
        }

        /// <summary>
        /// Load the categories
        /// </summary>
        /// <param name="parentCategory"></param>
        /// <returns></returns>
        public List<Category> LoadCategories(Category parentCategory = null)
        {
            Properties.Resources.Culture = new CultureInfo(_siteUtil.Settings == null ? string.Empty : _siteUtil.Settings.Language);

            var result = new List<Category>();

            if (parentCategory == null) 
            {
                result.Add(new Category { HasSubCategories = true, Name = "Movies", SubCategoriesDiscovered = false, Other="M", Thumb = Properties.Resources.AmazonMovieIcon });
                result.Add(new Category { HasSubCategories = true, Name = "Tv", SubCategoriesDiscovered = false, Other = "T", Thumb = Properties.Resources.AmazonTvIcon });
            }
            else
            {
                 // Grab next page categories here (we'll deal with videos as the category)
                if (parentCategory is NextPageCategory)
                {
                    result = (parentCategory as NextPageCategory).Url.LoadAmazonPrimeVideosAsCategoriesFromUrl(parentCategory.ParentCategory);
                    parentCategory.ParentCategory.SubCategories.AddRange(result);
                }
                else
                {
                    if (parentCategory.Other.ToString() == "M")
                        result = Properties.Resources.AmazonMovieCategoriesUrl.LoadAmazonPrimeCategoriesFromUrl(parentCategory);
                    else
                    {
                        if (parentCategory.Other.ToString().StartsWith("V~"))
                            result = ((parentCategory.Other.ToString().ToLower().Contains(Properties.Resources.AmazonRootUrl.ToLower()) ? string.Empty : Properties.Resources.AmazonRootUrl) + (parentCategory.Other.ToString()).Replace("V~", string.Empty)).LoadAmazonPrimeVideosAsCategoriesFromUrl(parentCategory);
                        else

                            result = Properties.Resources.AmazonTVCategoriesUrl.LoadAmazonPrimeCategoriesFromUrl(parentCategory);
                    }
                    parentCategory.SubCategories.AddRange(result);
                }
              
            }
            return result;
        }

        /// <summary>
        /// Load the videos using either the parentCategory, or the next page url (if parentCategory is null)
        /// </summary>
        /// <param name="parentCategory"></param>
        /// <returns></returns>
        public List<VideoInfo> LoadVideos(Category parentCategory)
        {
            return parentCategory.Other.ToString().LoadVideosFromUrl();

        }

    }
}
