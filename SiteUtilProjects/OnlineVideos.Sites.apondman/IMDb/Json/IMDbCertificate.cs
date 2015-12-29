using OnlineVideos._3rdParty.Newtonsoft.Json;

namespace OnlineVideos.Sites.Pondman.IMDb.Json
{
  public class IMDbCertificate {

        [JsonProperty("attr")]
        public string ID { get; set; }

        [JsonProperty("certificate")]
        public string Name { get; set; }
    }
}
