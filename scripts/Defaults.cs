

using System.Windows.Media;

namespace Symphonia.scripts
{
    internal static class Defaults
    {
        #region Images
        public const string pathToPlay = "images/play.png";
        public const string pathToPause = "images/pause.png";
        public const string pathToRepeatActive = "images/repeaton.png";
        public const string pathToRepeatInActive = "images/repeatoff.png";
        #endregion

        #region Strict Settings
        public const int maxPlaylistSize = 50;
        public const int maxTimeBeforePreviousPlay = 5; // time in seconds before we restart the song instead of going to the previous one
        public const double collapsedHeight = 250; // time in seconds before we restart the song instead of going to the previous one
        public const double normalHeight = 500; // time in seconds before we restart the song instead of going to the previous one
        public const string defaultMusicLabel = "Not Playing...";
        #endregion

        #region Colors
        public static System.Windows.Media.Brush DefaultRepeatButtonColor;
        public static System.Windows.Media.Brush InactiveRepeatButtonColor;
        #endregion

        public static void InitDefaults(MainWindow w)
        {
            DefaultRepeatButtonColor = w.MusicControlButton_2.Background;
            InactiveRepeatButtonColor = new SolidColorBrush(Colors.Gray);
        }
    }
}
