namespace OnlineVideos.Sites.Pondman.IMDb.Json {
    
    using Newtonsoft.Json;

    public class IMDbRuntime
    {
        [JsonProperty("attr")]
        public string Attr { get; set; }

        [JsonProperty("time")]
        public int Time { get; set; }
    }
}
