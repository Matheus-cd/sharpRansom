using System;
using System.IO;
using System.Runtime.InteropServices;

namespace gosharp
{
    public static class WallPaper
    {
        private const int SPI_SETDESKWALLPAPER = 0x0014;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int SystemParametersInfo(
            int uAction,
            int uParam,
            string lpvParam,
            int fuWinIni);

        /// <summary>
        /// Decodes a base64 image and saves it to a temporary file.
        /// </summary>
        /// <param name="base64Image">The base64-encoded image string</param>
        /// <returns>The path to the saved temporary file, or error message if failed</returns>
        public static string SaveFile(string base64Image)
        {
            try
            {
                // Decode the base64 image
                byte[] imageBytes = Convert.FromBase64String(base64Image);

                // Create a temporary file with a specific pattern
                string tempPath = Path.GetTempPath();
                string tempFileName = $"temp_wallpaper_{Guid.NewGuid()}.png";
                string fullPath = Path.Combine(tempPath, tempFileName);

                // Write the decoded image to the temporary file
                File.WriteAllBytes(fullPath, imageBytes);

                Console.WriteLine("[+] Wallpaper saved successfully.");
                return fullPath;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// Replaces backslashes with forward slashes in a path.
        /// </summary>
        /// <param name="path">The path to modify</param>
        /// <returns>The modified path with forward slashes</returns>
        public static string ReplaceBackslashes(string path)
        {
            return path.Replace("\\", "/");
        }

        /// <summary>
        /// Sets the desktop wallpaper to the specified image path.
        /// </summary>
        /// <param name="imgPath">The full path to the image file</param>
        public static void SetWallpaper(string imgPath)
        {
            Console.WriteLine($"[+] File path: {imgPath}");
            string newImgPath = ReplaceBackslashes(imgPath);
            Console.WriteLine($"[+] New File path: {newImgPath}");

            int result = SystemParametersInfo(
                SPI_SETDESKWALLPAPER,
                0,
                newImgPath,
                SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

            if (result == 0)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new System.ComponentModel.Win32Exception(errorCode,
                    $"Error setting wallpaper: {errorCode}");
            }
        }
    }
}
