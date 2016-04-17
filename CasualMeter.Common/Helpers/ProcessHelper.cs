using System;
using CasualMeter.Common.Conductors;
using Lunyx.Common;
using System.Runtime.InteropServices;
using System.Reflection;
using log4net;
using System.Threading;
using System.Windows.Forms;

namespace CasualMeter.Common.Helpers
{
    public sealed class ProcessHelper
    {
        private static readonly ILog Logger = LogManager.GetLogger(
            MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly Lazy<ProcessHelper> Lazy = new Lazy<ProcessHelper>(() => new ProcessHelper());

        public static ProcessHelper Instance => Lazy.Value;

        private const uint WM_KEYDOWN = 0x0100;
        private const uint WM_KEYUP = 0x0101;
        private const uint WM_CHAR = 0x0102;

        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly ProcessInfo.WinEventDelegate _dele;//leave this here to prevent garbage collection

        private ProcessHelper() 
        {
            //listen to window focus changed event
            _dele = OnFocusedWindowChanged;
            ProcessInfo.RegisterWindowFocusEvent(_dele);
        }

        public void Initialize()
        {
            //empty method to ensure initialization
        }

        public void ForceVisibilityRefresh(bool toggle=false)
        {
            CasualMessenger.Instance.RefreshVisibility(IsTeraActive, toggle);
        }

        public void UpdateHotKeys()
        {
            var isActive = IsTeraActive;
            if (!isActive.HasValue)
                return;
            if (isActive.Value)
                HotkeyHelper.Instance.Activate();
            else
                HotkeyHelper.Instance.Deactivate();
        }

        private void OnFocusedWindowChanged(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            ForceVisibilityRefresh();
            UpdateHotKeys();
        }

        public bool PressKey(Keys key)
        {
            if (TeraWindow == IntPtr.Zero)
            {
                Logger.Warn("Nullpointer of TeraWindow.");
                return false;
            }

            if(PostMessage(TeraWindow, WM_KEYDOWN, (int)key, 0)) {
                return true;
            }

            Logger.Warn(string.Format("Failed to press key {0}", key));
            return false;
        }

        public bool ReleaseKey(Keys key)
        {
            if (TeraWindow == IntPtr.Zero)
            {
                Logger.Warn("Nullpointer of TeraWindow.");
                return false;
            }

            if (PostMessage(TeraWindow, WM_KEYUP, (int)key, 0))
            {
                return true;
            }

            Logger.Warn(string.Format("Failed to release key {0}", key));
            return false;
        }

        public bool SendString(string s)
        {
            if (TeraWindow == IntPtr.Zero)
            {
                Logger.Warn("Nullpointer of TeraWindow.");
                return false;
            }
            try
            {
                ProcessInfo.SendString(TeraWindow, s);
                return true;
            }
            catch (Exception e)
            {
                //eat this
                Logger.Warn("Failed to send string:", e);
            }
            return false;
        }

        public bool? IsTeraActive => ProcessInfo.GetActiveProcessName()?.Equals("Tera", StringComparison.OrdinalIgnoreCase);

        public IntPtr TeraWindow => ProcessInfo.FindWindow("LaunchUnrealUWindowsClient", "TERA");
    }
}
