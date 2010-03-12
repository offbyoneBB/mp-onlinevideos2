using System;

namespace OnlineVideos
{
    /// <summary>
    /// Exception that can be used by Utils to show explicit error messages to the user when methods executed in background fail.
    /// The <see cref="Message"/> of this <see cref="Exception"/> will be presented to the user.
    /// </summary>
    public class OnlineVideosException : Exception
    {
        public OnlineVideosException(string message) : base(message) { }
    }
}
