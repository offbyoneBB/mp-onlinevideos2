namespace OnlineVideos.Sites.Pondman.IMDb.Json
{
    using Newtonsoft.Json;

    public class IMDbRole {

        [JsonProperty("char")]
        public string Character {get; set;}

        [JsonProperty("name")]
        public IMDbName Name { get; set; }

    }
}
