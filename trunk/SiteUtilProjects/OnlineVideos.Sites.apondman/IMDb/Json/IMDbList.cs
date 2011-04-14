namespace OnlineVideos.Sites.Pondman.IMDb.Json
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;

    public class IMDbList
    {
        [JsonProperty("label")]
        public string Label { get; set; }
    }

    public class IMDbChartList : IMDbList
    {
        [JsonProperty("list")]
        public IMDbTitle[] Titles { get; set; }
    }

    public class IMDbReleaseList : IMDbList
    {
        [JsonProperty("list")]
        public IMDbTitleListItem[] Titles { get; set; }

        [JsonProperty("token")]
        public DateTime ReleaseDate { get; set; }
    }

    public class IMDbTitleList : IMDbList
    {
        [JsonProperty("list")]
        public IMDbTitleListItem[] Titles { get; set; }
    }

    public class IMDbNameList : IMDbList
    {
        [JsonProperty("list")]
        public IMDbNameListItem[] Names { get; set; }
    }

}
