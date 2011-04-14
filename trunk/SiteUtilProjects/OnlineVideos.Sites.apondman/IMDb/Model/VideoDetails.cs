using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Pondman.IMDb.Model
{
    public class VideoDetails : VideoReference
    {
        public VideoDetails()
            : base()
        {
            Files = new Dictionary<VideoFormat, string>();
        }

        public int Duration { get; set; }

        public Dictionary<VideoFormat, string> Files { get; set; }

        public override Dictionary<string, string> GetExtendedProperties()
        {
            Dictionary<string, string> properties = base.GetExtendedProperties();
            properties.Add("Duration", Duration.ToString());
            return properties;
        }
    }
}
