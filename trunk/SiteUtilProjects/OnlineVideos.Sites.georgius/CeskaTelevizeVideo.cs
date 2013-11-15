using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.georgius
{
    public class CeskaTelevizeVideo
    {
        public CeskaTelevizeVideo()
        {
            this.Part = 0;
            this.Label = String.Empty;
            this.Url = String.Empty;
        }

        public int Part { get; set; }
        public String Label { get; set; }
        public String Url { get; set; }
    }
}
