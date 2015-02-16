using System;

namespace OnlineVideos
{
    public class DetailVideoInfo : VideoInfo
    {
        /// <summary>
        /// Used instead of the <see cref="Title"/> for the videos retrieved by <see cref="Sites.IChoice.GetVideoChoices"/> as display label in the UI<br/>
        /// You should set the <see cref="Title"/> to a fully identifying string for the video as it is used for favorites and downloads.
        /// </summary>
        public string Title2 { get; set; }

        public DetailVideoInfo() : base()
        {
            Title2 = string.Empty;
        }

        /// <summary>Creates a new instance and copies all data from the given video.</summary>
        /// <param name="video">The video to copy all data from</param>
        public DetailVideoInfo(VideoInfo video) : base()
        {
            Description = video.Description;
            Thumb = video.Thumb;
            ThumbnailImage = video.ThumbnailImage;
            ImageForcedAspectRatio = video.ImageForcedAspectRatio;
            Other = video.Other;

            Title = video.Title;
            VideoUrl = video.VideoUrl;
            Length = video.Length;
            Airdate = video.Airdate;
            StartTime = video.StartTime;
            SubtitleUrl = video.SubtitleUrl;
            SubtitleText = video.SubtitleText;
            PlaybackOptions = video.PlaybackOptions;
            HasDetails = video.HasDetails;

            Title2 = string.Empty;
        }
    }
}
