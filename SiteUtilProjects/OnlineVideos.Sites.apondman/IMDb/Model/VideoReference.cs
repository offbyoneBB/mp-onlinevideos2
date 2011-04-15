using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Pondman.IMDb.Model
{
    public class VideoReference : Reference, IVideoDetails
    {
        public string Title { get; set; }

        public string Description { get; set; }

        public string Image { get; set; }

        public TimeSpan Duration { get; set; }

        public VideoDetails GetDetails()
        {
            VideoDetails details = IMDbAPI.GetVideo(this.session, this.ID);
            if (details.Image == null)
            {
                details.Image = this.Image;
            }

            return details;
        }

        #region IVideoDetails Members

        public virtual Dictionary<string, string> GetExtendedProperties()
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add("Title", Title);
            properties.Add("Description", Description);
            properties.Add("Duration", Duration.ToString());
            return properties;
        }

        #endregion
    }
}
