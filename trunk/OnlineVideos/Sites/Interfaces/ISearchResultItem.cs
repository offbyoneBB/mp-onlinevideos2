using System;
using System.Collections.Generic;
using System.Text;

namespace OnlineVideos
{
    /// <summary>
    /// A common interface for <see cref="Category"/> and <see cref="VideoInfo"/> to allow both to be returned from a search.
    /// </summary>
    public interface ISearchResultItem
    {
        string Description { get; set; }
        string ThumbnailImage { get; set; }
        object Other { get; set; }
    }
}
