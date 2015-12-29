using OnlineVideos._3rdParty.Newtonsoft.Json;

namespace OnlineVideos.Sites.Pondman.IMDb.Json
{
  public class IMDbRole {

        [JsonProperty("char")]
        public string Character {get; set;}

        [JsonProperty("name")]
        public IMDbName Name { get; set; }

    }
}
