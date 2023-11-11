using Microsoft.VisualBasic;
using Symphonia.external;
using Symphonia.scripts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
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
                    Search.SearchData searchData = new(PathToMusicFolder, InputBoxField.Text);
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
                    }, searchData, true, true, true);
                    InputBoxField.Text = string.Empty;
                }
            };
        }

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
        #endregion

        private void Main_Closed(object sender, EventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void Main_GotFocus(object sender, RoutedEventArgs e)
        {
            InputBoxField.Focus();
        }
    }
}
