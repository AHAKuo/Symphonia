using Newtonsoft.Json;
using System;
using System.IO;

namespace Symphonia.scripts
{
    /// <summary>
    /// Stores the configuration of the app.
    /// </summary>
    internal static class Config
    {
        public const string prefsFolder = "PersonalApps/Symphonia";
        public static string ConfigPath => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), prefsFolder);

        #region Savables
        public static string PathToMusicFolder = string.Empty;
        public static CurrentWindowMode currentWindowMode = CurrentWindowMode.Normal;
        public static float CurrentSongVolume = 0.5f;
        public static bool CurrentlyTopMost = true;
        public static bool CurrentlyRepeating;
        #endregion

        #region Loading
        public static void LoadConfig(Action endAction)
        {
            string filePath = Path.Combine(ConfigPath, "config.json");

            // if file path doesn't exist create the directory
            if (!Directory.Exists(ConfigPath))
            {
                Directory.CreateDirectory(ConfigPath);
            }

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var configData = JsonConvert.DeserializeObject<ConfigData>(json);

                if (configData != null)
                {
                    // Load data from ConfigData object
                    PathToMusicFolder = configData.pathToMusicFolder;
                    CurrentSongVolume = configData.currentSongVolume;
                    currentWindowMode = configData.currentWindowMode;
                    CurrentlyTopMost = configData.currentlyTopMost;
                    CurrentlyRepeating = configData.currentlyRepeating;
                }
            }
            else
            {
                // Initialize default values
                PathToMusicFolder = ""; // Default music folder path
                CurrentSongVolume = 0.5f; // Default volume
                currentWindowMode = CurrentWindowMode.Normal; // Default window mode
                CurrentlyTopMost = true; // Default topmost setting
                CurrentlyRepeating = false; // Default repeating setting

                // Save the default configuration
                SaveConfig();
            }

            endAction?.Invoke();
        }

        public static void SaveConfig()
        {
            var configData = new ConfigData
            {
                pathToMusicFolder = PathToMusicFolder,
                currentSongVolume = CurrentSongVolume,
                currentWindowMode = currentWindowMode,
                currentlyTopMost = CurrentlyTopMost,
                currentlyRepeating = CurrentlyRepeating
            };

            string json = JsonConvert.SerializeObject(configData, Formatting.Indented);

            string filePath = Path.Combine(ConfigPath, "config.json");
            File.WriteAllText(filePath, json);
        }

        private class ConfigData
        {
            public string pathToMusicFolder { get; set; }
            public float currentSongVolume { get; set; }
            public CurrentWindowMode currentWindowMode { get; set; }
            public bool currentlyTopMost { get; set; }
            public bool currentlyRepeating { get; set; }
        }

        public enum CurrentWindowMode
        {
            Collapsed,
            Normal,
        }
        #endregion
    }
}
