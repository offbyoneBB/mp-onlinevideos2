using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pondman.OnlineVideos.ITunes.Nodes;

namespace OnlineVideos.Sites.apondman.ITMovieTrailers
{
    /// <summary>
    /// Wrapper class to be able to publish the movie details to online videos
    /// </summary>
    public class MovieDetails : IVideoDetails
    {
        public MovieDetails(Movie movie)
        {
            this.movie = movie;
        }

        public Movie Movie
        {
            get
            {
                return this.movie;
            }
        } Movie movie;

        #region IVideoDetails Members

        public Dictionary<string, string> GetExtendedProperties()
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();

            properties.Add("Title", Movie.Title);
            properties.Add("Synopsis", Movie.Synopsis);
            properties.Add("Directors", Movie.Directors.ToCommaSeperatedString());
            properties.Add("Actors", Movie.Actors.ToCommaSeperatedString());
            properties.Add("Genres", Movie.Genres.ToCommaSeperatedString());
            properties.Add("Studio", Movie.Studio);
            properties.Add("Rating", Movie.Rating);
            string releaseDate = Movie.ReleaseDate != DateTime.MinValue ? Movie.ReleaseDate.ToShortDateString() : "Coming Soon";
            properties.Add("ReleaseDate", releaseDate);
            properties.Add("Year", Movie.Year.ToString());

            return properties;
        }

        #endregion
    }
}
