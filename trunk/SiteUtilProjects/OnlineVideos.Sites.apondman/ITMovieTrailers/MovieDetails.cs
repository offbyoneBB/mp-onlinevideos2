using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pondman.Metadata.ITunes.MovieTrailers;

namespace OnlineVideos.Sites.apondman.ITMovieTrailers
{
    /// <summary>
    /// Wrapper class to be able to publish the movie details to online videos
    /// </summary>
    public class MovieDetails : IVideoDetails
    {

        public MovieDetails(ITMovie movie)
        {
            _movie = movie;
        }

        public ITMovie Movie
        {
            get
            {
                return _movie;
            }
        } ITMovie _movie;

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
