using System;

namespace Vlc.DotNet.Core
{
    public sealed class MediaNotAttachedToVlcControlException : Exception
    {
        internal MediaNotAttachedToVlcControlException()
            : base("Media is not aatached to VlcControl.")
        {
        }
    }
}