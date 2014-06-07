using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.DavidCalder.TMDB
{

  public class TVSeries
  {
    public string backdrop_path { get; set; }
    public Created_By[] created_by { get; set; }
    public int[] episode_run_time { get; set; }
    public string first_air_date { get; set; }
    public Genre[] genres { get; set; }
    public string homepage { get; set; }
    public int id { get; set; }
    public bool in_production { get; set; }
    public string[] languages { get; set; }
    public string last_air_date { get; set; }
    public string name { get; set; }
    public Network[] networks { get; set; }
    public int number_of_episodes { get; set; }
    public int number_of_seasons { get; set; }
    public string original_name { get; set; }
    public string[] origin_country { get; set; }
    public string overview { get; set; }
    public float popularity { get; set; }
    public string poster_path { get; set; }
    public Season[] seasons { get; set; }
    public string status { get; set; }
    public float vote_average { get; set; }
    public int vote_count { get; set; }
    public Images images { get; set; }
    public Videos videos { get; set; }
    public Credits credits { get; set; }
    public Translations translations { get; set; }
  }
}
