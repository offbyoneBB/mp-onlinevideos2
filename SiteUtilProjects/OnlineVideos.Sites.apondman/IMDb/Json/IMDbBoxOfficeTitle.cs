﻿using OnlineVideos._3rdParty.Newtonsoft.Json;

namespace OnlineVideos.Sites.Pondman.IMDb.Json
{
  public class IMDbBoxOfficeTitle
    {
        [JsonProperty("rank")]
        public int Rank { get; set; }

        [JsonProperty("title")]
        public IMDbTitle Title { get; set; }

        [JsonProperty("gross")]
        public IMDbCash Gross { get; set; }

        [JsonProperty("weekend")]
        public IMDbCash Weekend { get; set; }
    }
}
