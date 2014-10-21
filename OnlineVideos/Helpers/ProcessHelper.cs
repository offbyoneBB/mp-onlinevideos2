using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace OnlineVideos.Helpers
{
    /// <summary>
    /// Set window states of processes using win32
    /// </summary>
    public static class ProcessHelper
    {
        #region "Declarations for Minimizing Windows"

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, WINDOW_STATE state);
        [DllImport("User32", ExactSpelling = true)]
        private static extern int SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int cx, int cy, UInt32 uFlags);
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool SetActiveWindow(IntPtr hWnd);
        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool PostMessage(IntPtr hWnd, int Msg, System.Windows.Forms.Keys wParam, int lParam);

        public enum WINDOW_STATE
        {
            SW_HIDE = 0,
            SW_SHOWNORMAL = 1,
            SW_NORMAL = 1,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,
            SW_MAXIMIZE = 3,
            SW_SHOWNOACTIVATE = 4,
            SW_SHOW = 5,
            SW_MINIMIZE = 6,
            SW_SHOWMINNOACTIVE = 7,
            SW_SHOWNA = 8,
            SW_RESTORE = 9,
            SW_SHOWDEFAULT = 10,
            SW_FORCEMINIMIZE = 11,
            SW_MAX = 11
        }

        private const Int32 WM_KEYDOWN = 0x100;
        private const Int32 WM_KEYUP = 0x101;

        private const int HWND_TOPMOST = -1;
        private const UInt32 SWP_NOSIZE = 0x0001;
        private const UInt32 SWP_NOMOVE = 0x0002;
        #endregion

        #region Declarations for sending messages

        public const int WM_COPYDATA = 0x004a;

        [StructLayout(LayoutKind.Sequential)]
        public struct COPYDATASTRUCT
        {
            [MarshalAs(UnmanagedType.I4)]
            public int dwData;
            [MarshalAs(UnmanagedType.I4)]
            public int cbData;
            [MarshalAs(UnmanagedType.SysInt)]
            public IntPtr lpData;
        }

        [DllImport("User32.dll")]
        private static extern bool SendMessage(int hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        #endregion

        /// <summary>
        /// Send the specified key to the process
        /// </summary>
        /// <param name="mainWindowHandle"></param>
        /// <param name="key"></param>
        public static void SendKeyToProcess(string mainWindowTitle, System.Windows.Forms.Keys key)
        {
            var window = FindWindowByCaption(IntPtr.Zero, mainWindowTitle);
            SendKeyToProcess(window, key);
        }

        /// <summary>
        /// Send the specified key to the process
        /// </summary>
        /// <param name="mainWindowHandle"></param>
        /// <param name="key"></param>
        public static void SendKeyToProcess(IntPtr mainWindowHandle, System.Windows.Forms.Keys key)
        {
            PostMessage(mainWindowHandle, WM_KEYUP, key, 0);
        }

        /// <summary>
        /// Set the window to foreground - can't do this by process name if the window is hidden
        /// </summary>
        /// <param name="mainWindowHandle"></param>
        public static void SetForeground(IntPtr mainWindowHandle)
        {
            var result = SetForegroundWindow(mainWindowHandle);
            result = SetActiveWindow(mainWindowHandle);
        }

        /// <summary>
        /// Set the window to foreground - by name
        /// </summary>
        /// <param name="mainWindowTitle"></param>
        public static void SetForeground(string mainWindowTitle)
        {
            var window = FindWindowByCaption(IntPtr.Zero, mainWindowTitle);
            SetForegroundWindow(window);
            SetActiveWindow(window);
        }


        /// <summary>
        /// Restore a window and make it top most - can't do this by process name if the window is hidden
        /// </summary>
        /// <param name="mainWindowHandle"></param>
        public static void RestoreWindow(IntPtr mainWindowHandle)
        {
            SetWindowState(mainWindowHandle, ProcessHelper.WINDOW_STATE.SW_SHOW);
            SetWindowPos(mainWindowHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE);
        }

        /// <summary>
        /// Set the window state by the window handle
        /// </summary>
        /// <param name="mainWindowHandle"></param>
        /// <param name="state"></param>
        private static void SetWindowState(IntPtr mainWindowHandle, WINDOW_STATE state)
        {
            ShowWindow(mainWindowHandle, state);
        }

        /// <summary>
        /// Set the window state of the named process
        /// </summary>
        /// <param name="processName"></param>
        /// <param name="state"></param>
        public static IntPtr SetWindowState(string processName, WINDOW_STATE state)
        {
            var process = Process.GetProcessesByName(processName).FirstOrDefault();
            if (process != null)
            {
                SetWindowState(process.MainWindowHandle, state);
                return process.MainWindowHandle;
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Send a string to the specified window
        /// </summary>
        /// <param name="windowHandle"></param>
        /// <param name="stringToSend"></param>
        public static void SendStringToApplication(int windowHandle, string stringToSend)
        {
            IntPtr lpData = Marshal.StringToHGlobalUni(stringToSend);

            COPYDATASTRUCT data = new COPYDATASTRUCT();
            data.dwData = 0;
            data.cbData = stringToSend.Length * 2;
            data.lpData = lpData;

            IntPtr lpStruct = Marshal.AllocHGlobal(
                Marshal.SizeOf(data));

            Marshal.StructureToPtr(data, lpStruct, false);

            SendMessage(windowHandle, WM_COPYDATA, IntPtr.Zero, lpStruct);
        }

        /// <summary>
        /// Get the string from the WndProc message
        /// </summary>
        /// <param name="msg"></param>
        unsafe public static string ReadStringFromMessage(Message message)
        {
            if (message.Msg == WM_COPYDATA)
            {
                COPYDATASTRUCT data = (COPYDATASTRUCT)message.GetLParam(typeof(COPYDATASTRUCT));

                string str = new string((char*)(data.lpData), 0, data.cbData / 2);

                return str;
            }
            return string.Empty;
        }
    }
}
