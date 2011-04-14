using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace OnlineVideos.Sites.Pondman.IMDb.Json {
    
    using Newtonsoft.Json;

    public class IMDbName : IMDbPrincipal
    {
        
        [JsonProperty("image")]
        public IMDbImage Image {
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
