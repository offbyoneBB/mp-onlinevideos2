using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites.DavidCalder
{
  public class TMDbVideoDetails : IVideoDetails
  {
    public TMDB.Movie TMDbDetails { get; set; } 

    #region IVideoDetails Members
    
    public Dictionary<string, string> GetExtendedProperties()
    {
      Dictionary<string, string> p = new Dictionary<string, string>();

      p.Add("Title", this.TMDbDetails.title);
      p.Add("Plot", this.TMDbDetails.overview);
      p.Add("Directors", this.TMDbDetails.credits.CrewList("Directing"));
      p.Add("Writers", this.TMDbDetails.credits.CrewList("Writing"));
      p.Add("Actors", this.TMDbDetails.credits.CastList());
      p.Add("Genres", this.TMDbDetails.GenresListAsString());
      p.Add("Poster", this.TMDbDetails.PosterPathFullUrl());
      //p.Add("Certificate", this.TMDbDetails.certifications.ToString());
      p.Add("Rating", this.TMDbDetails.vote_average.ToString());
      p.Add("Seasons", this.TMDbDetails.budget.ToString());
      p.Add("Tagline", this.TMDbDetails.tagline);
      p.Add("Description", this.TMDbDetails.overview);
      p.Add("BackdropPath", this.TMDbDetails.BackdropPathFullUrl());
      p.Add("ReleaseDate", this.TMDbDetails.ReleaseDateAsString());
      p.Add("RatingImagePath", this.TMDbDetails.VoteAverageAsImageUrl());
      return p;   
    }

    #endregion
  }
}
