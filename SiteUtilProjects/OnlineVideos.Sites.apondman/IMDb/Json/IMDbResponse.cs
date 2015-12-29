using System;
using OnlineVideos._3rdParty.Newtonsoft.Json;
using OnlineVideos._3rdParty.Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites.Pondman.IMDb.Json {
  public class IMDbResponse {

        [JsonProperty("data")]
        public JObject Data { get; set; }

        [JsonProperty("exp")]
        public int Expires { get; set; }
    }

    public class IMDbResponse<T>
    {
        [JsonProperty("data")]
        public virtual T Data { get; set; }

        [JsonProperty("exp")]
        public virtual int Expires { get; set; }
    }

    public class IMDbMobileResponse<T>
    {
        [JsonProperty("list")]
        public virtual T Data { get; set; }

        [JsonProperty("path")]
        public virtual string Path { get; set; }

        [JsonProperty("exp")]
        public virtual int Expires { get; set; }
    }

    public class IMDbRankedResponse<T> : IMDbResponse<IMDbList<IMDbRankedObject<T>>>
    {

    }


}
