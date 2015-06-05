using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.WebAutomation.ConnectorImplementations.SkyGo.Extensions
{
    /// <summary>
    /// Extend Category with methods to get the Channel and the Id
    /// </summary>
    public static class SkyGoCategoryData
    {
        public enum CategoryType
        { 
            Unknown,
            Movie,
            CatchUp,
            CatchUpSubCategory, 
            CatchUpSubCategory1,
            BoxSets,
            Video
        }

        /// <summary>
        /// Determine if this Category represents a series or not
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public static CategoryType Type(this Category category)
        {
            if (category.Other.ToString().StartsWith("M~"))
                return CategoryType.Movie;
            if (category.Other.ToString().StartsWith("CS~"))
                return CategoryType.CatchUpSubCategory;
            if (category.Other.ToString().StartsWith("C1~"))
                return CategoryType.CatchUpSubCategory1;
            if (category.Other.ToString().StartsWith("C~"))
                return CategoryType.CatchUp;
            if (category.Other.ToString().StartsWith("B~"))
                return CategoryType.BoxSets;
            if (category.Other.ToString().StartsWith("V~"))
                return CategoryType.Video;
            return CategoryType.Unknown;
        }

        /// <summary>
        /// Other will contain {S/V/R}~{Series/Video/Root Id}~{Channel}
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public static string CategoryId(this Category category)
        {
            if (string.IsNullOrEmpty(category.Other.ToString())) return string.Empty;
            return category.Other.ToString().Split('~')[1];
        }

        /// <summary>
        /// Other will contain {S/V/R}~{Series/Video/Root Id}~{Channel}
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public static string Channel(this Category category)
        {
            if (string.IsNullOrEmpty(category.Other.ToString())) return string.Empty;
            var result = category.Other.ToString().Split('~');
            if (result.Count() > 1)
                return result[1];
            return string.Empty;
        }
    }
}
