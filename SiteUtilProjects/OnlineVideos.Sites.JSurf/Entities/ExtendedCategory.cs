using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.JSurf.Entities
{
    /// <summary>
    /// Add a sort column to category
    /// </summary>
    public class ExtendedCategory : Category
    {
        public string SortValue { get; set; }
    }
}
