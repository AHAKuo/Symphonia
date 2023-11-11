

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
        public const string pathToCollapsed = "images/collapse.png";
        public const string pathToExpand = "images/expand.png";
        #endregion

        #region Strict Settings
        public const int maxPlaylistSize = 50;
        public const int maxTimeBeforePreviousPlay = 5; // time in seconds before we restart the song instead of going to the previous one
        public const double collapsedHeight = 310; // time in seconds before we restart the song instead of going to the previous one
        public const double normalHeight = 500; // time in seconds before we restart the song instead of going to the previous one
        public const string defaultMusicLabel = "Not Playing...";
        public const string defaultFont = "Times New Roman";
        public const string defaultMessageBoxCaption = "Symphonia";
        public static string[] supportedFormats = new string[] { "*.mp3", "*.wav" };
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
