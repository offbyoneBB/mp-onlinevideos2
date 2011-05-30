using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Vlc.DotNet.Core.Interops.Signatures.LibVlc;

namespace Vlc.DotNet.Core.Interops
{
    /// <summary>
    /// LibVlcInteropsManager class
    /// </summary>
    public sealed class LibVlcInteropsManager : IDisposable
    {
        private IntPtr myLibVlcCoreDllHandle;
        private IntPtr myLibVlcDllHandle;

        /// <summary>
        /// LibVlcInteropsManager contructor
        /// </summary>
        /// <param name="libVlcDllsDirectory">The path to libvlc.dll and libvlccore.dll</param>
        public LibVlcInteropsManager(string libVlcDllsDirectory)
        {
            if (string.IsNullOrEmpty(libVlcDllsDirectory))
                throw new ArgumentNullException("libVlcDllsDirectory");

            InitVlcLib(libVlcDllsDirectory);
        }

        public LibVlcFunction<NewInstance> NewInstance { get; private set; }
        public LibVlcFunction<ReleaseInstance> ReleaseInstance { get; private set; }
        public LibVlcFunction<RetainInstance> RetainInstance { get; private set; }
        public LibVlcFunction<AddInterface> AddInterface { get; private set; }
        public LibVlcFunction<SetExitCallback> SetExitCallback { get; private set; }
        public LibVlcFunction<Wait> Wait { get; private set; }
        public LibVlcFunction<SetUserAgent> SetUserAgent { get; private set; }
        public LibVlcFunction<GetVersion> GetVersion { get; private set; }
        public LibVlcFunction<GetCompiler> GetCompiler { get; private set; }
        public LibVlcFunction<GetChangeSet> GetChangeSet { get; private set; }
        public LibVlcFunction<FreeMemory> FreeMemory { get; private set; }

        public LibVlcFunction<Vlc.DotNet.Core.Interops.Signatures.LibVlc.ErrorHandling.GetErrorMessage> GetErrorMessage { get; private set; }

        //public LibVlcFunction<GetModuleDescriptionList> GetModuleDescriptionList { get; private set; }
        //public LibVlcFunction<ReleaseModuleDescription> ReleaseModule { get; private set; }

        public LibVlcAsynchronousEvents EventInterops { get; private set; }
        public LibVlcLogging LoggingInterops { get; private set; }
        public LibVlcMediaPlayer MediaPlayerInterops { get; private set; }
        public LibVlcMedia MediaInterops { get; private set; }
        public LibVlcMediaList MediaListInterops { get; set; }
        public LibVlcAudio AudioInterops { get; private set; }
        public LibVlcVideo VideoInterops { get; private set; }

        #region IDisposable Members

        public void Dispose()
        {
            if (myLibVlcDllHandle != IntPtr.Zero)
            {
                Win32Interop.FreeLibrary(myLibVlcDllHandle);
                myLibVlcDllHandle = IntPtr.Zero;
            }
            if (myLibVlcCoreDllHandle != IntPtr.Zero)
            {
                Win32Interop.FreeLibrary(myLibVlcCoreDllHandle);
                myLibVlcCoreDllHandle = IntPtr.Zero;
            }
            NewInstance = null;
            ReleaseInstance = null;
            RetainInstance = null;
            AddInterface = null;
            SetExitCallback = null;
            Wait = null;
            SetUserAgent = null;
            GetVersion = null;
            GetCompiler = null;
            GetChangeSet = null;
            FreeMemory = null;
            //GetModuleDescriptionList = null;
            //ReleaseModule = null;

            if (EventInterops != null)
                EventInterops.Dispose();
            EventInterops = null;
            if (MediaPlayerInterops != null)
                MediaPlayerInterops.Dispose();
            MediaPlayerInterops = null;
            if (MediaInterops != null)
                MediaInterops.Dispose();
            MediaInterops = null;
            if (MediaListInterops != null)
                MediaListInterops.Dispose();
            MediaListInterops = null;
            if (AudioInterops != null)
                AudioInterops.Dispose();
            AudioInterops = null;
            if (VideoInterops != null)
                VideoInterops.Dispose();
            VideoInterops = null;
            if (LoggingInterops != null)
                LoggingInterops.Dispose();
            LoggingInterops = null;
        }

        #endregion

        private void InitVlcLib(string libVlcDllsDirectory)
        {
            if (!Directory.Exists(libVlcDllsDirectory))
                throw new DirectoryNotFoundException(string.Format("The directory : {0} not found.", libVlcDllsDirectory));

            string libVlcFilePath = Path.Combine(libVlcDllsDirectory, "libvlc.dll");
            if (!File.Exists(libVlcFilePath))
                throw new FileNotFoundException("Libvlc library not found in directory.");
            string libVlcCoreFilePath = Path.Combine(libVlcDllsDirectory, "libvlccore.dll");
            if (!File.Exists(libVlcCoreFilePath))
                throw new FileNotFoundException("Libvlccore library not found in directory.");

            myLibVlcCoreDllHandle = Win32Interop.LoadLibrary(libVlcCoreFilePath);
            if (myLibVlcCoreDllHandle == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            myLibVlcDllHandle = Win32Interop.LoadLibrary(libVlcFilePath);
            if (myLibVlcDllHandle == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            GetVersion = new LibVlcFunction<GetVersion>(myLibVlcDllHandle);

            var reg = new Regex("^[0-9.]*");
            var match = reg.Match(GetVersion.Invoke());
            var vlcVersion = new Version(match.Groups[0].Value); 
            
            NewInstance = new LibVlcFunction<NewInstance>(myLibVlcDllHandle, vlcVersion);
            ReleaseInstance = new LibVlcFunction<ReleaseInstance>(myLibVlcDllHandle, vlcVersion);
            RetainInstance = new LibVlcFunction<RetainInstance>(myLibVlcDllHandle, vlcVersion);
            AddInterface = new LibVlcFunction<AddInterface>(myLibVlcDllHandle, vlcVersion);
            SetExitCallback = new LibVlcFunction<SetExitCallback>(myLibVlcDllHandle, vlcVersion);
            Wait = new LibVlcFunction<Wait>(myLibVlcDllHandle, vlcVersion);
            SetUserAgent = new LibVlcFunction<SetUserAgent>(myLibVlcDllHandle, vlcVersion);
            GetCompiler = new LibVlcFunction<GetCompiler>(myLibVlcDllHandle, vlcVersion);
            GetChangeSet = new LibVlcFunction<GetChangeSet>(myLibVlcDllHandle, vlcVersion);
            FreeMemory = new LibVlcFunction<FreeMemory>(myLibVlcDllHandle, vlcVersion);
            //GetModuleDescriptionList = new LibVlcFunction<GetModuleDescriptionList>(myLibVlcDllHandle, vlcVersion);
            //ReleaseModule = new LibVlcFunction<ReleaseModuleDescription>(myLibVlcDllHandle, vlcVersion);

            GetErrorMessage = new LibVlcFunction<Signatures.LibVlc.ErrorHandling.GetErrorMessage>(myLibVlcDllHandle, vlcVersion);

            EventInterops = new LibVlcAsynchronousEvents(myLibVlcDllHandle, vlcVersion);
            MediaPlayerInterops = new LibVlcMediaPlayer(myLibVlcDllHandle, vlcVersion);
            MediaInterops = new LibVlcMedia(myLibVlcDllHandle, vlcVersion);
            MediaListInterops = new LibVlcMediaList(myLibVlcDllHandle, vlcVersion);
            AudioInterops = new LibVlcAudio(myLibVlcDllHandle, vlcVersion);
            VideoInterops = new LibVlcVideo(myLibVlcDllHandle, vlcVersion);
            LoggingInterops = new LibVlcLogging(myLibVlcDllHandle, vlcVersion);
        }
    }
}