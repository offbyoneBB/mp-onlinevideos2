using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace OnlineVideos.Sites.Pondman.IMDb.Json
{
    public class IMDbPhoto
    {
        [JsonProperty("image")]
        public IMDbImage Image
        {
            get
            {
                if (this.image == null)
                {
                    this.Image = new IMDbImage();
                }

                return this.image;
            }
            set
            {
                this.image = value;
            }
        } private IMDbImage image;
    }
}
