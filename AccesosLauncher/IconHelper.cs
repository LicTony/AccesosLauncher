using System;
using System.Collections.Concurrent;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace AccesosLauncher
{
    internal static class IconHelper
    {
        private static readonly ConcurrentDictionary<string, ImageSource> Cache = new(StringComparer.OrdinalIgnoreCase);

        [SupportedOSPlatform("windows")]
        public static ImageSource GetIconImageSource(string path, bool preferLarge = true)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return GetDefaultIcon();

            // Special handling for .url files to get the default browser icon
            if (path.EndsWith(".url", StringComparison.OrdinalIgnoreCase))
            {
                const string browserCacheKey = "::DEFAULT_BROWSER_ICON::";
                if (Cache.TryGetValue(browserCacheKey, out var browserIcon))
                    return browserIcon;

                try
                {
                    var browserPath = GetHttpHandlerPath();
                    if (!string.IsNullOrEmpty(browserPath) && File.Exists(browserPath))
                    {
                        using var ico = Icon.ExtractAssociatedIcon(browserPath);
                        if (ico != null)
                        {
                            var source = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                            source.Freeze();
                            Cache[browserCacheKey] = source; // Cache it
                            return source;
                        }
                    }
                }
                catch
                {
                    // Fall through to default handling for .url files if browser detection fails
                }
            }

            // Default handling for all other files (and fallback for .url)
            if (Cache.TryGetValue(path, out var img))
                return img;

            try
            {
                using var ico = Icon.ExtractAssociatedIcon(path);
                if (ico == null)
                    return GetDefaultIcon();

                var source = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                source.Freeze(); // para usar cross-thread
                Cache[path] = source;
                return source;
            }
            catch
            {
                return GetDefaultIcon();
            }
        }

        [SupportedOSPlatform("windows")]
        private static BitmapSource GetDefaultIcon()
        {
            using var ico = SystemIcons.Application;
            var source = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }

        private static string? GetHttpHandlerPath()
        {
            const string assocStr = "http";
            uint pcchOut = 0;
            // Get buffer size
            AssocQueryString(AssocF.None, AssocStr.Executable, assocStr, null, null, ref pcchOut);
            if (pcchOut == 0)
                return null;

            var pszOut = new StringBuilder((int)pcchOut);
            if (AssocQueryString(AssocF.None, AssocStr.Executable, assocStr, null, pszOut, ref pcchOut) == 0) // S_OK is 0
            {
                return pszOut.ToString();
            }

            return null;
        }

        #region P/Invoke for AssocQueryString

        [DllImport("Shlwapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint AssocQueryString(
            AssocF flags,
            AssocStr str,
            [In] string pszAssoc,
            [In] string? pszExtra,
            [Out] StringBuilder? pszOut,
            [In, Out] ref uint pcchOut
        );

        [Flags]
        private enum AssocF : uint
        {
            None = 0,
            Init_NoRemapCLSID = 0x1,
            Init_ByExeName = 0x2,
            Open_ByExeName = 0x2,
            Init_DefaultToStar = 0x4,
            Init_DefaultToFolder = 0x8,
            NoUserSettings = 0x10,
            NoTruncate = 0x20,
            Verify = 0x40,
            RemapRunDll = 0x80,
            NoFixUps = 0x100,
            IgnoreBaseClass = 0x200,
            Init_IgnoreUnknown = 0x400,
            Init_Fixed_ProgId = 0x800,
            Is_Protocol = 0x1000,
            Init_For_File = 0x2000,
        }

        private enum AssocStr
        {
            Command = 1,
            Executable,
            FriendlyDocName,
            FriendlyAppName,
            NoOpen,
            ShellNewValue,
            DDECommand,
            DDEIfExec,
            DDEApplication,
            DDETopic,
            InfoTip,
            QuickTip,
            TileInfo,
            ContentType,
            DefaultIcon,
            ShellExtension,
            DropTarget,
            DelegateExecute,
            Supported_Uri_Protocols,
            ProgID,
            AppID,
            AppPublisher,
            AppIconReference,
            Max,
        }

        #endregion
    }
}