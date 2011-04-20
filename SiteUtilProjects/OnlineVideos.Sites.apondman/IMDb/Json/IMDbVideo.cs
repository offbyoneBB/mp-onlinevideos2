using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Pondman.IMDb.Json
{
    using Newtonsoft.Json;

    public class IMDbVideo
    {
        [JsonProperty("id")]
        public string ID { get; set; }
        
        [JsonProperty("@type")]
        public string Type { get; set; }
        
        [JsonProperty("content_type")]
        public string ContentType { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("duration_seconds")]
        public int Duration { get; set; }

        [JsonProperty("encodings")]
        public Dictionary<string, IMDbVideoFormat> Encodings { get; set; }

        [JsonProperty("slates")]
        public List<IMDbImage> Slates { get; set; }

    }
}
