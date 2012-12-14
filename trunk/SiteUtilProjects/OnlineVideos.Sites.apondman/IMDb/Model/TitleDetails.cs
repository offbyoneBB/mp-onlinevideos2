using System;
using System.Collections.Generic;
using System.Linq;

namespace OnlineVideos.Sites.Pondman.IMDb.Model
{
    using HtmlAgilityPack;
    using Newtonsoft.Json;

    /// <summary>
    /// IMDb Title
    /// </summary>
    public class TitleDetails : TitleBase, IVideoDetails
    {
        public TitleDetails()
            : base()
        {
            this.Cast = new List<Character>();
            this.Writers = new List<NameReference>();
            this.Directors = new List<NameReference>();
            this.Genres = new List<string>();
            this.Photos = new List<string>();
            this.Seasons = new List<string>();
            this.ReleaseDate = DateTime.MinValue;
        }

        /// <summary>
        /// internal variable to store the main trailer id (if no trailer object)
        /// </summary>
        internal string trailer;
        
        public virtual DateTime ReleaseDate { get; internal set; }

        public virtual List<Character> Cast { get; internal set; }

        public virtual List<NameReference> Writers { get; internal set; }

        public virtual List<NameReference> Directors { get; internal set; }

        public virtual List<string> Genres { get; internal set; }

        public virtual List<string> Photos { get; internal set; }

        public virtual List<string> Seasons { get; internal set; }

        public virtual VideoDetails Trailer { get; internal set; }

        public virtual string ProductionStatus { get; internal set; }
        
        public virtual string Plot { get; internal set; }

        public virtual string Tagline { get; internal set; }

        public virtual string Certificate { get; internal set; }

        public override List<VideoReference> GetVideos()
        {
            List<VideoReference> list = IMDbAPI.GetVideos(this.session, this.ID);
            if (this.Trailer != null)
            {
                // replace the reference with a details version
                int i = list.FindIndex(x => x.ID == this.Trailer.ID);
                if (i > -1)
                {
                    list.RemoveAt(i);
                }

                // put the main trailer on top
                list.Insert(0, this.Trailer);
            }
            else if (this.trailer != null)
            {
                int i = list.FindIndex(x => x.ID == this.trailer);
                if (i > -1)
                {
                    // move the video on top and rename the title
                    VideoReference video = list[i];
                    video.Title = "Main Trailer";
                    list.RemoveAt(i);
                    list.Insert(0, video);
                }
            }

            return list;
        }

        #region Protected methods

        /*
        protected virtual NameReference GetName(IMDbName dto)
        {
            NameReference name = new NameReference();
            name.session = this.session;
            name.FillFrom(dto);

            return name;
        }
        */
        #endregion

        #region Internal methods

        /*
        internal virtual void FillFrom(IMDbTitleDetails dto)
        {
            FillFrom(dto as IMDbTitle);

            this.ProductionStatus = dto.ProductionStatus;
            this.Tagline = dto.Tagline;

            string dt;
            if (dto.ReleaseDate != null && dto.ReleaseDate.TryGetValue("normal", out dt))
            {
                DateTime value;
                if (DateTime.TryParse(dt, out value))
                {
                    this.ReleaseDate = value;
                }
            }

            if (dto.Plot != null)
            {
                this.Plot = dto.Plot.Outline;
            }

            if (dto.Certificate != null)
            {
                this.Certificate = dto.Certificate.Name;
            }

            if (dto.CastSummary != null)
            {
                foreach (IMDbRole role in dto.CastSummary)
                {
                    Character c = new Character();
                    c.Name = role.Character;
                    c.Actor = GetName(role.Name);

                    this.Cast.Add(c);
                }
            }

            if (dto.DirectorsSummary != null)
            {
                foreach (IMDbStaff staff in dto.DirectorsSummary)
                {
                    NameReference n = GetName(staff.Name);
                    this.Directors.Add(n);
                }
            }

            if (dto.WritersSummary != null)
            {
                foreach (IMDbStaff staff in dto.WritersSummary)
                {
                    NameReference n = GetName(staff.Name);
                    this.Writers.Add(n);
                }
            }

            if (dto.Photos != null)
            {
                foreach (IMDbPhoto photo in dto.Photos)
                {
                    this.Photos.Add(photo.Image.Url);
                }
            }

            if (dto.Genres != null)
            {
                foreach (string genre in dto.Genres)
                {
                    this.Genres.Add(genre);
                }
            }

            if (dto.Seasons != null)
            {
                foreach (string season in dto.Seasons)
                {
                    this.Seasons.Add(season);
                }
            }

            if (dto.Trailer != null)
            {
                VideoDetails trailer = new VideoDetails();
                trailer.session = this.session;
                trailer.FillFrom(dto.Trailer);

                this.Trailer = trailer;
            }
        }
        */

        #endregion

        #region IVideoDetails Members

        public override Dictionary<string, string> GetExtendedProperties()
        {
            Dictionary<string, string> p = base.GetExtendedProperties();

            p.Add("Plot", this.Plot);
            p.Add("Directors", this.Directors.Select(x => x.Name).ToList().ToCommaSeperatedString());
            p.Add("Writers", this.Writers.Select(x => x.Name).ToList().ToCommaSeperatedString());
            p.Add("Actors", this.Cast.Select(x => x.Actor.Name).ToList().ToCommaSeperatedString());
            p.Add("Genres", this.Genres.ToCommaSeperatedString());
            p.Add("Certificate", this.Certificate);
            p.Add("Seasons", this.Seasons.ToCommaSeperatedString());
            p.Add("Tagline", this.Tagline);
            
            string releaseDate = this.ReleaseDate != DateTime.MinValue ? this.ReleaseDate.ToShortDateString() : "Coming Soon";
            p.Add("ReleaseDate", releaseDate);

            return p;
        }

        #endregion
    }
}
