namespace OnlineVideos.Sites.Pondman.IMDb.Json
{
    using System;
    using Newtonsoft.Json;

    // todo: needs better name!
    public class IMDbTitleMobile
    {
        [JsonProperty("header")]
        public DateTime ReleaseDate { get; set; }
        
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("url")]
        public string URL { get; set; }

        [JsonProperty("detail")]
        public string Detail { get; set; }

        [JsonProperty("extra")]
        public string Extra { get; set; }

        [JsonProperty("placeholder")]
        public string Placeholder { get; set; }

        [JsonProperty("img")]
        public IMDbImage Image
        {
            get
            {
                if (this.image == null)
                {
                    this.Image = new IMDbImage();
                }

                return this.image;
            }
            set
            {
                this.image = value;
            }
        } private IMDbImage image;
    }
}
