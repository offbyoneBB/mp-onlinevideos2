using OnlineVideos._3rdParty.Newtonsoft.Json;

namespace OnlineVideos.Sites.Pondman.IMDb.Json
{
  public class IMDbStaff
    {

        [JsonProperty("as")]
        public string Alias { get; set; }

        [JsonProperty("name")]
        public IMDbName Name { get; set; }

        [JsonProperty("attr")]
        public string Attribute { get; set; }

    }
}
