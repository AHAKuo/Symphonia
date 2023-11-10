using System;

namespace Symphonia.scripts
{
    /// <summary>
    /// Stores the configuration of the app.
    /// </summary>
    internal static class Config
    {
        public const string prefsFolder = "PersonalApps/Symphonia";
        public static string configPath => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), prefsFolder);

        #region Savables
        public static string pathToMusicFolder = string.Empty;
        public static CurrentWindowMode currentWindowMode = CurrentWindowMode.Normal;
        public static float CurrentSongVolume = 0.5f;
        public static bool CurrentlyTopMost = true;
        public static bool CurrentlyRepeating;
        #endregion

        #region Loading
        /// <summary>
        /// Load the config
        /// </summary>
        public static void LoadConfig(Action endAction)
        {
            // if it doesn't exist, create it
            if (!System.IO.Directory.Exists(configPath))
                System.IO.Directory.CreateDirectory(configPath);

            // Define the path to the 'folderpath.txt' file
            string filePath = System.IO.Path.Combine(configPath, "folderpath.txt");

            // Check if the 'folderpath.txt' file exists
            if (System.IO.File.Exists(filePath))
            {
                // Read the path from the file
                string savedPath = System.IO.File.ReadAllText(filePath);

                // Check if the read path is not empty and directory exists
                if (!string.IsNullOrWhiteSpace(savedPath) && System.IO.Directory.Exists(savedPath))
                {
                    pathToMusicFolder = savedPath;
                }
                else
                {
                    System.Windows.MessageBox.Show("Saved music folder path is invalid or does not exist.");
                    // You can also set a default path or prompt the user to select a folder again
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Music folder path not set. Please select a folder.");
                // Prompt the user to select a folder or set a default path
            }

            CurrentSongVolume = 0.5f; // to be loaded later
            currentWindowMode = CurrentWindowMode.Normal; // to be loaded.
            CurrentlyTopMost = true; // to be loaded

            endAction?.Invoke();
        }

        public static void SaveConfig()
        {
            // saves current data
        }

        public enum CurrentWindowMode
        {
            Collapsed,
            Normal,
        }
        #endregion
    }
}
