using System.Windows;

namespace Symphonia.Dialogs
{
    public enum FavoriteConfigAction
    {
        Export,
        Import
    }

    public partial class FavoriteConfigDialog : Window
    {
        public FavoriteConfigDialog()
        {
            InitializeComponent();
        }

        public FavoriteConfigAction? SelectedAction { get; private set; }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedAction = FavoriteConfigAction.Export;
            DialogResult = true;
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedAction = FavoriteConfigAction.Import;
            DialogResult = true;
        }
    }
}
