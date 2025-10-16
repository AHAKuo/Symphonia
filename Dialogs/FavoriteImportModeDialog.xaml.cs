using System.Windows;

namespace Symphonia.Dialogs
{
    public enum FavoriteImportMode
    {
        Expand,
        Overwrite
    }

    public partial class FavoriteImportModeDialog : Window
    {
        public FavoriteImportModeDialog(int importedSongCount)
        {
            InitializeComponent();
            ImportSummaryText.Text = importedSongCount == 1
                ? "1 favorite will be imported."
                : $"{importedSongCount} favorites will be imported.";
        }

        public FavoriteImportMode? SelectedMode { get; private set; }

        private void ExpandButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedMode = FavoriteImportMode.Expand;
            DialogResult = true;
        }

        private void OverwriteButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedMode = FavoriteImportMode.Overwrite;
            DialogResult = true;
        }
    }
}
