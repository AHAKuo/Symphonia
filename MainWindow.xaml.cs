using Microsoft.VisualBasic;
using Symphonia.Dialogs;
using Symphonia.external;
using Symphonia.scripts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using static Symphonia.scripts.Config;
using static Symphonia.scripts.Defaults;
using static Symphonia.scripts.MusicPlayer;
using static Symphonia.scripts.PlaylistManager;

namespace Symphonia
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Tracking Bools

        #endregion

        #region OS Keyboard Integration
        private HwndSource hwndSource;
        private bool hotkeysRegistered;

        // Win32 constants
        private const int WM_HOTKEY = 0x0312;
        private const int WM_APPCOMMAND = 0x0319;

        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;

        // AppCommand constants
        private const int APPCOMMAND_VOLUME_MUTE = 8;
        private const int APPCOMMAND_VOLUME_DOWN = 9;
        private const int APPCOMMAND_VOLUME_UP = 10;
        private const int APPCOMMAND_MEDIA_NEXTTRACK = 11;
        private const int APPCOMMAND_MEDIA_PREVIOUSTRACK = 12;
        private const int APPCOMMAND_MEDIA_STOP = 13;
        private const int APPCOMMAND_MEDIA_PLAY_PAUSE = 14;

        // Hotkey IDs
        private const int HOTKEY_PLAY_PAUSE = 1;
        private const int HOTKEY_NEXT = 2;
        private const int HOTKEY_PREV = 3;
        private const int HOTKEY_VOL_UP = 4;
        private const int HOTKEY_VOL_DOWN = 5;
        private const int HOTKEY_TOGGLE_REPEAT = 6;
        private const int HOTKEY_SHUFFLE = 7;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        #endregion

        public MainWindow()
        {
            InitializeComponent(); // from system

            WindowsManager.InitAll(this);

            InitPlayerEvents();

            InitEvents(); // connect buttons

            InitDefaults(this); // init the defaults

            LoadConfig(() =>
            {
                VolumeBar.Value = CurrentSongVolume;
            }); // load settings

            UpdateAll(); // update window with settings.

            // Handle command-line invocation and OS keyboard integration
            HandleCommandLineArgs();
        }

        private void InitPlayerEvents()
        {
            OnHasPlayed += UpdateAll;
            OnUpdate += UpdateSeekbar;
        }

        #region Inits
        /// <summary>
        /// Subscribes the buttons to their respective methods
        /// </summary>
        private void InitEvents()
        {
            Topbar.MouseLeftButtonDown += (sender, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    this.DragMove();
                }
            };

            CloseWindowButton.Click += (sender, e) =>
            {
                IsEnabled = false;
                CurrentPlayingFileReader?.Dispose();
                CurrentPlayingEvent?.Dispose();

                // Animations for opacity
                var animOpacityOut = new DoubleAnimation(0, TimeSpan.FromSeconds(0.15));
                var animOpacityIn = new DoubleAnimation(1, TimeSpan.FromSeconds(0));

                // Animations for scale
                var animScaleOut = new DoubleAnimation(0, TimeSpan.FromSeconds(0.15));
                var animScaleIn = new DoubleAnimation(1, TimeSpan.FromSeconds(0));

                // Set the animation to run when completed.
                animOpacityOut.Completed += (s, _) =>
                {
                    Close();
                };

                // Start the animations.
                this.BeginAnimation(UIElement.OpacityProperty, animOpacityOut);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animScaleOut);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animScaleOut);
            };

            MinimizeWindowButton.Click += (sender, e) =>
            {
                IsEnabled = false;

                // Animations for opacity
                var animOpacityOut = new DoubleAnimation(0, TimeSpan.FromSeconds(0.15));
                var animOpacityIn = new DoubleAnimation(1, TimeSpan.FromSeconds(0));

                // Animations for scale
                var animScaleOut = new DoubleAnimation(0, TimeSpan.FromSeconds(0.15));
                var animScaleIn = new DoubleAnimation(1, TimeSpan.FromSeconds(0));

                // Set the animation to run when completed.
                animOpacityOut.Completed += (s, _) =>
                {
                    this.WindowState = WindowState.Minimized;
                    IsEnabled = true;

                    // Reset animations
                    this.BeginAnimation(UIElement.OpacityProperty, animOpacityIn);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animScaleIn);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animScaleIn);
                };

                // Start the animations.
                this.BeginAnimation(UIElement.OpacityProperty, animOpacityOut);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animScaleOut);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animScaleOut);
            };
            SetPathButton.Click += (sender, e) => SetMusicPath();
            FavoriteConfigButton.Click += FavoriteConfigButton_Click;
            MusicControlButton_1.Click += (sender, e) => PauseOrPlay(UpdateAll);
            MusicControlButton_3.Click += (sender, e) => ShuffleAll();
            SeekBar.Loaded += (sender, e) =>
            {
                // Get the Track control from the Slider
                var track = (Track)SeekBar.Template.FindName("PART_Track", SeekBar);

                if (track != null)
                {
                    // Get the Thumb control from the Track
                    var thumb = track.Thumb;

                    if (thumb != null)
                    {
                        thumb.DragStarted += (s, eArgs) => { IsHoldingSeekbar = true; };
                        thumb.DragDelta += (s, eArgs) => { UpdateTimestamp(); };
                        thumb.DragCompleted += (s, eArgs) => { IsHoldingSeekbar = false; };
                    }

                    // Handle clicks on the track
                    track.PreviewMouseLeftButtonDown += (s, eArgs) =>
                    {
                        System.Windows.Point position = eArgs.GetPosition(SeekBar);
                        double value = SeekBar.Minimum + (SeekBar.Maximum - SeekBar.Minimum) * (position.X / track.ActualWidth);
                        SeekBar.Value = value;

                        IsHoldingSeekbar = true;
                        UpdateSongTime();
                        IsHoldingSeekbar = false;
                    };
                }
            };
            SeekBar.ValueChanged += (sender, e) => UpdateSongTime();
            VolumeBar.ValueChanged += (sender, e) => UpdateVolume();
            SetMinPositiveVolumeButton.Click += (sender, e) => VolumeBar.Value = 0.005f;
            MusicControlButton_4.Click += (sender, e) => NextSong();
            MusicControlButton_0.Click += (sender, e) => PreviousSong();
            MusicControlButton_2.Click += (sender, e) => ToggleRepeat(UpdateAll);
            ToggleWindowMode.Click += (sender, e) => {
                currentWindowMode = currentWindowMode == CurrentWindowMode.Collapsed ? CurrentWindowMode.Normal : CurrentWindowMode.Collapsed;
                UpdateImages();
                UpdateConfigs();
            };
            InputBoxField.KeyDown += (sender, e) =>
            {
                if(e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Return)
                {
                    SearchForFiles(PathToMusicFolder, InputBoxField.Text, false);
                }
            };

            // set the album opener key on the icon
            SymphoniaIcon.MouseDown += (sender, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    // create a new search query window that fills the window with all albums in the music folder
                    Search.SearchQuery _ = new();
                    _.SearchByMetaData((s) =>
                    {
                        SearchForFiles(Path.GetFullPath(s), " ", true);
                    }, Search.MediaMetaData.Folders, PathToMusicFolder, true);
                }
            };

            // Heart icon click handler for favorites
            HeartIcon.MouseDown += (sender, e) =>
            {
                if (e.ChangedButton == MouseButton.Left && HasInited)
                {
                    ToggleCurrentSongFavorite();
                }
            };
        }

        #region Methods
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Attach native message hook for media keys and hotkeys
            hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            hwndSource?.AddHook(WndProc);

            RegisterGlobalHotKeys();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                switch (id)
                {
                    case HOTKEY_PLAY_PAUSE:
                        PauseOrPlay(UpdateAll);
                        handled = true;
                        break;
                    case HOTKEY_NEXT:
                        NextSong();
                        handled = true;
                        break;
                    case HOTKEY_PREV:
                        PreviousSong();
                        handled = true;
                        break;
                    case HOTKEY_VOL_UP:
                        AdjustVolume(+0.05);
                        handled = true;
                        break;
                    case HOTKEY_VOL_DOWN:
                        AdjustVolume(-0.05);
                        handled = true;
                        break;
                    case HOTKEY_TOGGLE_REPEAT:
                        ToggleRepeat(UpdateAll);
                        handled = true;
                        break;
                    case HOTKEY_SHUFFLE:
                        ShuffleAll();
                        handled = true;
                        break;
                }
            }
            else if (msg == WM_APPCOMMAND)
            {
                int appCommand = (short)((((long)lParam) >> 16) & 0xFFFF);
                switch (appCommand)
                {
                    case APPCOMMAND_MEDIA_PLAY_PAUSE:
                        PauseOrPlay(UpdateAll);
                        handled = true;
                        break;
                    case APPCOMMAND_MEDIA_NEXTTRACK:
                        NextSong();
                        handled = true;
                        break;
                    case APPCOMMAND_MEDIA_PREVIOUSTRACK:
                        PreviousSong();
                        handled = true;
                        break;
                    case APPCOMMAND_MEDIA_STOP:
                        if (IsPlaying)
                        {
                            PauseCurrentMusic(UpdateAll);
                        }
                        handled = true;
                        break;
                    case APPCOMMAND_VOLUME_UP:
                        AdjustVolume(+0.05);
                        handled = true;
                        break;
                    case APPCOMMAND_VOLUME_DOWN:
                        AdjustVolume(-0.05);
                        handled = true;
                        break;
                    case APPCOMMAND_VOLUME_MUTE:
                        VolumeBar.Value = 0;
                        UpdateVolume();
                        handled = true;
                        break;
                }
            }

            return IntPtr.Zero;
        }

        private void RegisterGlobalHotKeys()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            static uint VK(Key key) => (uint)KeyInterop.VirtualKeyFromKey(key);

            // Ctrl+Alt+P => Play/Pause
            hotkeysRegistered |= RegisterHotKey(hwnd, HOTKEY_PLAY_PAUSE, MOD_CONTROL | MOD_ALT, VK(Key.P));
            // Ctrl+Alt+Right => Next
            hotkeysRegistered |= RegisterHotKey(hwnd, HOTKEY_NEXT, MOD_CONTROL | MOD_ALT, VK(Key.Right));
            // Ctrl+Alt+Left => Previous
            hotkeysRegistered |= RegisterHotKey(hwnd, HOTKEY_PREV, MOD_CONTROL | MOD_ALT, VK(Key.Left));
            // Ctrl+Alt+Up => Volume Up
            hotkeysRegistered |= RegisterHotKey(hwnd, HOTKEY_VOL_UP, MOD_CONTROL | MOD_ALT, VK(Key.Up));
            // Ctrl+Alt+Down => Volume Down
            hotkeysRegistered |= RegisterHotKey(hwnd, HOTKEY_VOL_DOWN, MOD_CONTROL | MOD_ALT, VK(Key.Down));
            // Ctrl+Alt+R => Toggle Repeat
            hotkeysRegistered |= RegisterHotKey(hwnd, HOTKEY_TOGGLE_REPEAT, MOD_CONTROL | MOD_ALT, VK(Key.R));
            // Ctrl+Alt+S => Shuffle
            hotkeysRegistered |= RegisterHotKey(hwnd, HOTKEY_SHUFFLE, MOD_CONTROL | MOD_ALT, VK(Key.S));
        }

        private void UnregisterGlobalHotKeys()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            UnregisterHotKey(hwnd, HOTKEY_PLAY_PAUSE);
            UnregisterHotKey(hwnd, HOTKEY_NEXT);
            UnregisterHotKey(hwnd, HOTKEY_PREV);
            UnregisterHotKey(hwnd, HOTKEY_VOL_UP);
            UnregisterHotKey(hwnd, HOTKEY_VOL_DOWN);
            UnregisterHotKey(hwnd, HOTKEY_TOGGLE_REPEAT);
            UnregisterHotKey(hwnd, HOTKEY_SHUFFLE);
        }

        private void AdjustVolume(double delta)
        {
            double min = 0.0;
            double max = 1.0;
            double newValue = VolumeBar.Value + delta;
            if (newValue < min) newValue = min;
            if (newValue > max) newValue = max;
            VolumeBar.Value = newValue;
            UpdateVolume();
        }

        private void CleanupOsKeyboardIntegration()
        {
            if (hotkeysRegistered)
            {
                UnregisterGlobalHotKeys();
                hotkeysRegistered = false;
            }
            if (hwndSource != null)
            {
                hwndSource.RemoveHook(WndProc);
                hwndSource = null;
            }
        }

        private void HandleCommandLineArgs()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                if (args == null || args.Length < 2)
                {
                    return;
                }

                string path = args[1];
                if (Directory.Exists(path))
                {
                    PathToMusicFolder = path;
                    SaveFilePath();
                    SaveConfig();
                    UpdateAll();
                    return;
                }
                if (File.Exists(path))
                {
                    if (IsSupportedAudioFile(path))
                    {
                        if (HasInited)
                        {
                            ClearCurrentPlayer();
                        }
                        CurrentPlaylist.ResetPlaylist();
                        CurrentPlaylist.InitializePlaylist(PlaylistManager.Playlist.InitMode.FromSearch, path);
                        _ = MusicPlayingTask();
                        return;
                    }
                    if (string.Equals(Path.GetExtension(path), ".symphofs", StringComparison.OrdinalIgnoreCase))
                    {
                        ImportFavoriteSongsFromPath(path);
                        return;
                    }
                }
            }
            catch { /* ignore command line parsing errors */ }
        }

        private static bool IsSupportedAudioFile(string filePath)
        {
            string ext = Path.GetExtension(filePath);
            if (string.IsNullOrEmpty(ext)) return false;
            return string.Equals(ext, ".mp3", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".wav", StringComparison.OrdinalIgnoreCase);
        }

        private void ImportFavoriteSongsFromPath(string filePath)
        {
            try
            {
                var importedLines = File.ReadAllLines(filePath)
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToList();

                if (importedLines.Count == 0)
                {
                    System.Windows.MessageBox.Show(
                        "The selected file does not contain any favorite songs.",
                        "Favorite Configuration",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                foreach (var song in importedLines)
                {
                    FavoriteSongs.Add(song);
                }

                SaveConfig();
                UpdateHeartIconForCurrentSong();

                System.Windows.MessageBox.Show(
                    "Favorites imported successfully.",
                    "Favorite Configuration",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (IOException ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to read favorites file.\n{ex.Message}",
                    "Favorite Configuration",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Main_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private async void Main_Drop(object sender, System.Windows.DragEventArgs e)
        {
            try
            {
                if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    return;
                }
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files == null || files.Length == 0)
                {
                    return;
                }

                string first = files[0];
                if (Directory.Exists(first))
                {
                    PathToMusicFolder = first;
                    SaveFilePath();
                    SaveConfig();
                    UpdateAll();
                    return;
                }

                if (File.Exists(first))
                {
                    if (IsSupportedAudioFile(first))
                    {
                        if (HasInited)
                        {
                            ClearCurrentPlayer();
                        }
                        CurrentPlaylist.ResetPlaylist();
                        CurrentPlaylist.InitializePlaylist(PlaylistManager.Playlist.InitMode.FromSearch, first);
                        await MusicPlayingTask();
                        return;
                    }

                    if (string.Equals(Path.GetExtension(first), ".symphofs", StringComparison.OrdinalIgnoreCase))
                    {
                        ImportFavoriteSongsFromPath(first);
                        return;
                    }
                }
            }
            catch { /* ignore drag-drop issues */ }
        }
        private void FavoriteConfigButton_Click(object sender, RoutedEventArgs e)
        {
            FavoriteConfigDialog favoriteConfigDialog = new()
            {
                Owner = this
            };

            if (favoriteConfigDialog.ShowDialog() != true || favoriteConfigDialog.SelectedAction == null)
            {
                return;
            }

            switch (favoriteConfigDialog.SelectedAction)
            {
                case FavoriteConfigAction.Export:
                    ExportFavoriteSongs();
                    break;
                case FavoriteConfigAction.Import:
                    ImportFavoriteSongs();
                    break;
            }
        }

        private void ExportFavoriteSongs()
        {
            if (FavoriteSongs.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "There are no favorite songs to export.",
                    "Favorite Configuration",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            using FolderBrowserDialog folderBrowserDialog = new()
            {
                Description = "Select a folder to export your favorites"
            };

            if (folderBrowserDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            string fileName = $"favorites_{DateTime.Now:yyyyMMdd_HHmmss}.symphofs";
            string filePath = Path.Combine(folderBrowserDialog.SelectedPath, fileName);

            File.WriteAllLines(filePath, FavoriteSongs.OrderBy(song => song));

            System.Windows.MessageBox.Show(
                $"Favorites exported successfully to:\n{filePath}",
                "Favorite Configuration",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ImportFavoriteSongs()
        {
            using OpenFileDialog openFileDialog = new()
            {
                Title = "Select a Symphonia favorites file",
                Filter = "Symphonia Favorites (*.symphofs)|*.symphofs",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            string[] importedLines;

            try
            {
                importedLines = File.ReadAllLines(openFileDialog.FileName);
            }
            catch (IOException ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to read favorites file.\n{ex.Message}",
                    "Favorite Configuration",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var importedFavorites = importedLines
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            if (importedFavorites.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "The selected file does not contain any favorite songs.",
                    "Favorite Configuration",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            FavoriteImportModeDialog importModeDialog = new(importedFavorites.Count)
            {
                Owner = this
            };

            if (importModeDialog.ShowDialog() != true || importModeDialog.SelectedMode == null)
            {
                return;
            }

            if (importModeDialog.SelectedMode == FavoriteImportMode.Overwrite)
            {
                FavoriteSongs = new HashSet<string>(importedFavorites);
            }
            else if (importModeDialog.SelectedMode == FavoriteImportMode.Expand)
            {
                foreach (string song in importedFavorites)
                {
                    FavoriteSongs.Add(song);
                }
            }

            SaveConfig();
            UpdateHeartIconForCurrentSong();

            System.Windows.MessageBox.Show(
                "Favorites imported successfully.",
                "Favorite Configuration",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void SearchForFiles(string path, string arg, bool albumSearch)
        {
            Search.SearchData searchData = new(path, arg);
            Search.SearchQuery searchQuery = new();
            searchQuery.PerformSearch(async (s) =>
            {
                if (HasInited)
                {
                    ClearCurrentPlayer();
                }
                CurrentPlaylist.ResetPlaylist();
                CurrentPlaylist.InitializePlaylist(PlaylistManager.Playlist.InitMode.FromSearch, s);
                await MusicPlayingTask();
            }, searchData, true, true, true, albumSearch);
            InputBoxField.Text = string.Empty;
        }

        /// <summary>
        /// Toggles the current song's favorite status and updates the heart icon
        /// </summary>
        private void ToggleCurrentSongFavorite()
        {
            if (!HasInited)
                return;

            string currentSongName = CurrentPlaylist.CurrentSong;
            bool isNowFavorite = ToggleFavorite(currentSongName);
            
            // Update heart icon visual state
            UpdateHeartIcon(isNowFavorite);
            
            // Save configuration
            SaveConfig();
        }

        /// <summary>
        /// Updates the heart icon based on favorite status
        /// </summary>
        /// <param name="isFavorite">True if the song is favorited</param>
        private void UpdateHeartIcon(bool isFavorite)
        {
            string heartImagePath = isFavorite ? "/images/heart_inline.png" : "/images/heart_outline.png";
            HeartIcon.Source = new BitmapImage(new Uri(heartImagePath, UriKind.Relative));
        }
        #endregion

        #endregion

        #region Config Methods

        /// <summary>
        /// Opens a file dialog to browse for the music folder and sets the path
        /// </summary>
        private void SetMusicPath()
        {
            // then ask the user to browse for the music folder
            FolderBrowserDialog folderBrowserDialog = new()
            {
                Description = "Please select your music folder"
            };

            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var path = folderBrowserDialog.SelectedPath;

                // check if path is valid
                if (!System.IO.Directory.Exists(path))
                {
                    System.Windows.MessageBox.Show("Invalid path");
                    return;
                }

                // then set the path
                PathToMusicFolder = path;

                SaveFilePath();

                UpdateAll();
            }
        }

        private void SaveFilePath()
        {
            // Save the path to a text file called 'folderpath.txt'
            string filePath = System.IO.Path.Combine(ConfigPath, "folderpath.txt");

            System.IO.File.WriteAllText(filePath, PathToMusicFolder);
        }
        #endregion

        #region Updates
        private void UpdateAll()
        {
            UpdatePlayingLabel();
            UpdateControlsInteractivity();
            UpdateImages();
            UpdateAlbumArtFromCurrentSongPath();
            UpdateSeekbar();
            UpdateTimestamp();
            UpdateVolume();
            UpdateConfigs();
            UpdateHeartIconForCurrentSong();
        }

        private void UpdatePlayingLabel()
        {
            PlayingMusicLabel.FontStyle = !HasInited ? FontStyles.Italic : FontStyles.Normal;
            PlayingMusicLabel.Content = !HasInited ? defaultMusicLabel : CurrentPlaylist.CurrentSong;
            PlayingMusicLabel.Foreground = !HasInited ? new SolidColorBrush(Colors.Gray) : new SolidColorBrush(Colors.White);
        }

        private void UpdateImages()
        {
            PlayButtonIcon.Source = IsPausedOrNotValid ? new BitmapImage(new Uri(pathToPlay, UriKind.Relative)) : new BitmapImage(new Uri(pathToPause, UriKind.Relative));
            RepeatButtonIcon.Source = CurrentlyRepeating ? new BitmapImage(new Uri(pathToRepeatActive, UriKind.Relative)) : new BitmapImage(new Uri(pathToRepeatInActive, UriKind.Relative));
            MusicControlButton_2.Background = CurrentlyRepeating ? DefaultRepeatButtonColor : InactiveRepeatButtonColor;
            ToggleWindowModeIcon.Source = currentWindowMode == CurrentWindowMode.Collapsed ? new BitmapImage(new Uri(pathToExpand, UriKind.Relative)) : new BitmapImage(new Uri(pathToCollapsed, UriKind.Relative));
        }

        private void UpdateControlsInteractivity()
        {
            var active = IsPaused || IsPlaying;

            var controls = new List<System.Windows.Controls.Control>
            {
                MusicControlButton_0,
                MusicControlButton_1,
                MusicControlButton_2,
                MusicControlButton_3,
                MusicControlButton_4,
                SeekBar,
                InputBoxField,
                SeekBar,
                VolumeBar
            };

            // then, set interactivity step 1
            controls.ForEach(x => x.IsEnabled = !string.IsNullOrEmpty(PathToMusicFolder));

            // then, set interactivity for the play pause button
            controls[1].IsEnabled = active;

            // then, set seekbar interactivity
            controls[5].IsEnabled = active;

            controls[0].IsEnabled = active;

            controls[4].IsEnabled = active;
        }

        private void UpdateAlbumArtFromCurrentSongPath()
        {
            if (!HasInited)
            {
                return;
            }

            MetaImage.Source = CurrentPlaylist.CurrentSongCover();
        }

        private void UpdateSeekbar()
        {
            if (!HasInited || IsHoldingSeekbar)
            {
                return;
            }

            // update seekbar time
            var normalizedPosition = CurrentPlayingFileReader.CurrentTime;

            // Update the SeekBar value on the UI thread
            SeekBar.Value = (normalizedPosition.TotalSeconds / CurrentSongDuration.TotalSeconds) * 10;

            UpdateTimestamp();
        }

        private void UpdateSongTime()
        {
            if (!HasInited ||
                !IsHoldingSeekbar)
            {
                return;
            }

            double desiredPositionInSeconds = (SeekBar.Value / 10.0) * CurrentSongDuration.TotalSeconds;

            CurrentPlayingFileReader.CurrentTime = TimeSpan.FromSeconds(desiredPositionInSeconds);
        }

        private void UpdateVolume()
        {
            if (!HasInited)
            {
                return;
            }

            CurrentSongVolume = (float)VolumeBar.Value;

            UpdateConfigs();
        }

        private void UpdateTimestamp()
        {
            if (!HasInited)
            {
                return;
            }

            string durationString;
            string currentTimeString;

            if (CurrentSongDuration.TotalHours < 1)
            {
                durationString = CurrentSongDuration.ToString(@"mm\:ss");
            }
            else
            {
                durationString = CurrentSongDuration.ToString(@"hh\:mm\:ss");
            }

            // Ensure currentPlayingFileReader.CurrentTime doesn't exceed total duration
            if (CurrentPlayingFileReader.CurrentTime > CurrentSongDuration)
            {
                CurrentPlayingFileReader.CurrentTime = CurrentSongDuration;
            }

            if (CurrentPlayingFileReader.CurrentTime.TotalHours < 1)
            {
                currentTimeString = CurrentPlayingFileReader.CurrentTime.ToString(@"mm\:ss");
            }
            else
            {
                currentTimeString = CurrentPlayingFileReader.CurrentTime.ToString(@"hh\:mm\:ss");
            }

            Timestamp.Content = $"{currentTimeString} | {durationString}";
        }

        private void UpdateConfigs() 
        {
            CurrentlyTopMost = Topmost;

            // Determine the target height
            double targetHeight = currentWindowMode == CurrentWindowMode.Collapsed ? collapsedHeight : normalHeight;

            // Create and configure the height animation
            var heightAnimation = new DoubleAnimation
            {
                To = targetHeight,
                Duration = TimeSpan.FromSeconds(0.2), // Duration of 0.5 seconds, adjust as needed
                                                      // You can set other properties like acceleration or deceleration here if desired
            };

            // Apply the animation to the Height property
            this.BeginAnimation(Window.HeightProperty, heightAnimation); SaveConfig();
        }

        private void UpdateHeartIconForCurrentSong()
        {
            if (!HasInited)
            {
                // Show outline heart when no song is playing
                UpdateHeartIcon(false);
                return;
            }

            string currentSongName = CurrentPlaylist.CurrentSong;
            bool isFavorite = IsFavorite(currentSongName);
            UpdateHeartIcon(isFavorite);
        }
        #endregion

        private void Main_Closed(object sender, EventArgs e)
        {
            CleanupOsKeyboardIntegration();
            System.Windows.Application.Current.Shutdown();
        }

        private void Main_GotFocus(object sender, RoutedEventArgs e)
        {
            InputBoxField.Focus();
        }
    }
}
