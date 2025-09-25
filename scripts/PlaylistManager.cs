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
                            GetRandomSongFromPath()
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

            public string CurrentSongFilePath => songPaths[currentIndex];
            public string CurrentSong => System.IO.Path.GetFileNameWithoutExtension(CurrentSongFilePath);

            public BitmapImage CurrentSongCover()
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

            public enum InitMode
            {
                ShuffleAll,
                FromSearch
            }

            public void IncrementPlaylist(bool goBack)
            {
                if (songPaths.Count + 1 > maxPlaylistSize)
                {
                    songPaths.RemoveAt(0);
                }

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

                    currentIndex++;
                    var newSong = GetRandomSongFromPath(songPaths.Count > 0 ? songPaths[currentIndex] : null);
                    songPaths.Add(newSong);
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
        }

        /// <summary>
        /// Returns a random song from the path, with a chance to select from favorites.
        /// </summary>
        /// <returns></returns>
        // Returns a random song, avoiding the current song if possible
        private static string GetRandomSongFromPath(string currentSongPath = null)
        {
            Random rand = new();

            // Check if we should try to play a favorite song (30% chance)
            string favoriteSong = GetRandomFavoriteSong();
            if (favoriteSong != null && rand.Next(100) < 30) // 30% chance to play a favorite
            {
                // Try to find the favorite song in the music folder
                var musicFiles = supportedFormats.SelectMany(format => Directory.GetFiles(PathToMusicFolder, format, SearchOption.AllDirectories))
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

            // Exclude current song if more than one song exists
            if (!string.IsNullOrEmpty(currentSongPath) && musicFiles.Count > 1)
            {
                musicFiles = musicFiles.Where(f => !string.Equals(f, currentSongPath, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            string songToPlay = allMusicFiles.OrderBy(x => rand.Next()).FirstOrDefault();
            return songToPlay ?? string.Empty;
        }
    }
}
