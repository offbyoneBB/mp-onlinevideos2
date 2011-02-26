using System;

namespace Vlc.DotNet.Core
{
    public sealed class PlayerNotAttachedToVlcControlException : Exception
    {
        internal PlayerNotAttachedToVlcControlException()
            : base("Player is not aatached to VlcControl.")
        {
        }
    }
}