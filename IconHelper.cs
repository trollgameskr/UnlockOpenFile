using System;
using System.Drawing;

namespace UnlockOpenFile
{
    /// <summary>
    /// Helper class for loading application icons
    /// </summary>
    public static class IconHelper
    {
        /// <summary>
        /// Loads the application icon with intelligent fallback strategy
        /// </summary>
        /// <returns>The application icon, or a system default icon if loading fails</returns>
        public static Icon LoadApplicationIcon()
        {
            try
            {
                // First try to extract the icon from the executable itself
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath) && System.IO.File.Exists(exePath))
                {
                    var extractedIcon = Icon.ExtractAssociatedIcon(exePath);
                    if (extractedIcon != null)
                    {
                        return extractedIcon;
                    }
                }
            }
            catch
            {
                // If extraction fails, try loading from file
            }

            try
            {
                // Fallback to loading from app.ico file
                var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    return new Icon(iconPath);
                }
            }
            catch
            {
                // Final fallback to system icon
            }
            
            return SystemIcons.Application;
        }
    }
}
