using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.WebAutomation.ConnectorImplementations._4OD.Extensions
{
    /// <summary>
    /// Extend Category with methods to get the id and the type
    /// Other will contain {G/C/U/P}~{Id}~{Category Info Page}~{Series No}
    /// </summary>
    public static class _4ODCategoryData
    {
        public enum CategoryType
        {
            Unknown,
            GeneralCategory,
            Collection,
            CatchUp,
            Programme
        }

        /// <summary>
        /// Determine if this Category represents a Collection or not
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public static CategoryType Type(this Category category)
        {
            if (category.Other.ToString().StartsWith("G"))
                return CategoryType.GeneralCategory;
            if (category.Other.ToString().StartsWith("C"))
                return CategoryType.Collection;
            if (category.Other.ToString().StartsWith("U"))
                return CategoryType.CatchUp;
            if (category.Other.ToString().StartsWith("P"))
                return CategoryType.Programme;
            return CategoryType.Unknown;
        }

        /// <summary>
        /// Category Id
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public static string CategoryId(this Category category)
        {
            if (string.IsNullOrEmpty(category.Other.ToString())) return string.Empty;
            return category.Other.ToString().Split('~')[1];
        }

        /// <summary>
        /// The url of the info page for the category
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public static string CategoryInformationPage(this Category category)
        {
            if (string.IsNullOrEmpty(category.Other.ToString()) || category.Other.ToString().Split('~').Count() < 3) return string.Empty;
            return category.Other.ToString().Split('~')[2];
        }

        /// <summary>
        /// The series id
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public static string SeriesId(this Category category)
        {
            if (string.IsNullOrEmpty(category.Other.ToString()) || category.Other.ToString().Split('~').Count() < 4) return string.Empty;
            return category.Other.ToString().Split('~')[3];
        }
    }
}
