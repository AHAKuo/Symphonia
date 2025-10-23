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
using System.Windows.Controls;
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
        private Storyboard scrollingStoryboard;
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
            
            // Update scrolling animation
            Dispatcher.BeginInvoke(new Action(() => UpdateScrollingAnimation()), System.Windows.Threading.DispatcherPriority.Loaded);
        }
        
        private void UpdateScrollingAnimation()
        {
            // Stop any existing animation
            scrollingStoryboard?.Stop();
            scrollingStoryboard = null;
            
            // Reset position
            Canvas.SetLeft(PlayingMusicLabel, 0);
            
            // Force measure to get actual text width
            PlayingMusicLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double textWidth = PlayingMusicLabel.DesiredSize.Width;
            double containerWidth = PlayingMusicLabelContainer.ActualWidth;
            
            // Only scroll if text is wider than container
            if (textWidth > containerWidth && containerWidth > 0)
            {
                // Create scrolling animation
                scrollingStoryboard = new Storyboard();
                scrollingStoryboard.RepeatBehavior = RepeatBehavior.Forever;
                
                var scrollAnimation = new DoubleAnimationUsingKeyFrames();
                
                // Start position (centered or from right)
                scrollAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(containerWidth, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
                
                // Pause at start
                scrollAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(containerWidth, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2))));
                
                // Scroll to the left
                double scrollDistance = textWidth + containerWidth;
                double scrollDuration = scrollDistance / 50; // 50 pixels per second
                scrollAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(-textWidth, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2 + scrollDuration))));
                
                // Pause at end
                scrollAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(-textWidth, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(4 + scrollDuration))));
                
                Storyboard.SetTarget(scrollAnimation, PlayingMusicLabel);
                Storyboard.SetTargetProperty(scrollAnimation, new PropertyPath("(Canvas.Left)"));
                
                scrollingStoryboard.Children.Add(scrollAnimation);
                scrollingStoryboard.Begin();
            }
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
            // Stop scrolling animation
            scrollingStoryboard?.Stop();
            scrollingStoryboard = null;
            
            System.Windows.Application.Current.Shutdown();
        }

        private void Main_GotFocus(object sender, RoutedEventArgs e)
        {
            InputBoxField.Focus();
        }
    }
}
