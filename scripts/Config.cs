using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        public static HashSet<string> FavoriteSongs = new HashSet<string>();
        public static int SongsUntilNextFavoriteCheck = 0;
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
                    FavoriteSongs = configData.favoriteSongs ?? new HashSet<string>();
                    SongsUntilNextFavoriteCheck = configData.songsUntilNextFavoriteCheck;
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
                FavoriteSongs = new HashSet<string>(); // Default empty favorites
                SongsUntilNextFavoriteCheck = 0; // Default no delay

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
                currentlyRepeating = CurrentlyRepeating,
                favoriteSongs = FavoriteSongs,
                songsUntilNextFavoriteCheck = SongsUntilNextFavoriteCheck
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
            public HashSet<string> favoriteSongs { get; set; }
            public int songsUntilNextFavoriteCheck { get; set; }
        }

        public enum CurrentWindowMode
        {
            Collapsed,
            Normal,
        }

        /// <summary>
        /// Adds or removes a song from favorites based on its current state
        /// </summary>
        /// <param name="songName">The song name without path</param>
        /// <returns>True if the song is now favorited, false if removed from favorites</returns>
        public static bool ToggleFavorite(string songName)
        {
            if (FavoriteSongs.Contains(songName))
            {
                FavoriteSongs.Remove(songName);
                return false;
            }
            else
            {
                FavoriteSongs.Add(songName);
                return true;
            }
        }

        /// <summary>
        /// Checks if a song is favorited
        /// </summary>
        /// <param name="songName">The song name without path</param>
        /// <returns>True if the song is favorited</returns>
        public static bool IsFavorite(string songName)
        {
            return FavoriteSongs.Contains(songName);
        }

        /// <summary>
        /// Gets a random favorite song if available and cooldown allows
        /// </summary>
        /// <returns>A random favorite song name, or null if none available or cooldown active</returns>
        public static string GetRandomFavoriteSong()
        {
            if (SongsUntilNextFavoriteCheck > 0 || FavoriteSongs.Count == 0)
            {
                return null;
            }

            var random = new Random();
            var favoriteArray = FavoriteSongs.ToArray();
            return favoriteArray[random.Next(favoriteArray.Length)];
        }

        /// <summary>
        /// Sets the cooldown for favorite song selection
        /// </summary>
        public static void SetFavoriteCooldown()
        {
            var random = new Random();
            SongsUntilNextFavoriteCheck = random.Next(2, 6); // Random between 2-5 songs
        }

        /// <summary>
        /// Decrements the favorite cooldown counter
        /// </summary>
        public static void DecrementFavoriteCooldown()
        {
            if (SongsUntilNextFavoriteCheck > 0)
            {
                SongsUntilNextFavoriteCheck--;
            }
        }
        #endregion
    }
}
