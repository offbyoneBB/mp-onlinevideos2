using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.DavidCalder.TMDB
{
  public class Images
  {
    public string id { get; set; }
    public Backdrop[] backdrops { get; set; }
    public Poster[] posters { get; set; }
  }
}
