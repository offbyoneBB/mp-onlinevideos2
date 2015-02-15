using System;
using System.Text.RegularExpressions;

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

    public enum VideoKind { Other, TvSeries, Movie, MovieTrailer, GameTrailer, MusicVideo, News }

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
        public Match Regex
        {
            set { FillFromRegex(value); }
        }

        private void FillFromRegex(Match trackingInfoMatch)
        {
            if (trackingInfoMatch != null && trackingInfoMatch.Success)
            {
                System.Text.RegularExpressions.Group grp;

                if ((grp = trackingInfoMatch.Groups["VideoKind"]).Success)
                    try
                    {
                        VideoKind = (VideoKind)Enum.Parse(typeof(VideoKind), grp.Value);
                    }
                    catch { };

                if ((grp = trackingInfoMatch.Groups["Title"]).Success)
                    Title = grp.Value;

                uint? v;
                if ((v = getuintFomGroup(trackingInfoMatch.Groups["Season"])).HasValue)
                    Season = v.Value;
                if ((v = getuintFomGroup(trackingInfoMatch.Groups["Episode"])).HasValue)
                    Episode = v.Value;
                if ((v = getuintFomGroup(trackingInfoMatch.Groups["Year"])).HasValue)
                    Year = v.Value;

                if ((grp = trackingInfoMatch.Groups["ID_IMDB"]).Success)
                    ID_IMDB = grp.Value;

                if ((grp = trackingInfoMatch.Groups["ID_TMDB"]).Success)
                    ID_IMDB = grp.Value;

                if ((grp = trackingInfoMatch.Groups["ID_TVDB"]).Success)
                    ID_IMDB = grp.Value;

            }
        }

        private static uint? getuintFomGroup(System.Text.RegularExpressions.Group grp)
        {
            int value;
            if (grp.Success && int.TryParse(grp.Value, out value))
                return (uint)value;
            return null;
        }


    }
}
