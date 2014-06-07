using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.DavidCalder.TMDB
{
  public class Episode
  {
    public string air_date { get; set; }
    public int episode_number { get; set; }
    public string name { get; set; }
    public string overview { get; set; }
    public string id { get; set; }
    public string production_code { get; set; }
    public string season_number { get; set; }
    public string still_path { get; set; }
    public float vote_average { get; set; }
    public int vote_count { get; set; }
  }
}
