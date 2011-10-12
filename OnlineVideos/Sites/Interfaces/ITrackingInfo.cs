using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos
{
    public interface ITrackingInfo
    {
        VideoKind VideoKind { get; set; }
        string Title { get; set; }
        uint Season { get; set; }
        uint Episode { get; set; }
        uint Year { get; set; }
        string ID_IMDB { get; set; }
        string ID_TMDB { get; set; }
        string ID_TVDB { get; set; }
    }

	[Serializable]
    public class TrackingInfo : ITrackingInfo
    {
        public VideoKind VideoKind { get; set; }
        public string Title { get; set; }
        public uint Season { get; set; }
        public uint Episode { get; set; }
        public uint Year { get; set; }
        public string ID_IMDB { get; set; }
        public string ID_TMDB { get; set; }
        public string ID_TVDB { get; set; }
    }
}
