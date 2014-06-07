using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.DavidCalder.TMDB
{
  public class ProductionCompanies
  {
    public string name { get; set; }
    public int id { get; set; }

    public override string ToString()
    {
      return name;
    }
  }
}
