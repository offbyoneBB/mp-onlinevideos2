using System;

namespace Vlc.DotNet.Core
{
    /// <summary>
    /// Interface for VlcControl
    /// </summary>
    public interface IVlcControl : IDisposable
    {
        /// <summary>
        /// Stop media player
        /// </summary>
        void Stop();
    }
}