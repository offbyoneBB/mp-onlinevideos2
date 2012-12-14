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

        #region internal members

        /*
        internal virtual void FillFrom(IMDbVideo dto)
        {
            this.ID = dto.ID;

            // todo: use the actual title but filter the movie name etc..
            this.Title = "Main Trailer";

            this.Description = dto.Description;
            this.Duration = new TimeSpan(0, 0, dto.Duration);

            if (dto.Slates != null)
            {
                // take first image only
                this.Image = dto.Slates[0].Url;
            }
        }
        */

        #endregion

        #region IVideoDetails Members

        public override Dictionary<string, string> GetExtendedProperties()
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add("Title", this.Title);
            properties.Add("Description", this.Description);
            properties.Add("Duration", this.Duration.ToString());
            return properties;
        }

        #endregion
    }
}
