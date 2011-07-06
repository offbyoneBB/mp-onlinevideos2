using System;
using Vlc.DotNet.Core.Interops.Signatures.LibVlc.ErrorHandling;

namespace Vlc.DotNet.Core.Interops
{
    /// <summary>
    /// LibVlcErrorHandling class
    /// </summary>
    public sealed class LibVlcErrorHandling : IDisposable
    {
        public LibVlcFunction<ClearError> ClearError { get; private set; }
        public LibVlcFunction<GetErrorMessage> GetErrorMessage { get; private set; }

        public LibVlcErrorHandling(IntPtr libVlcDllHandle, Version vlcVersion)
        {
            GetErrorMessage = new LibVlcFunction<GetErrorMessage>(libVlcDllHandle, vlcVersion);
            ClearError = new LibVlcFunction<ClearError>(libVlcDllHandle, vlcVersion);
        }

        #region IDisposable Members

        public void Dispose()
        {
            GetErrorMessage = null;
            ClearError = null;
        }
        #endregion
    }
}