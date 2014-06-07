using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.DavidCalder.TMDB
{
  public class ProductionCountries
  {
    public string iso_3166_1 { get; set; }
    public string name { get; set; }

    public override string ToString()
    {
      return name;
    }
  }
}
