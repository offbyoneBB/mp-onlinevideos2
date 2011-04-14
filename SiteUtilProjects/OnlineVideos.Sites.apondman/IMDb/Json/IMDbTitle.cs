namespace OnlineVideos.Sites.Pondman.IMDb.Json
{
    using Newtonsoft.Json;

    public class IMDbTitle {

        [JsonProperty("tconst")]
        public string TConst { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("year")]
        public int Year { get; set; }

        [JsonProperty("image")]
        public IMDbImage Image {
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
