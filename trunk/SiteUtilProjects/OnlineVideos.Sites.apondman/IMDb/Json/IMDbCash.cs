namespace OnlineVideos.Sites.Pondman.IMDb.Json
{
    using Newtonsoft.Json;

    public class IMDbCash
    {
        [JsonProperty("amount")]
        public int Amount { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }
    }
}
