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
            /// Url to retrieve sub-categories. This is done for Series -> Episodes link.
            /// </summary>
            public String CategoryUrl { get; set; }
        }
    }
}
