using System;
using System.Collections.Generic;
using System.Linq;

namespace OnlineVideos.Sites.Pondman.IMDb.Model
{
    using HtmlAgilityPack;
    using Newtonsoft.Json;
    using OnlineVideos.Sites.Pondman.IMDb.Json;

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
            this.ReleaseDate = DateTime.MinValue;
        }

        public virtual DateTime ReleaseDate { get; internal set; }

        public virtual List<Character> Cast { get; internal set; }

        public virtual List<NameReference> Writers { get; internal set; }

        public virtual List<NameReference> Directors { get; internal set; }

        public virtual List<string> Genres { get; internal set; }

        public virtual List<string> Photos { get; internal set; }

        public virtual string Plot { get; internal set; }

        public virtual string Certificate { get; internal set; }

        internal virtual void FillFrom(IMDbTitleDetails dto)
        {
            FillFrom(dto as IMDbTitle);

            string dt;
            if (dto.ReleaseDate != null && dto.ReleaseDate.TryGetValue("normal", out dt))
            {
                ReleaseDate = DateTime.Parse(dt);
            }

            if (dto.Plot != null)
            {
                Plot = dto.Plot.Outline;
            }

            if (dto.Certificate != null)
            {
                Certificate = dto.Certificate.Name;
            }

            if (dto.CastSummary != null)
            {
                foreach (IMDbRole role in dto.CastSummary)
                {
                    Character c = new Character();
                    c.Name = role.Character;
                    c.Actor = GetName(role.Name);

                    Cast.Add(c);
                }
            }

            if (dto.DirectorsSummary != null)
            {
                foreach (IMDbStaff staff in dto.DirectorsSummary)
                {
                    NameReference n = GetName(staff.Name);
                    Directors.Add(n);
                }
            }

            if (dto.WritersSummary != null)
            {
                foreach (IMDbStaff staff in dto.WritersSummary)
                {
                    NameReference n = GetName(staff.Name);
                    Writers.Add(n);
                }
            }

            if (dto.Photos != null)
            {
                foreach (IMDbPhoto photo in dto.Photos)
                {
                    Photos.Add(photo.Image.Url);
                }
            }

            if (dto.Genres != null)
            {
                foreach (string genre in dto.Genres)
                {
                    Genres.Add(genre);
                }
            }
        }

        protected virtual NameReference GetName(IMDbName dto)
        {
            NameReference name = new NameReference();
            name.session = this.session;
            name.FillFrom(dto);

            return name;
        }

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
            
            string releaseDate = this.ReleaseDate != DateTime.MinValue ? this.ReleaseDate.ToShortDateString() : "Coming Soon";
            p.Add("ReleaseDate", releaseDate);

            return p;
        }

        #endregion
    }
}
