using System;
using Newtonsoft.Json;

namespace OnlineVideos.Sites.Pondman.IMDb.Json
{
    public class IMDbPlot {

        [JsonProperty("more")]
        public int More { get; set; }

        [JsonProperty("outline")]
        public string Outline { get; set; }
    }
}
