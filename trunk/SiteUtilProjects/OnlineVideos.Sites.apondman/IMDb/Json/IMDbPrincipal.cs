namespace OnlineVideos.Sites.Pondman.IMDb.Json
{
    using Newtonsoft.Json;
    
    public class IMDbPrincipal
    {
        [JsonProperty("nconst")]
        public string NConst { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
