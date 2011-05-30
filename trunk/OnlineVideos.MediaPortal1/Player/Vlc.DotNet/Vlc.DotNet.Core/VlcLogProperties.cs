using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Vlc.DotNet.Core.Interops.Signatures.LibVlc;

namespace Vlc.DotNet.Core
{
    /// <summary>
    /// VlcLogProperties class
    /// </summary>
    public sealed class VlcLogProperties : IDisposable
    {
        /// <summary>
        /// VlcLogProperties constructor
        /// </summary>
        internal VlcLogProperties()
        {
            LogMessages = new VlcLogMessages();
        }

        /// <summary>
        /// Get / Set log verbosity
        /// </summary>
        public VlcLogVerbosities Verbosity
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.LoggingInterops != null &&
                    VlcContext.InteropManager.LoggingInterops.GetVerbosity.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.LibVlcHandle != IntPtr.Zero)
                {
                    return (VlcLogVerbosities)VlcContext.InteropManager.LoggingInterops.GetVerbosity.Invoke(VlcContext.HandleManager.LibVlcHandle);
                }
                return VlcLogVerbosities.None;
            }
            set
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.LoggingInterops != null &&
                    VlcContext.InteropManager.LoggingInterops.GetVerbosity.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.LibVlcHandle != IntPtr.Zero)
                {
                    VlcContext.InteropManager.LoggingInterops.SetVerbosity.Invoke(VlcContext.HandleManager.LibVlcHandle, (uint)value);
                }
            }
        }

        /// <summary>
        /// Retreive log messages
        /// </summary>
        public VlcLogMessages LogMessages { get; private set; }

        public void Dispose()
        {
            LogMessages.Dispose();
        }

        /// <summary>
        /// VlcLogMessages class
        /// </summary>
        public sealed class VlcLogMessages : IEnumerable<VlcLogMessage>, IDisposable
        {
            private readonly IntPtr myLogInstance;

            /// <summary>
            /// VlcLogMessages constructor
            /// </summary>
            internal VlcLogMessages()
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.LoggingInterops != null &&
                    VlcContext.InteropManager.LoggingInterops.Open.IsAvailable)
                {
                    myLogInstance = VlcContext.InteropManager.LoggingInterops.Open.Invoke(VlcContext.HandleManager.LibVlcHandle);
                }
            }

            /// <summary>
            /// Clear log messages
            /// </summary>
            public void Clear()
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.LoggingInterops != null &&
                    VlcContext.InteropManager.LoggingInterops.Clear.IsAvailable &&
                    myLogInstance != IntPtr.Zero)
                {
                    VlcContext.InteropManager.LoggingInterops.Clear.Invoke(myLogInstance);
                }
            }
            /// <summary>
            /// Count log messages
            /// </summary>
            public uint Count
            {
                get
                {
                    if (VlcContext.InteropManager != null &&
                        VlcContext.InteropManager.LoggingInterops != null &&
                        VlcContext.InteropManager.LoggingInterops.Count.IsAvailable &&
                        myLogInstance != IntPtr.Zero)
                    {
                        return VlcContext.InteropManager.LoggingInterops.Count.Invoke(myLogInstance);
                    }
                    return 0;
                }
            }
            /// <summary>
            /// Returns an enumerator that iterates VlcLogMessage.
            /// </summary>
            /// <returns>The current VlcLogMessage</returns>
            public IEnumerator<VlcLogMessage> GetEnumerator()
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.LoggingInterops != null &&
                    VlcContext.InteropManager.LoggingInterops.GetIterator.IsAvailable &&
                    myLogInstance != IntPtr.Zero &&
                    VlcContext.InteropManager.LoggingInterops.HasNext.IsAvailable &&
                    VlcContext.InteropManager.LoggingInterops.FreeInstance.IsAvailable)
                {

                    var iterator = VlcContext.InteropManager.LoggingInterops.GetIterator.Invoke(myLogInstance);

                    while (VlcContext.InteropManager.LoggingInterops.HasNext.Invoke(iterator))
                    {
                        var msg = new LogMessage();
                        VlcContext.InteropManager.LoggingInterops.Next.Invoke(iterator, ref msg);

                        var vlcLogMessage = new VlcLogMessage(
                            msg.i_severity,
                            Marshal.PtrToStringAnsi(msg.psz_type),
                            Marshal.PtrToStringAnsi(msg.psz_name),
                            Marshal.PtrToStringAnsi(msg.psz_header),
                            Marshal.PtrToStringAnsi(msg.psz_message));

                        yield return vlcLogMessage;
                    }

                    VlcContext.InteropManager.LoggingInterops.FreeInstance.Invoke(iterator);
                }
            }
            /// <summary>
            /// Returns an enumerator that iterates VlcLogMessage.
            /// </summary>
            /// <returns>The current VlcLogMessage</returns>
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.LoggingInterops != null &&
                    VlcContext.InteropManager.LoggingInterops.GetIterator.IsAvailable &&
                    myLogInstance != IntPtr.Zero &&
                    VlcContext.InteropManager.LoggingInterops.HasNext.IsAvailable &&
                    VlcContext.InteropManager.LoggingInterops.FreeInstance.IsAvailable)
                {

                    var iterator = VlcContext.InteropManager.LoggingInterops.GetIterator.Invoke(myLogInstance);

                    while (VlcContext.InteropManager.LoggingInterops.HasNext.Invoke(iterator))
                    {
                        var msg = new LogMessage();
                        VlcContext.InteropManager.LoggingInterops.Next.Invoke(iterator, ref msg);

                        var vlcLogMessage = new VlcLogMessage(
                            msg.i_severity,
                            Marshal.PtrToStringAnsi(msg.psz_type),
                            Marshal.PtrToStringAnsi(msg.psz_name),
                            Marshal.PtrToStringAnsi(msg.psz_header),
                            Marshal.PtrToStringAnsi(msg.psz_message));

                        yield return vlcLogMessage;
                    }

                    VlcContext.InteropManager.LoggingInterops.FreeInstance.Invoke(iterator);
                }

            }

            public void Dispose()
            {
                if (myLogInstance != IntPtr.Zero &&
                    VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.LoggingInterops != null &&
                    VlcContext.InteropManager.LoggingInterops.Close.IsAvailable)
                {
                    VlcContext.InteropManager.LoggingInterops.Close.Invoke(myLogInstance);
                }
            }
        }
    }
}
