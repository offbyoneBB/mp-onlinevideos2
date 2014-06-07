using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.DavidCalder.TMDB
{
  public class Poster
  {
    public string file_path { get; set; }
    public int width { get; set; }
    public int height { get; set; }
    public string iso_639_1 { get; set; }
    public float aspect_ratio { get; set; }
    public float vote_average { get; set; }
    public int vote_count { get; set; }
  }
}
