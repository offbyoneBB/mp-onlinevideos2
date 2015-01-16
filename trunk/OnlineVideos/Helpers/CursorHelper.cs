using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace OnlineVideos.Helpers
{
    /// <summary>
    /// Helpers for the cursor
    /// </summary>
    public static class CursorHelper
    {
        [DllImport("gdi32")]
        public static extern uint GetPixel(IntPtr hDC, int XPos, int YPos);
        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("user32.dll",CharSet=CharSet.Auto, CallingConvention=CallingConvention.StdCall, EntryPoint="mouse_event")]
        public static extern void Mouse_Event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);   

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;   
        private const int MOUSEEVENTF_LEFTUP = 0x04;   
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;   
        private const int MOUSEEVENTF_RIGHTUP = 0x10;   

        /// <summary> 
        /// Gets the System.Drawing.Color from under the mouse cursor. 
        /// </summary> 
        /// <returns>The color value.</returns> 
        public static Color GetColourUnderCursor()
        {
            Color color = Color.Empty;
            IntPtr hdc = GetDC(IntPtr.Zero);
            uint pixel = GetPixel(hdc, Cursor.Position.X, Cursor.Position.Y);
            ReleaseDC(IntPtr.Zero, hdc);

            color = Color.FromArgb(
                (int)(pixel & 0x000000FF),
                (int)(pixel & 0x0000FF00) >> 8,
                (int)(pixel & 0x00FF0000) >> 16);
            return color;

        }

        /// <summary>
        /// Left mouse click
        /// </summary>
        public static void DoLeftMouseClick()  
        {     
            //Call the imported function with the cursor's current position      
            Mouse_Event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, Cursor.Position.X, Cursor.Position.Y, 0, 0);   
        }
    }
}
