using System;
using Vlc.DotNet.Core.Interops.Signatures.LibVlc.AsynchronousEvents;

namespace Vlc.DotNet.Core.Interops
{
    /// <summary>
    /// LibVlcAsynchronousEvents class
    /// </summary>
    public sealed class LibVlcAsynchronousEvents : IDisposable
    {
        internal LibVlcAsynchronousEvents(IntPtr libVlcDllHandle, Version vlcVersion)
        {
            Attach = new LibVlcFunction<Attach>(libVlcDllHandle, vlcVersion);
            Detach = new LibVlcFunction<Detach>(libVlcDllHandle, vlcVersion);
            GetTypeName = new LibVlcFunction<GetTypeName>(libVlcDllHandle, vlcVersion);
        }

        public LibVlcFunction<Attach> Attach { get; private set; }
        public LibVlcFunction<Detach> Detach { get; private set; }
        public LibVlcFunction<GetTypeName> GetTypeName { get; private set; }

        #region IDisposable Members

        public void Dispose()
        {
            Attach = null;
            Detach = null;
            GetTypeName = null;
        }

        #endregion
    }
}