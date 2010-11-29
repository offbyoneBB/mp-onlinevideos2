using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pondman.OnlineVideos.ITunes.Nodes;

namespace OnlineVideos.Sites.apondman.ITMovieTrailers
{
    /// <summary>
    /// Inherited class to be able to publish the movie details to online videos
    /// </summary>
    public class VideoDetails : Video, IVideoDetails
    {
        public VideoDetails(Video video)
        {
            this.video = video;
        }

        public Video Video
        {
            get
            {
                return this.video;
            }
        } Video video;
        
        
        #region IVideoDetails Members

        public Dictionary<string, string> GetExtendedProperties()
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add("Date", Published != DateTime.MinValue ? Published.ToShortDateString() : "N/A");
            return properties;
        }

        #endregion
    }
}
