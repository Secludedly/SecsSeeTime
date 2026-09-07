using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SecSeeTime.Helpers
{
    /// <summary>
    /// Window plumbing that WPF does not expose directly.
    /// </summary>
    public static class WindowHelper
    {
        /*
         * Windows refuses SetForegroundWindow to a process that does not
         * own the current foreground window. Briefly attaching to that
         * window's input queue is the documented way for an application
         * with a legitimate reason -- an alarm firing -- to raise itself.
         */

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(
            IntPtr hWnd,
            IntPtr processId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AttachThreadInput(
            uint attachTo,
            uint attachFrom,
            bool attach);


        /// <summary>
        /// Brings a window to the front and gives it keyboard focus,
        /// even when another application currently owns the foreground.
        /// Never throws: failing to raise the window must not take the
        /// alarm down with it.
        /// </summary>
        public static void ForceToForeground(Window window)
        {
            if (window == null)
                return;

            try
            {
                if (window.WindowState == WindowState.Minimized)
                    window.WindowState = WindowState.Normal;

                window.Show();
                window.Activate();
                window.Topmost = true;
                window.Focus();

                IntPtr handle =
                    new WindowInteropHelper(window).Handle;

                if (handle == IntPtr.Zero)
                    return;

                IntPtr foreground = GetForegroundWindow();

                if (foreground == handle)
                    return;

                uint foregroundThread =
                    GetWindowThreadProcessId(foreground, IntPtr.Zero);

                uint currentThread = GetCurrentThreadId();

                if (foregroundThread == 0 ||
                    foregroundThread == currentThread)
                {
                    SetForegroundWindow(handle);
                    return;
                }

                if (AttachThreadInput(currentThread, foregroundThread, true))
                {
                    try
                    {
                        SetForegroundWindow(handle);
                    }
                    finally
                    {
                        AttachThreadInput(currentThread, foregroundThread, false);
                    }
                }
                else
                {
                    SetForegroundWindow(handle);
                }
            }
            catch
            {
                // A window that will not come forward is a cosmetic
                // problem. The alarm is still sounding.
            }
        }
    }
}
