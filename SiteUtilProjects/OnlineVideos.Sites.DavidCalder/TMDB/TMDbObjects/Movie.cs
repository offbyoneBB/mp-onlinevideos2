using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.DavidCalder.TMDB
{

  public class Movie 
  {
    public bool adult { get; set; }
    public string backdrop_path { get; set; }
    public object belongs_to_collection { get; set; }
    public int budget { get; set; }
    public Genre[] genres { get; set; }
    public string homepage { get; set; }
    public string id { get; set; }
    public string imdb_id { get; set; }
    public string original_title { get; set; }
    public string overview { get; set; }
    public string popularity { get; set; }
    public string poster_path { get; set; }
    public ProductionCompanies[] production_companies { get; set; }
    public ProductionCountries[] production_countries { get; set; }
    public string release_date { get; set; }
    public int revenue { get; set; }
    public string runtime { get; set; }
    public SpokenLanguages[] spoken_languages { get; set; }
    public string status { get; set; }
    public string tagline { get; set; }
    public string title { get; set; }
    public double vote_average { get; set; }
    public int vote_count { get; set; }
    public Releases releases { get; set; }
    public Videos videos { get; set; }
    public Credits credits { get; set; }
    public Images images { get; set; }
    public Translations translations { get; set; }
    public Certifications certifications { get; set; }

    public string PosterPathFullUrl() 
    {
      return string.Format("http://image.tmdb.org/t/p/w1000/{0}", poster_path);   
    }

    public string BackdropPathFullUrl()
    {
      return string.Format("http://image.tmdb.org/t/p/w1280/{0}", backdrop_path);
    } 

    public string GenresListAsString()
    {
      List<string> list = new List<string>();
      foreach (Genre genre in genres)
      {
        list.Add(genre.name);
      }
      return string.Join(", ", list.ToArray());
    }

    public string CompanyListAsString()
    {
      List<string> list = new List<string>();
      foreach (ProductionCompanies company in production_companies)
      {
        list.Add(company.name);
      }
      return string.Join(", ", list.ToArray());
    }

    public string CountriesListAsString()
    {
      List<string> list = new List<string>();
      foreach (ProductionCountries countries in production_countries)
      {
        list.Add(countries.name);
      }
      return string.Join(", ", list.ToArray());
    }

    public string LanguagesListAsString()
    {
      List<string> list = new List<string>();
      foreach (SpokenLanguages languages in spoken_languages)
      {
        list.Add(languages.name);
      }
      return string.Join(", ", list.ToArray());
    }

    public string ReleaseDateAsString()
    {
      string date = string.Empty;
      if (!string.IsNullOrEmpty(this.release_date))
      {
        if (Convert.ToDateTime(this.release_date).Date <= DateTime.Now.Date)
        {
          date = Convert.ToDateTime(this.release_date).ToShortDateString();
        }
        else date = "Coming Soon..";
      }
      else date = "Coming Soon..";

      return date;
    }

    public string VoteAverageAsImageUrl()
    {
      double x = TMDB.TMDB_Movie25.MovieInfo.vote_average;
      double f = x - Math.Truncate(x); string i = "0";
      if (f < 0.25) x = Math.Floor(x); else if (f > 0.25 && f < 0.75) { x = Math.Floor(x); i = "5"; } else if (f > 0.75) x = Math.Ceiling(x);
      string num = x.ToString() + i; if (num == "00") num = "";
      return string.Format(@"TVSeries\star{0}.png", num);
    }
  }
}
