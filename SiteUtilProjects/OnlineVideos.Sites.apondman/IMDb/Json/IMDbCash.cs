using OnlineVideos._3rdParty.Newtonsoft.Json;

namespace OnlineVideos.Sites.Pondman.IMDb.Json
{
  public class IMDbCash
    {
        [JsonProperty("amount")]
        public int Amount { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }
    }
}
