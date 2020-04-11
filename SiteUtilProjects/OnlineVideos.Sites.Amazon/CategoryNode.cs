using System;
using System.Collections.Generic;

namespace OnlineVideos.Sites.Amazon
{
    public partial class AmazonUtil
    {
        private class CategoryNode
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

        }

        private enum UrlType
        {
            Undefined,
            SeriesCategory,
            Movies,
            SearchResults,
            Genres
        }
    }
}
