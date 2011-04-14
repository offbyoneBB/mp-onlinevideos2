using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Pondman.IMDb.Model
{
    using OnlineVideos.Sites.Pondman.IMDb.Json;
    
    public class NameReference : Reference
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public virtual string Name { get; internal set; }

        /// <summary>
        /// Gets or sets a list of titles this name is known for.
        /// </summary>
        /// <value>The known for.</value>
        public virtual string KnownFor { get; internal set; }

        /// <summary>
        /// Gets or sets the image URL.
        /// </summary>
        /// <value>The image URL.</value>
        public virtual string Image { get; internal set; }

        #region Fill from JSON object

        internal void FillFrom(IMDbPrincipal dto)
        {
            ID = dto.NConst;
            Name = dto.Name;
            Image = string.Empty;
        }

        internal void FillFrom(IMDbName dto)
        {
            FillFrom(dto as IMDbPrincipal);
            Image = dto.Image.Url;
        }

        internal void FillFrom(IMDbNameListItem dto)
        {
            FillFrom(dto as IMDbName);
            KnownFor = dto.KnownFor;
        }

        #endregion
    }
}
