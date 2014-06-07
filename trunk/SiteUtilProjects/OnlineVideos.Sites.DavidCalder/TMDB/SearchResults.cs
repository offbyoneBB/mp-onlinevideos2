using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.DavidCalder
{
  public class Result
  {
    public bool adult { get; set; }
    public string backdrop_path { get; set; }
    public string id { get; set; }
    public string original_name { get; set; }
    public string original_title { get; set; }
    public string first_air_date { get; set; }
    public string release_date { get; set; }
    public string poster_path { get; set; }
    public float popularity { get; set; }
    public string name { get; set; }
    public string title { get; set; }
    public float vote_average { get; set; }
    public int vote_count { get; set; }
  }

  public class SearchResult  
  {
    public int page { get; set; }
    public Result[] results { get; set; }
    public int total_pages { get; set; }
    public int total_results { get; set; }

    public string ID
    {
      get { return results[0].id.ToString(); }
      set { ID = value; }
    }
  }
}
