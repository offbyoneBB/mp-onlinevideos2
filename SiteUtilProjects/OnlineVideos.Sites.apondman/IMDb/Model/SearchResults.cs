using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Pondman.IMDb.Model
{
    public class SearchResults
    {
        public SearchResults()
        {
            Titles = new Dictionary<ResultType, List<TitleReference>>();
            Names = new Dictionary<ResultType, List<NameReference>>();
        }
        
        public virtual Dictionary<ResultType, List<TitleReference>> Titles { get; internal set; }

        public virtual Dictionary<ResultType, List<NameReference>> Names { get; internal set; }
    }
}
