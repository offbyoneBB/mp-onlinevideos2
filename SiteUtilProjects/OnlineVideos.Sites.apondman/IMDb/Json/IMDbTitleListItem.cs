using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Pondman.IMDb.Json
{
    using Newtonsoft.Json;
    
    public class IMDbTitleListItem : IMDbTitle
    {
        [JsonProperty("principals")]
        public IMDbPrincipal[] Principals { get; set; }
    }
}
