using System;

namespace Vlc.DotNet.Core
{
    public class VlcEventArgs<T> : EventArgs
    {
        internal VlcEventArgs(T data)
        {
            Data = data;
        }

        public T Data { get; private set; }
    }
}