using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos._3rdParty.Newtonsoft.Json;

namespace OnlineVideos.Sites.Pondman.IMDb.Json
{
  public class IMDbTitleListItem : IMDbTitle
    {
        [JsonProperty("principals")]
        public IMDbPrincipal[] Principals { get; set; }
    }
}
