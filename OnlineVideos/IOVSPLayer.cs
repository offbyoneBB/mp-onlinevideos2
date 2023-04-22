namespace OnlineVideos
{
    enum PlayState { Init, Playing, Paused, Ended };

    public interface IOVSPLayer
    {
        bool GoFullscreen { get; set; }
        string SubtitleFile { get; set; }
        string PlaybackUrl { get; } // hack to get around the MP 1.3 Alpha bug with non http URLs
    }
}
