using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos._3rdParty.Newtonsoft.Json;

namespace OnlineVideos.Sites.Pondman.IMDb.Json
{
  public class IMDbVideoFormat
    {
        [JsonProperty("format")]
        public string Format { get; set; }

        [JsonProperty("url")]
        public string URL { get; set; }
    }
}
