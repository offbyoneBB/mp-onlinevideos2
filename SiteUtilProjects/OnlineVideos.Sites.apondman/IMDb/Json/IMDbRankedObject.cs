using OnlineVideos._3rdParty.Newtonsoft.Json;

namespace OnlineVideos.Sites.Pondman.IMDb.Json
{
  public class IMDbRankedObject<T>
    {
        [JsonProperty("object")]
        public T Object { get; set; }

        [JsonProperty("prev")]
        public int PreviousRank { get; set; }

        [JsonProperty("rank")]
        public int CurrentRank { get; set; }
    }
}
