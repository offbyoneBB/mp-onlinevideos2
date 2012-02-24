using System;

namespace Vlc.DotNet.Core.Interops
{
    public sealed class LibVlcMedia
    {
        public LibVlcFunction<Signatures.LibVlc.Media.NewFromLocation> NewInstanceFromLocation { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Media.NewFromPath> NewInstanceFromPath { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Media.NewFromFileDescriptor> NewInstanceFromFileDescriptor { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Media.NewEmpty> NewInstanceEmpty { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Media.AddOption> AddOption { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Media.AddOptionFlag> AddOptionFlag { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Media.RetainInstance> RetainInstance { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Media.ReleaseInstance> ReleaseInstance { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Media.GetMrl> GetMrl { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Media.Duplicate> Duplicate { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Media.GetMetadata> GetMetadata { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Media.SetMetadata> SetMetadata { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Media.SaveMetadatas> SaveMetadatas { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Media.GetState> GetState { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Media.GetStats> GetStats { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Media.GetSubItems> GetSubItems { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Media.EventManager> EventManager { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Media.GetDuration> GetDuration { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Media.Parse> Parse { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Media.ParseAsync> ParseAsync { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Media.IsParsed> IsParsed { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Media.SetUserData> SetUserData { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Media.GetUserData> GetUserData { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.Media.GetTrackInfo> GetTrackInfo { get; private set; }

        internal LibVlcMedia(IntPtr libVlcDllHandle, Version vlcVersion)
        {
            NewInstanceFromLocation = new LibVlcFunction<Signatures.LibVlc.Media.NewFromLocation>(libVlcDllHandle, vlcVersion);
            NewInstanceFromPath = new LibVlcFunction<Signatures.LibVlc.Media.NewFromPath>(libVlcDllHandle, vlcVersion);
            NewInstanceFromFileDescriptor = new LibVlcFunction<Signatures.LibVlc.Media.NewFromFileDescriptor>(libVlcDllHandle, vlcVersion);
            NewInstanceEmpty = new LibVlcFunction<Signatures.LibVlc.Media.NewEmpty>(libVlcDllHandle, vlcVersion);
            AddOption = new LibVlcFunction<Signatures.LibVlc.Media.AddOption>(libVlcDllHandle, vlcVersion);
            AddOptionFlag = new LibVlcFunction<Signatures.LibVlc.Media.AddOptionFlag>(libVlcDllHandle, vlcVersion);
            RetainInstance = new LibVlcFunction<Signatures.LibVlc.Media.RetainInstance>(libVlcDllHandle, vlcVersion);
            ReleaseInstance = new LibVlcFunction<Signatures.LibVlc.Media.ReleaseInstance>(libVlcDllHandle, vlcVersion);
            GetMrl = new LibVlcFunction<Signatures.LibVlc.Media.GetMrl>(libVlcDllHandle, vlcVersion);
            Duplicate = new LibVlcFunction<Signatures.LibVlc.Media.Duplicate>(libVlcDllHandle, vlcVersion);
            GetMetadata = new LibVlcFunction<Signatures.LibVlc.Media.GetMetadata>(libVlcDllHandle, vlcVersion);
            SetMetadata = new LibVlcFunction<Signatures.LibVlc.Media.SetMetadata>(libVlcDllHandle, vlcVersion);
            SaveMetadatas = new LibVlcFunction<Signatures.LibVlc.Media.SaveMetadatas>(libVlcDllHandle, vlcVersion);
            GetState = new LibVlcFunction<Signatures.LibVlc.Media.GetState>(libVlcDllHandle, vlcVersion);
            GetStats = new LibVlcFunction<Signatures.LibVlc.Media.GetStats>(libVlcDllHandle, vlcVersion);
            GetSubItems = new LibVlcFunction<Signatures.LibVlc.Media.GetSubItems>(libVlcDllHandle, vlcVersion);
            EventManager = new LibVlcFunction<Signatures.LibVlc.Media.EventManager>(libVlcDllHandle, vlcVersion);
            GetDuration = new LibVlcFunction<Signatures.LibVlc.Media.GetDuration>(libVlcDllHandle, vlcVersion);
            Parse = new LibVlcFunction<Signatures.LibVlc.Media.Parse>(libVlcDllHandle, vlcVersion);
            ParseAsync = new LibVlcFunction<Signatures.LibVlc.Media.ParseAsync>(libVlcDllHandle, vlcVersion);
            IsParsed = new LibVlcFunction<Signatures.LibVlc.Media.IsParsed>(libVlcDllHandle, vlcVersion);
            SetUserData = new LibVlcFunction<Signatures.LibVlc.Media.SetUserData>(libVlcDllHandle, vlcVersion);
            GetUserData = new LibVlcFunction<Signatures.LibVlc.Media.GetUserData>(libVlcDllHandle, vlcVersion);
            GetTrackInfo = new LibVlcFunction<Signatures.LibVlc.Media.GetTrackInfo>(libVlcDllHandle, vlcVersion);
        }

        #region IDisposable Members

        public void Dispose()
        {
            NewInstanceFromLocation = null;
            NewInstanceFromPath = null;
            NewInstanceFromFileDescriptor = null;
            NewInstanceEmpty = null;
            AddOption = null;
            AddOptionFlag = null;
            RetainInstance = null;
            ReleaseInstance = null;
            GetMrl = null;
            Duplicate = null;
            GetMetadata = null;
            SetMetadata = null;
            SaveMetadatas = null;
            GetState = null;
            GetStats = null;
            GetSubItems = null;
            EventManager = null;
            GetDuration = null;
            Parse = null;
            ParseAsync = null;
            IsParsed = null;
            SetUserData = null;
            GetUserData = null;
            GetTrackInfo = null;
        }

        #endregion
    }
}