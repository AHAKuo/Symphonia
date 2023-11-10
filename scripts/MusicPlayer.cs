using NAudio.Wave;
using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using static Symphonia.scripts.Config;
using static Symphonia.scripts.PlaylistManager;

namespace Symphonia.scripts
{
    /// <summary>
    /// Handles things in the player itself, not the song.
    /// </summary>
    internal static class MusicPlayer
    {
        public static bool HasInited => CurrentPlayingEvent != null
        &&
        (CurrentPlayingEvent.PlaybackState == PlaybackState.Playing || CurrentPlayingEvent.PlaybackState == PlaybackState.Paused || CurrentPlayingEvent.PlaybackState == PlaybackState.Paused);

        public static bool IsPlaying => CurrentPlayingEvent != null
        &&
        (CurrentPlayingEvent.PlaybackState == PlaybackState.Playing);

        public static bool IsPaused => CurrentPlayingEvent != null
        &&
        (CurrentPlayingEvent.PlaybackState == PlaybackState.Paused);

        public static bool IsPausedOrNotValid => IsPaused || !HasInited;
        public static bool IsHoldingSeekbar;
        public static TimeSpan CurrentSongDuration => (TimeSpan)(CurrentPlayingFileReader?.TotalTime);

        #region MyRegion

        #endregion

        public static AudioFileReader CurrentPlayingFileReader;
        public static WaveOutEvent CurrentPlayingEvent = new()
        {
            Volume = CurrentSongVolume
        };

        public delegate void HasPlayed();
        public static event HasPlayed OnHasPlayed;

        public delegate void Update();
        public static event Update OnUpdate;

        public delegate void HasStopped();
        public static event HasStopped OnHasStopped;

        public static void ClearCurrentPlayer() => CurrentPlayingEvent.Dispose();

        /// <summary>
        /// Plays the song with the file path.
        /// </summary>
        /// <param name="filePath"></param>
        public static async Task MusicPlayingTask()
        {
            try
            {
                CurrentPlayingEvent = new();
                CurrentPlayingFileReader = new AudioFileReader(CurrentPlaylist.CurrentSongFilePath); // Reads the mp3 file
                CurrentPlayingEvent.Init(CurrentPlayingFileReader);
                CurrentPlayingFileReader.Volume = CurrentSongVolume;
                CurrentPlayingEvent.Play();

                // add continue event

                CurrentPlayingEvent.PlaybackStopped += (sender, e) =>
                {
                    OnHasStopped?.Invoke();
                };

                OnHasPlayed?.Invoke();

                // Wait for the file to finish playing
                while (IsPlaying
                    || IsPaused)
                {
                    await Task.Delay(100);
                    OnUpdate?.Invoke();
                }

                GoToNextSongAutomatically();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while playing the file: " + ex.Message);
            }
        }

        #region Player Methods

        public static void PauseOrPlay(Action endAction)
        {
            if (IsPlaying)
            {
                PauseCurrentMusic(endAction);
                return;
            }

            if (IsPaused)
            {
                ResumeCurrentMusic(endAction);
                return;
            }
        }

        /// <summary>
        /// Shuffles all the music in the music folder.
        /// </summary>
        public static async void ShuffleAll()
        {
            // first, stop music if playing

            if (HasInited)
            {
                ClearCurrentPlayer();
            }

            CurrentPlaylist.ResetPlaylist();
            CurrentPlaylist.InitializePlaylist(Playlist.InitMode.ShuffleAll);

            await MusicPlayingTask();
        }

        public static void ToggleRepeat(Action endAction)
        {
            CurrentlyRepeating = !CurrentlyRepeating;
            endAction?.Invoke();
        }

        /// <summary>
        /// Goes to the next song, while keeping the previous one safe. Similar to the shuffle method, but doesn't clear.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public static async void NextSong()
        {
            // first, stop music if playing
            if (HasInited)
            {
                ClearCurrentPlayer();
            }

            CurrentPlaylist?.IncrementPlaylist(false);

            await MusicPlayingTask();
        }

        /// <summary>
        /// Goes to the next song, while keeping the list intact. Similar to the shuffle method, but doesn't clear.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public static async void PreviousSong()
        {
            // first, stop music if playing
            if (HasInited)
            {
                ClearCurrentPlayer();
            }

            // if not more than 5 seconds passed, go back 
            if (CurrentPlayingFileReader.CurrentTime.TotalSeconds < 5)
            {
                CurrentPlaylist?.IncrementPlaylist(true);
            }

            await MusicPlayingTask();
        }

        public static async Task RestartSong()
        {
            // first, stop music if playing
            if (HasInited)
            {
                ClearCurrentPlayer();
            }

            await MusicPlayingTask();
        }

        #region Pause & Play


        public static void ResumeCurrentMusic(Action endAction)
        {
            if (IsPaused)
            {
                CurrentPlayingEvent?.Play();
                endAction?.Invoke();
            }
        }

        /// <summary>
        /// Pauses the song.
        /// </summary>
        public static void PauseCurrentMusic(Action endAction)
        {
            if (IsPlaying)
            {
                CurrentPlayingEvent?.Pause();
                endAction?.Invoke();
            }
        }

        /// <summary>
        /// For next song.
        /// </summary>
        public static async void GoToNextSongAutomatically()
        {
            if (CurrentlyRepeating)
            {
                await RestartSong();
            }
            else
            {
                NextSong();
            }
        }
        #endregion

        #endregion
    }
}
