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
            Root,
            Series,
            Video
        }

        /// <summary>
        /// Determine if this Category represents a series or not
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public static CategoryType Type(this Category category)
        {
            if (category.Other.ToString().StartsWith("S"))
                return CategoryType.Series;
            if (category.Other.ToString().StartsWith("V"))
                return CategoryType.Video;
            if (category.Other.ToString().StartsWith("R"))
                return CategoryType.Root;
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
