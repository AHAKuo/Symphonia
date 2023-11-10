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
            public void InitializePlaylist(InitMode initMode)
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
                ShuffleAll
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
                    var newSong = GetRandomSongFromPath();
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
        /// Returns a random song from the path.
        /// </summary>
        /// <returns></returns>
        private static string GetRandomSongFromPath()
        {
            try
            {
                Random rand = new();
                var musicFiles = new List<string>();

                // Get all mp3 files in the root directory
                musicFiles.AddRange(Directory.GetFiles(pathToMusicFolder, "*.mp3"));

                // Get all subdirectories in the root directory
                var firstLevelDirectories = Directory.GetDirectories(pathToMusicFolder);

                foreach (var dir in firstLevelDirectories)
                {
                    // Get all mp3 files in the first level subdirectory
                    musicFiles.AddRange(Directory.GetFiles(dir, "*.mp3"));

                    // Get all subdirectories in the first level subdirectory
                    var secondLevelDirectories = Directory.GetDirectories(dir);
                    foreach (var subDir in secondLevelDirectories)
                    {
                        // Get all mp3 files in the second level subdirectory
                        musicFiles.AddRange(Directory.GetFiles(subDir, "*.mp3"));
                    }
                }

                if (musicFiles.Count <= 0)
                {
                    System.Windows.MessageBox.Show("No music files found in the specified folder.", "Symphonia", MessageBoxButton.OK, MessageBoxImage.Error);
                    return string.Empty;
                }

                string songToPlay = musicFiles.OrderBy(x => rand.Next()).ToArray().FirstOrDefault();

                if (string.IsNullOrEmpty(songToPlay)) { return string.Empty; }

                return songToPlay;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("An error occured: " + ex.Message, "Symphonia", MessageBoxButton.OK, MessageBoxImage.Error);
                return string.Empty;
            }
        }

        #region Playlist Handler

        #endregion


    }
}
