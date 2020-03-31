﻿namespace OnlineVideos.Sites.Interfaces
{
    /// <summary>
    /// This interface is implemented by site utils that use the WebDriver interface to automate browsers for playback.
    /// </summary>
    public interface IWebDriverSite
    {
        /// <summary>
        /// Starts the playback. If there is any authentication needed, it must be handled inside this method first.
        /// </summary>
        /// <param name="playbackUrl">Playback url</param>
        /// <returns><c>true</c> if successful</returns>
        bool StartPlayback(string playbackUrl);

        /// <summary>
        /// Handle forwarded keys or action inside the site util. Usually this affects arrow keys, enter, play, pause a.s.o.
        /// </summary>
        /// <param name="keyOrAction">Key</param>
        /// <returns><c>true</c> if successful</returns>
        bool HandleAction(string keyOrAction);

        /// <summary>
        /// Set playback window to fullscreen.
        /// </summary>
        /// <returns><c>true</c> if successful</returns>
        bool Fullscreen();

        /// <summary>
        /// Position the window to the given point and size.
        /// </summary>
        /// <returns><c>true</c> if successful</returns>
        bool SetWindowBoundaries(System.Drawing.Point position, System.Drawing.Size size);

        /// <summary>
        /// End playback and free all resources (like automation process).
        /// </summary>
        /// <returns><c>true</c> if successful</returns>
        bool ShutDown();
    }
}
