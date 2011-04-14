using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace OnlineVideos.Sites.Pondman.IMDb.Json
{
    using Newtonsoft.Json;
    
    public class IMDbTrailer
    {
        [JsonProperty("title")]
        public string TitleID { get; set; }
        
        [JsonProperty("title_title")]
        public string Title { get; set; }

        [JsonProperty("poster")]
        public string Image { get; set; }

        [JsonProperty("duration")]
        public string Duration { get; set; }

        [JsonProperty("duration_seconds")]
        public string DurationSeconds { get; set; }

        [JsonProperty("title_data")]
        public string TitleHTML { get; set; }

        [JsonProperty("popup_data")]
        public string PopupHTML { get; set; }

        [JsonProperty("video_title")]
        public string VideoTitle { get; set; }

        [JsonProperty("video")]
        public string VideoID { get; set; }

    }
}
