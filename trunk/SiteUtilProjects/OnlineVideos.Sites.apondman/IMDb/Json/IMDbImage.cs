using System;
using Newtonsoft.Json;

namespace OnlineVideos.Sites.Pondman.IMDb.Json {
    
    public class IMDbImage {

        [JsonProperty("width")]
        public int Width { get; set;}

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

}
