using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.DavidCalder.TMDB
{
  public class Season
  {
    public string air_date { get; set; }
    public Episode[] episodes { get; set; }
    public string name { get; set; }
    public string overview { get; set; }
    public int id { get; set; }
    public string poster_path { get; set; }
    public int season_number { get; set; }
  }
}
