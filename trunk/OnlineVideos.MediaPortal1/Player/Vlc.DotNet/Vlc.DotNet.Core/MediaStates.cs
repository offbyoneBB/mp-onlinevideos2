namespace Vlc.DotNet.Core
{
    public enum MediaStates
    {
        NothingSpecial = 0,
        Opening,
        Buffering,
        Playing,
        Paused,
        Stopped,
        Ended,
        Error
    }
}