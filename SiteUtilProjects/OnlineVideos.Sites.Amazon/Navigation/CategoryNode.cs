using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites.Amazon.Navigation
{
    public class CategoryNode
    {
        public Category Category { get; set; }
        public List<CategoryNode> SubCategories { get; set; } = new List<CategoryNode>();
        /// <summary>
        /// Url to actual title to be played.
        /// </summary>
        public String Url { get; set; }
        /// <summary>
        /// Specifies the type of content and how the result should be parsed
        /// </summary>
        public UrlType UrlType { get; set; }

        public Regex ParseRegex { get; set; }
    }

    public enum UrlType
    {
        Undefined,
        Home,
        SeriesCategory,
        Movies,
        SearchResults,
        Genres
    }
}
