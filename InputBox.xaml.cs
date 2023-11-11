using System.Windows;
using System.Windows.Controls.Primitives;

namespace Symphonia
{
    /// <summary>
    /// Interaction logic for InputBox.xaml
    /// </summary>
    public partial class InputBox : Window
    {
        public InputBox()
        {
            InitializeComponent(); // from system
        }

        private void InputBoxField_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // if the user pressed enter, we want to search
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SearchButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
        }
    }
}
