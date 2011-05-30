using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Vlc.DotNet.Core.Interops
{
    /// <summary>
    /// LibVlcFunction class
    /// </summary>
    /// <typeparam name="T">Function signature type</typeparam>
    public sealed class LibVlcFunction<T>
    {
        private T myDelegate;

        internal LibVlcFunction(IntPtr libVlcHandle)
            : this(libVlcHandle, (Version)null)
        {
        }

        internal LibVlcFunction(IntPtr libVlcHandle, Version currentVlcVersion)
        {
            IsAvailable = false;
            object[] attrs = typeof(T).GetCustomAttributes(typeof(LibVlcFunctionAttribute), false);
            if (attrs.Length != 1)
                throw new Exception("Can't find the VlcFunctionAttribute.");
            var attr = attrs[0] as LibVlcFunctionAttribute;
            if (attr == null)
                return;
            VlcFunctionName = attr.FunctionName;
            if ((attr.MinVersion == null || currentVlcVersion >= attr.MinVersion) && (attr.MaxVersion == null || currentVlcVersion < attr.MaxVersion))
            {
                CreateDelegate(libVlcHandle);
                IsAvailable = true;
            }
        }

        /// <summary>
        /// The function name in libvlc.
        /// </summary>
        public string VlcFunctionName { get; private set; }
        /// <summary>
        /// Invoke the method.
        /// </summary>
        public T Invoke
        {
            get
            {
                if (!IsAvailable)
                    throw new MissingMethodException(string.Format("The {0} function is not available for this version of libvlc.", VlcFunctionName));
                return myDelegate;
            }
        }
        /// <summary>
        /// Check if this method is available with this version of libvlc.
        /// </summary>
        public bool IsAvailable { get; private set; }

        private void CreateDelegate(IntPtr libVlcDllPointer)
        {
            try
            {
                IntPtr procAddress = Win32Interop.GetProcAddress(libVlcDllPointer, VlcFunctionName);
                if (procAddress == IntPtr.Zero)
                    throw new Win32Exception();
                Delegate delegateForFunctionPointer = Marshal.GetDelegateForFunctionPointer(procAddress, typeof(T));
                myDelegate = (T)Convert.ChangeType(delegateForFunctionPointer, typeof(T));
            }
            catch (Win32Exception e)
            {
                throw new MissingMethodException(String.Format("The address of the function {0} does not exist in libvlc library.", VlcFunctionName), e);
            }
        }
    }
}