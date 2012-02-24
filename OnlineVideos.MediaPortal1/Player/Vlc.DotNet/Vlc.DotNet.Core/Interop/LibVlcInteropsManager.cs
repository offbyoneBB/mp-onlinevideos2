using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Vlc.DotNet.Core.Interops.Signatures.LibVlc;

#if SILVERLIGHT

#else
using System.ComponentModel;
#endif

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
        /// Initializes a new instance of the LibVlcInteropsManager class.
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

        //public LibVlcFunction<GetModuleDescriptionList> GetModuleDescriptionList { get; private set; }
        //public LibVlcFunction<ReleaseModuleDescription> ReleaseModule { get; private set; }

        public LibVlcAsynchronousEvents EventInterops { get; private set; }
        public LibVlcLogging LoggingInterops { get; private set; }
        public LibVlcMediaPlayer MediaPlayerInterops { get; private set; }
        public LibVlcMedia MediaInterops { get; private set; }
        public LibVlcMediaList MediaListInterops { get; set; }
        public LibVlcAudio AudioInterops { get; private set; }
        public LibVlcVideo VideoInterops { get; private set; }
        public LibVlcErrorHandling ErrorHandlingInterops { get; private set; }
        public LibVlcMediaListPlayer MediaListPlayerInterops { get; private set; } 

        public Version VlcVersion
        {
            get; private set;
        }

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
            if (MediaListPlayerInterops != null)
                MediaListPlayerInterops.Dispose();
            MediaListPlayerInterops = null;
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
            var match = reg.Match(IntPtrExtensions.ToStringAnsi(GetVersion.Invoke()));
            VlcVersion = new Version(match.Groups[0].Value);

            NewInstance = new LibVlcFunction<NewInstance>(myLibVlcDllHandle, VlcVersion);
            ReleaseInstance = new LibVlcFunction<ReleaseInstance>(myLibVlcDllHandle, VlcVersion);
            RetainInstance = new LibVlcFunction<RetainInstance>(myLibVlcDllHandle, VlcVersion);
            AddInterface = new LibVlcFunction<AddInterface>(myLibVlcDllHandle, VlcVersion);
            SetExitCallback = new LibVlcFunction<SetExitCallback>(myLibVlcDllHandle, VlcVersion);
            Wait = new LibVlcFunction<Wait>(myLibVlcDllHandle, VlcVersion);
            SetUserAgent = new LibVlcFunction<SetUserAgent>(myLibVlcDllHandle, VlcVersion);
            GetCompiler = new LibVlcFunction<GetCompiler>(myLibVlcDllHandle, VlcVersion);
            GetChangeSet = new LibVlcFunction<GetChangeSet>(myLibVlcDllHandle, VlcVersion);
            FreeMemory = new LibVlcFunction<FreeMemory>(myLibVlcDllHandle, VlcVersion);
            //GetModuleDescriptionList = new LibVlcFunction<GetModuleDescriptionList>(myLibVlcDllHandle, VlcVersion);
            //ReleaseModule = new LibVlcFunction<ReleaseModuleDescription>(myLibVlcDllHandle, VlcVersion);

            EventInterops = new LibVlcAsynchronousEvents(myLibVlcDllHandle, VlcVersion);
            MediaPlayerInterops = new LibVlcMediaPlayer(myLibVlcDllHandle, VlcVersion);
            MediaInterops = new LibVlcMedia(myLibVlcDllHandle, VlcVersion);
            MediaListInterops = new LibVlcMediaList(myLibVlcDllHandle, VlcVersion);
            AudioInterops = new LibVlcAudio(myLibVlcDllHandle, VlcVersion);
            VideoInterops = new LibVlcVideo(myLibVlcDllHandle, VlcVersion);
            LoggingInterops = new LibVlcLogging(myLibVlcDllHandle, VlcVersion);
            ErrorHandlingInterops = new LibVlcErrorHandling(myLibVlcDllHandle, VlcVersion);
            MediaListPlayerInterops = new LibVlcMediaListPlayer(myLibVlcDllHandle, VlcVersion);
        }
    }
}