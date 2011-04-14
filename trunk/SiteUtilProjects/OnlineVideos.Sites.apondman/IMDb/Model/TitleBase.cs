using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Pondman.IMDb.Model
{
    using OnlineVideos.Sites.Pondman.IMDb.Json;

    public class TitleBase : Reference, IVideoDetails
    {
        public virtual TitleType Type { get; internal set; }

        public virtual string Title { get; internal set; }

        public virtual int Year { get; internal set; }

        public virtual string Image { get; internal set; }

        public virtual List<VideoReference> GetVideos()
        {
            return IMDbAPI.GetVideos(this.session, this.ID);
        }

        internal virtual void FillFrom(IMDbTitle dto) {
            
            ID = dto.TConst;
            Title = dto.Title;
            Year = dto.Year;

            if (dto.Image != null)
            {
                Image = dto.Image.Url;
            }

            switch (dto.Type)
            {
                case "feature":
                    Type = TitleType.Movie;
                    break;
                // todo: add other types
                default:
                    Type = TitleType.Unknown;
                    break;
            }
        }

        #region IVideoDetails Members

        public virtual Dictionary<string, string> GetExtendedProperties()
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();

            properties.Add("Title", this.Title);
            properties.Add("Year", this.Year.ToString());

            return properties;
        }

        #endregion
    }
}
