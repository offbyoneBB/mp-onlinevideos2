using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.DavidCalder
{
  public class Credits
  {
    public int id { get; set; }
    public TMDB.Cast[] cast { get; set; }
    public TMDB.Crew[] crew { get; set; }

    public string CastList()
    {
      List<string> list = new List<string>();
      foreach (TMDB.Cast _cast in cast)
      {
        list.Add(_cast.name);
      }
      return string.Join(", ", list.ToArray());
    }

    public string CrewList(string department)
    {
      List<string> list = new List<string>();
      foreach (TMDB.Crew _crew in crew)
      {
        if (_crew.department == department)
          list.Add(_crew.name);
      }
      return string.Join(", ", list.ToArray());
    }
  }

  public class Videos
  {
    public string id { get; set; }
    public TMDB.Trailer[] results { get; set; }

    public List<TMDB.Trailer> TrailerList()
    {
      List<TMDB.Trailer> list = new List<TMDB.Trailer>();
      foreach (TMDB.Trailer trailer in results)
      {
        list.Add(trailer);
      }
      return list;
    }
  }

  public class Releases
  {
    public TMDB.Country[] countries { get; set; }
  }

  public class Certification
  {
    public Certifications certifications { get; set; }
  }

  public class Certifications
  {
    public AU[] AU { get; set; }
    public CA[] CA { get; set; }
    public DE[] DE { get; set; }
    public FR[] FR { get; set; }
    public GB[] GB { get; set; }
    public IN[] IN { get; set; }
    public NZ[] NZ { get; set; }
    public US[] US { get; set; }
  }

  public class Translations
  {
    public Translation[] translations { get; set; }
  }
}
