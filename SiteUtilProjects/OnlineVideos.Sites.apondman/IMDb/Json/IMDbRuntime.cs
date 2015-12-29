using OnlineVideos._3rdParty.Newtonsoft.Json;

namespace OnlineVideos.Sites.Pondman.IMDb.Json {
  public class IMDbRuntime
    {
        [JsonProperty("attr")]
        public string Attr { get; set; }

        [JsonProperty("time")]
        public int Time { get; set; }
    }
}
