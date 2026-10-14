using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Browy.Services
{
    public enum BackdropType
    {
        Auto = 0,
        None = 1,
        Mica = 2,
        Acrylic = 3,
        MicaAlt = 4
    }

    public enum WindowCornerPreference
    {
        Default = 0,
        DoNotRound = 1,
        Round = 2,
        RoundSmall = 3
    }

    public static class WindowsBackdropService
    {
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        public static bool IsWindows11OrGreater =>
            Environment.OSVersion.Version.Major > 10 ||
            (Environment.OSVersion.Version.Major == 10 && Environment.OSVersion.Version.Build >= 22000);

        public static bool ApplyConfiguredBackdrop(Window window, string material)
        {
            if (!IsWindows11OrGreater || string.Equals(material, "SolidDark", StringComparison.OrdinalIgnoreCase))
            {
                window.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x0F, 0x0F, 0x14));
                return false;
            }

            window.Background = System.Windows.Media.Brushes.Transparent;
            var type = material?.ToLowerInvariant() switch
            {
                "mica" => BackdropType.Mica,
                "micaalt" or "mica_alt" or "mica alt" => BackdropType.MicaAlt,
                "none" => BackdropType.None,
                _ => BackdropType.Acrylic
            };

            return ApplyBackdrop(window, type, isDarkMode: true);
        }

        public static bool ApplyBackdrop(Window window, BackdropType backdropType, bool isDarkMode = true, WindowCornerPreference cornerPreference = WindowCornerPreference.Round)
        {
            try
            {
                var handle = new WindowInteropHelper(window).EnsureHandle();
                if (handle == IntPtr.Zero)
                    return false;

                // 1. Set Immersive Dark Mode
                int darkModeVal = isDarkMode ? 1 : 0;
                DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkModeVal, sizeof(int));

                // 2. Set Corner Preference (native Windows 11 rounded corners + shadow)
                int cornerVal = (int)cornerPreference;
                DwmSetWindowAttribute(handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerVal, sizeof(int));

                // 3. Extend Frame into entire client area
                MARGINS margins = new MARGINS
                {
                    cxLeftWidth = -1,
                    cxRightWidth = -1,
                    cyTopHeight = -1,
                    cyBottomHeight = -1
                };
                DwmExtendFrameIntoClientArea(handle, ref margins);

                // 4. Set System Backdrop Type (Acrylic, Mica, or MicaAlt)
                int backdropVal = (int)backdropType;
                int hr = DwmSetWindowAttribute(handle, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropVal, sizeof(int));

                return hr == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
