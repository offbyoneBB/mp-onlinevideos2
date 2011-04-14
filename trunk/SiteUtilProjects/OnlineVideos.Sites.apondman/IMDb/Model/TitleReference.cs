namespace OnlineVideos.Sites.Pondman.IMDb.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using OnlineVideos.Sites.Pondman.IMDb.Json;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Summarized version of a title
    /// </summary>
    public class TitleReference : TitleBase
    {
        public TitleReference()
            : base()
        {
            this.Principals = new List<NameReference>();
            this.ReleaseDate = DateTime.MinValue;
        }

        public virtual List<NameReference> Principals { get; internal set; }

        public virtual DateTime ReleaseDate { get; internal set; }
        
        public virtual TitleDetails GetDetails()
        {
            return IMDbAPI.GetTitle(this.session, this.ID);
        }

        internal virtual void FillFrom(IMDbTitleListItem dto)
        {
            FillFrom(dto as IMDbTitle);

            if (dto.Principals != null)
            {

                foreach (IMDbPrincipal person in dto.Principals)
                {
                    NameReference name = new NameReference();
                    name.session = this.session;
                    name.FillFrom(person);

                    Principals.Add(name);
                }
            }
        }

        internal virtual void FillFrom(IMDbTrailer dto)
        {
            this.ID = dto.TitleID;
            this.Title = dto.Title;
            this.Type = TitleType.Movie;
            this.Image = dto.Image;
        }

        public override Dictionary<string, string> GetExtendedProperties()
        {
            Dictionary<string, string> p = base.GetExtendedProperties();
            p.Add("Actors", this.Principals.Select(x => x.Name).ToList().ToCommaSeperatedString());
            string releaseDate = this.ReleaseDate != DateTime.MinValue ? this.ReleaseDate.ToShortDateString() : "Coming Soon";
            p.Add("ReleaseDate", releaseDate);

            return p;
        }
    }
}
