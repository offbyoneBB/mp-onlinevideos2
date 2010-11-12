using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pondman.Metadata.ITunes.MovieTrailers;

namespace OnlineVideos.Sites.apondman.ITMovieTrailers
{
    /// <summary>
    /// Inherited class to be able to publish the movie details to online videos
    /// </summary>
    public class VideoDetails : ITVideo, IVideoDetails
    {
        public VideoDetails(string uri) : base(uri) { }

        #region IVideoDetails Members

        public Dictionary<string, string> GetExtendedProperties()
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add("Date", Published != DateTime.MinValue ? Published.ToShortDateString() : "N/A");
            return properties;
        }

        #endregion

        /// <summary>
        /// Creates a VideoDetails object from another object
        /// </summary>
        /// <param name="vid">ITVideo object instance</param>
        /// <returns>VideoDetails object instance</returns>
        public static VideoDetails Create(ITVideo vid)
        {
            try
            {
                VideoDetails result = new VideoDetails(vid.Uri.AbsoluteUri);
                result.Duration = vid.Duration;
                result.Published = vid.Published;
                result.State = vid.State;
                result.Title = vid.Title;
                result.Uri = vid.Uri;
                return result;
            }
            catch
            {
                return null;
            }
        }
    }
}
