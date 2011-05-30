using System;

namespace Vlc.DotNet.Core.Interops
{
    public sealed class LibVlcLogging : IDisposable
    {
        public LibVlcFunction<Signatures.LibVlc.Logging.GetVerbosity> GetVerbosity { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Logging.SetVerbosity> SetVerbosity { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Logging.Open> Open { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Logging.Close> Close { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Logging.Count> Count { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Logging.Clear> Clear { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Logging.GetIterator> GetIterator { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Logging.HasNext> HasNext { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Logging.FreeInstance> FreeInstance { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Logging.Next> Next { get; private set; }

        internal LibVlcLogging(IntPtr myLibVlcDllHandle, Version vlcVersion)
        {
            GetVerbosity = new LibVlcFunction<Signatures.LibVlc.Logging.GetVerbosity>(myLibVlcDllHandle, vlcVersion);
            SetVerbosity = new LibVlcFunction<Signatures.LibVlc.Logging.SetVerbosity>(myLibVlcDllHandle, vlcVersion);
            Open = new LibVlcFunction<Signatures.LibVlc.Logging.Open>(myLibVlcDllHandle, vlcVersion);
            Close = new LibVlcFunction<Signatures.LibVlc.Logging.Close>(myLibVlcDllHandle, vlcVersion);
            Count = new LibVlcFunction<Signatures.LibVlc.Logging.Count>(myLibVlcDllHandle, vlcVersion);
            Clear = new LibVlcFunction<Signatures.LibVlc.Logging.Clear>(myLibVlcDllHandle, vlcVersion);
            GetIterator = new LibVlcFunction<Signatures.LibVlc.Logging.GetIterator>(myLibVlcDllHandle, vlcVersion);
            HasNext = new LibVlcFunction<Signatures.LibVlc.Logging.HasNext>(myLibVlcDllHandle, vlcVersion);
            FreeInstance = new LibVlcFunction<Signatures.LibVlc.Logging.FreeInstance>(myLibVlcDllHandle, vlcVersion);
            Next = new LibVlcFunction<Signatures.LibVlc.Logging.Next>(myLibVlcDllHandle, vlcVersion);
        }

        public void Dispose()
        {
            GetVerbosity = null;
            SetVerbosity = null;
            Open = null;
            Count = null;
            Close = null;
            Clear = null;
            GetIterator = null;
            HasNext = null;
            FreeInstance = null;
            Next = null;
        }
    }
}
