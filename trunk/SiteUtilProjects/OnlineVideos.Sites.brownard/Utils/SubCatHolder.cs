using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Utils
{
    class SubCatHolder
    {
        public List<Category> SubCategories { get; set; }
        public Dictionary<string, string> SearchableCategories { get; set; }
        public object Other { get; set; }
    }
}
