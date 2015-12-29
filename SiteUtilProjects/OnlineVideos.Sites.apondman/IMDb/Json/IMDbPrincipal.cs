using OnlineVideos._3rdParty.Newtonsoft.Json;

namespace OnlineVideos.Sites.Pondman.IMDb.Json
{
  public class IMDbPrincipal
    {
        [JsonProperty("nconst")]
        public string NConst { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
