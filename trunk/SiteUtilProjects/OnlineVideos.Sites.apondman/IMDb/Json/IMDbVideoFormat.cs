using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Pondman.IMDb.Json
{
    using Newtonsoft.Json;   
    
    public class IMDbVideoFormat
    {
        [JsonProperty("format")]
        public string Format { get; set; }

        [JsonProperty("url")]
        public string URL { get; set; }
    }
}
