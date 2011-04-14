using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace OnlineVideos.Sites.Pondman.IMDb.Json
{
    public class IMDbNameListItem : IMDbName
    {
        [JsonProperty("known_for")]
        public string KnownFor { get; set; }
    }
}
