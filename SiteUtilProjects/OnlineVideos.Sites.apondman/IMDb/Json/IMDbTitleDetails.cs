﻿using System.Collections.Generic;
using System;
using OnlineVideos._3rdParty.Newtonsoft.Json;

namespace OnlineVideos.Sites.Pondman.IMDb.Json {
  public class IMDbTitleDetails : IMDbTitle {

        public IMDbTitleDetails()
        {
            this.Plot = new IMDbPlot();
            this.ReleaseDate = new Dictionary<string, string>();
        }

        [JsonProperty("cast_summary")]
        public List<IMDbRole> CastSummary { get; set; }

        [JsonProperty("directors_summary")]
        public List<IMDbStaff> DirectorsSummary { get; set; }

        [JsonProperty("writers_summary")]
        public List<IMDbStaff> WritersSummary { get; set; }

        [JsonProperty("genres")]
        public List<string> Genres { get; set; }

        // "more_cast", "trivia", "goofs", "quotes", "more_plot", "user_comments", "external_reviews", 
        // "parentalguide", "synopsis", "photos"
        [JsonProperty("has")]
        public List<string> has { get; set; }

        [JsonProperty("photos")]
        public List<IMDbPhoto> Photos { get; set; }

        [JsonProperty("plot")]
        public IMDbPlot Plot { get; set; }

        [JsonProperty("release_date")] 
        public Dictionary<string, string> ReleaseDate { get; set; }

        [JsonProperty("production_status")]
        public string ProductionStatus { get; set; }

        [JsonProperty("seasons")] 
        public List<string> Seasons { get; set; }

        [JsonProperty("runtime")]
        public IMDbRuntime Runtime { get; set; }

        [JsonProperty("tagline")]
        public string Tagline { get; set; }

        [JsonProperty("certificate")]
        public IMDbCertificate Certificate { get; set; }

        [JsonProperty("trailer")]
        public IMDbVideo Trailer { get; set;}

    }
}
