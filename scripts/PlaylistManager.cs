using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using TagLib;
using static Symphonia.scripts.Config;
using static Symphonia.scripts.Defaults;

namespace Symphonia.scripts
{
    /// <summary>
    /// Used to keep track of all songs being played.
    /// </summary>
    internal static class PlaylistManager
    {
        public static Playlist CurrentPlaylist = new();

        public class Playlist
        {
            public List<string> songPaths = new();
            public int currentIndex = -1;  
            
            /// <summary>
            /// Initialize the playlist with init mode.
            /// </summary>
            /// <param name="initMode"></param>
            public void InitializePlaylist(InitMode initMode, string searchPath = "")
            {
                switch (initMode)
                {
                    case InitMode.ShuffleAll:
                        songPaths = new List<string>()
                        {
                            GetRandomSongFromPath(null, new List<string>())
                        };
                        currentIndex = 0;
                        break;

                    case InitMode.FromSearch:
                        songPaths = new List<string>()
                        {
                            searchPath
                        };
                        currentIndex = 0;
                        break;
                }
            }

            public string CurrentSongFilePath => (currentIndex >= 0 && currentIndex < songPaths.Count) ? songPaths[currentIndex] : string.Empty;
            public string CurrentSong => System.IO.Path.GetFileNameWithoutExtension(CurrentSongFilePath);

            public BitmapImage CurrentSongCover()
            {
                if (string.IsNullOrEmpty(CurrentSongFilePath))
                {
                    return new BitmapImage(new Uri("", UriKind.Relative));
                }

                try
                {
                    var file = TagLib.File.Create(CurrentSongFilePath);
                    IPicture pic = file.Tag.Pictures.FirstOrDefault();
                    if (pic != null)
                    {
                        using (MemoryStream ms = new MemoryStream(pic.Data.Data))
                        {
                            BitmapImage bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.StreamSource = ms;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            return bitmap;
                        }
                    }
                    else
                    {
                        return new BitmapImage(new Uri("", UriKind.Relative));
                    }
                }
                catch
                {
                    return new BitmapImage(new Uri("", UriKind.Relative));
                }
            }

            public enum InitMode
            {
                ShuffleAll,
                FromSearch
            }

            public void IncrementPlaylist(bool goBack)
            {
                if (goBack)
                {
                    currentIndex--;
                    currentIndex = (int)MathF.Max(currentIndex, 0);
                }
                else
                {
                    if (currentIndex < songPaths.Count - 1)
                    {
                        currentIndex++;
                        return;
                    }

                    // We're at the end of the playlist, need to add a new song
                    var currentSongPath = (songPaths.Count > 0 && currentIndex >= 0 && currentIndex < songPaths.Count) 
                        ? songPaths[currentIndex] 
                        : null;
                    
                    var newSong = GetRandomSongFromPath(currentSongPath, songPaths);
                    
                    // Additional safety check to prevent duplicates
                    if (!string.IsNullOrEmpty(newSong) && !ContainsSong(newSong))
                    {
                        songPaths.Add(newSong);
                        currentIndex++;
                    }
                }
            }


            public void DecrementPlaylist()
            {
                if (currentIndex <= 0)
                {
                    currentIndex = 0;
                    return;
                }

                currentIndex--;
                return;
            }

            public void ResetPlaylist()
            {
                songPaths.Clear();
                currentIndex = -1;
            }

            /// <summary>
            /// Checks if a song already exists in the playlist.
            /// </summary>
            /// <param name="songPath">The song path to check</param>
            /// <returns>True if the song exists in the playlist, false otherwise</returns>
            public bool ContainsSong(string songPath)
            {
                return songPaths.Any(path => string.Equals(path, songPath, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Returns a random song from the path, with a chance to select from favorites.
        /// </summary>
        /// <param name="currentSongPath">The current song path to avoid</param>
        /// <param name="existingPlaylist">The current playlist to avoid duplicates</param>
        /// <returns></returns>
        // Returns a random song, avoiding the current song and existing playlist songs if possible
        private static string GetRandomSongFromPath(string currentSongPath = null, List<string> existingPlaylist = null)
        {
            Random rand = new();

            List<string> musicFiles = new();
            // Check if we should try to play a favorite song (30% chance)
            string favoriteSong = GetRandomFavoriteSong();
            if (favoriteSong != null && rand.Next(100) < 30) // 30% chance to play a favorite
            {
                // Try to find the favorite song in the music folder
                musicFiles = supportedFormats.SelectMany(format => Directory.GetFiles(PathToMusicFolder, format, SearchOption.AllDirectories))
                                                 .ToList();

                var favoriteSongPath = musicFiles.FirstOrDefault(file => 
                    Path.GetFileNameWithoutExtension(file).Equals(favoriteSong, StringComparison.OrdinalIgnoreCase));

                if (favoriteSongPath != null)
                {
                    // Set cooldown after playing a favorite
                    SetFavoriteCooldown();
                    return favoriteSongPath;
                }
            }

            // Decrement cooldown counter
            DecrementFavoriteCooldown();

            // Regular random song selection
            var allMusicFiles = supportedFormats.SelectMany(format => Directory.GetFiles(PathToMusicFolder, format, SearchOption.AllDirectories))
                .ToList();

            if (allMusicFiles.Count == 0)
            {
                System.Windows.MessageBox.Show("No music files found in the specified folder.", "Symphonia", MessageBoxButton.OK, MessageBoxImage.Error);
                return string.Empty;
            }

            // Exclude current song and existing playlist songs if more than one song exists
            var songsToExclude = new List<string>();
            
            if (!string.IsNullOrEmpty(currentSongPath))
            {
                songsToExclude.Add(currentSongPath);
            }
            
            if (existingPlaylist != null && existingPlaylist.Count > 0)
            {
                songsToExclude.AddRange(existingPlaylist);
            }
            
            if (songsToExclude.Count > 0 && allMusicFiles.Count > songsToExclude.Count)
            {
                allMusicFiles = allMusicFiles.Where(f => !songsToExclude.Any(exclude => 
                    string.Equals(f, exclude, StringComparison.OrdinalIgnoreCase))).ToList();
            }

            string songToPlay = allMusicFiles.OrderBy(x => rand.Next()).FirstOrDefault();
            return songToPlay ?? string.Empty;
        }
    }
}
