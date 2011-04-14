namespace OnlineVideos.Sites.Pondman.IMDb.Json
{
    using Newtonsoft.Json;
    
    public class IMDbCertificate {

        [JsonProperty("attr")]
        public string ID { get; set; }

        [JsonProperty("certificate")]
        public string Name { get; set; }
    }
}
