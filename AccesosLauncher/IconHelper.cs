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
using System.Runtime.InteropServices.Marshalling;
using System.Text;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1401:P/Invokes should not be visible", Justification = "P/Invoke methods are internal implementation details")]

namespace AccesosLauncher
{
    internal static class IconHelper
    {
        private static readonly ConcurrentDictionary<string, ImageSource> Cache = new(StringComparer.OrdinalIgnoreCase);

        [SupportedOSPlatform("windows")]
        public static ImageSource GetIconImageSource(string path)
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
                            var source = ConvertIconToBitmapSource(ico);
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

                var source = ConvertIconToBitmapSource(ico);
                Cache[path] = source;
                return source;
            }
            catch
            {
                return GetDefaultIcon();
            }
        }

        [SupportedOSPlatform("windows")]
        public static ImageSource GetFolderIcon()
        {
            const string folderCacheKey = "::FOLDER_ICON::";
            if (Cache.TryGetValue(folderCacheKey, out var folderIcon))
                return folderIcon;

            try
            {
                // Get the system folder icon
                var shinfo = new SHFILEINFO();
                var ret = SHGetFileInfo(@"C:\", 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), 
                    SHGFI_ICON | SHGFI_LARGEICON | SHGFI_USEFILEATTRIBUTES);
                
                if (ret != IntPtr.Zero)
                {
                    using var icon = Icon.FromHandle(shinfo.hIcon);
                    var source = ConvertIconToBitmapSource(icon);
                    Cache[folderCacheKey] = source;
                    return source;
                }
            }
            catch
            {
                // If we can't get the folder icon, fall back to default icon
                return GetDefaultIcon();
            }
            
            return GetDefaultIcon();
        }

        private static BitmapImage ConvertIconToBitmapSource(Icon icon)
        {
            using var bmp = icon.ToBitmap();
            var stream = new MemoryStream();
            bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            stream.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = stream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }

        [SupportedOSPlatform("windows")]
        private static BitmapImage GetDefaultIcon()
        {
            using var ico = SystemIcons.Application;
            return ConvertIconToBitmapSource(ico);
        }

        private static string? GetHttpHandlerPath()
        {
            const string assocStr = "http";
            uint pcchOut = 0;
            // Get buffer size
            uint hresult = AssocQueryString(AssocF.None, AssocStr.Executable, assocStr, null, null, ref pcchOut);
            if (hresult > 1 || pcchOut == 0) // S_OK=0, S_FALSE=1. Anything else is an error.
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
            Open_ByExeName = Init_ByExeName,
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

        #region P/Invoke for Folder Icon

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SHGetFileInfo(
            string pszPath, 
            uint dwFileAttributes, 
            ref SHFILEINFO psfi, 
            uint cbFileInfo, 
            uint uFlags);

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_LARGEICON = 0x000000000;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

        #endregion
    }
}