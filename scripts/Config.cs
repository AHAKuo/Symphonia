using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Symphonia.scripts
{
    /// <summary>
    /// Represents a favorite song with name and album information
    /// </summary>
    public class FavoriteSong
    {
        public string SongName { get; set; }
        public string AlbumName { get; set; }

        public FavoriteSong() { }

        public FavoriteSong(string songName, string albumName)
        {
            SongName = songName;
            AlbumName = albumName ?? "Unknown Album";
        }

        public override bool Equals(object obj)
        {
            if (obj is FavoriteSong other)
            {
                return string.Equals(SongName, other.SongName, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(AlbumName, other.AlbumName, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                SongName?.ToLowerInvariant() ?? string.Empty,
                AlbumName?.ToLowerInvariant() ?? string.Empty
            );
        }

        public override string ToString()
        {
            return $"{SongName}|||{AlbumName}";
        }

        public static FavoriteSong FromString(string str)
        {
            var parts = str.Split(new[] { "|||" }, StringSplitOptions.None);
            if (parts.Length == 2)
            {
                return new FavoriteSong(parts[0], parts[1]);
            }
            // Backward compatibility: if no separator, treat as song name only
            return new FavoriteSong(str, "Unknown Album");
        }
    }

    /// <summary>
    /// Stores the configuration of the app.
    /// </summary>
    internal static class Config
    {
        public const string prefsFolder = "Ahasan アハさん/Symphonia";
        public static string ConfigPath => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), prefsFolder);

        #region Savables
        public static string PathToMusicFolder = string.Empty;
        public static CurrentWindowMode currentWindowMode = CurrentWindowMode.Normal;
        public static float CurrentSongVolume = 0.5f;
        public static bool CurrentlyTopMost = true;
        public static bool CurrentlyRepeating;
        public static HashSet<FavoriteSong> FavoriteSongs = new HashSet<FavoriteSong>();
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
                    
                    // Load favorites with backward compatibility
                    if (configData.favoriteSongsV2 != null && configData.favoriteSongsV2.Count > 0)
                    {
                        // New format: use FavoriteSong objects
                        FavoriteSongs = configData.favoriteSongsV2;
                    }
                    else if (configData.favoriteSongs != null && configData.favoriteSongs.Count > 0)
                    {
                        // Old format: migrate from string to FavoriteSong with album detection
                        FavoriteSongs = new HashSet<FavoriteSong>();
                        foreach (var oldFavoriteName in configData.favoriteSongs)
                        {
                            // Try to find the song file and extract album info
                            string albumName = TryGetAlbumNameForSong(oldFavoriteName);
                            FavoriteSongs.Add(new FavoriteSong(oldFavoriteName, albumName));
                        }
                        // Save the migrated data immediately
                        SaveConfig();
                    }
                    else
                    {
                        FavoriteSongs = new HashSet<FavoriteSong>();
                    }
                    
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
                FavoriteSongs = new HashSet<FavoriteSong>(); // Default empty favorites
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
                favoriteSongs = null, // Deprecated, keeping for compatibility
                favoriteSongsV2 = FavoriteSongs,
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
            [Obsolete("Use favoriteSongsV2 instead")]
            public HashSet<string> favoriteSongs { get; set; } // Old format for backward compatibility
            public HashSet<FavoriteSong> favoriteSongsV2 { get; set; } // New format with album info
            public int songsUntilNextFavoriteCheck { get; set; }
        }

        public enum CurrentWindowMode
        {
            Collapsed,
            Normal,
        }

        /// <summary>
        /// Tries to get the album name for a song by searching for the file
        /// </summary>
        private static string TryGetAlbumNameForSong(string songName)
        {
            if (string.IsNullOrEmpty(PathToMusicFolder) || !Directory.Exists(PathToMusicFolder))
            {
                return "Unknown Album";
            }

            try
            {
                var musicFiles = Defaults.supportedFormats
                    .SelectMany(format => Directory.GetFiles(PathToMusicFolder, format, SearchOption.AllDirectories))
                    .ToList();

                var matchingFile = musicFiles.FirstOrDefault(file =>
                    Path.GetFileNameWithoutExtension(file).Equals(songName, StringComparison.OrdinalIgnoreCase));

                if (matchingFile != null)
                {
                    return GetAlbumNameFromFile(matchingFile);
                }
            }
            catch
            {
                // If any error occurs, return unknown album
            }

            return "Unknown Album";
        }

        /// <summary>
        /// Extracts album name from a music file using TagLib
        /// </summary>
        public static string GetAlbumNameFromFile(string filePath)
        {
            try
            {
                var file = TagLib.File.Create(filePath);
                string albumName = file.Tag.Album;
                return string.IsNullOrWhiteSpace(albumName) ? "Unknown Album" : albumName;
            }
            catch
            {
                return "Unknown Album";
            }
        }

        /// <summary>
        /// Adds or removes a song from favorites based on its current state
        /// </summary>
        /// <param name="songName">The song name without path</param>
        /// <param name="albumName">The album name</param>
        /// <returns>True if the song is now favorited, false if removed from favorites</returns>
        public static bool ToggleFavorite(string songName, string albumName)
        {
            var favoriteSong = new FavoriteSong(songName, albumName);
            if (FavoriteSongs.Contains(favoriteSong))
            {
                FavoriteSongs.Remove(favoriteSong);
                return false;
            }
            else
            {
                FavoriteSongs.Add(favoriteSong);
                return true;
            }
        }

        /// <summary>
        /// Checks if a song is favorited
        /// </summary>
        /// <param name="songName">The song name without path</param>
        /// <param name="albumName">The album name</param>
        /// <returns>True if the song is favorited</returns>
        public static bool IsFavorite(string songName, string albumName)
        {
            var favoriteSong = new FavoriteSong(songName, albumName);
            return FavoriteSongs.Contains(favoriteSong);
        }

        /// <summary>
        /// Gets a random favorite song if available and cooldown allows
        /// </summary>
        /// <returns>A random favorite song, or null if none available or cooldown active</returns>
        public static FavoriteSong GetRandomFavoriteSong()
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
