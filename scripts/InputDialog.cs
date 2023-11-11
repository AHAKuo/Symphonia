using System;

namespace Symphonia.scripts
{
    internal class InputDialog
    {
        public static void ShowPrompt(Action<string> endAction)
        {
            InputBox inputBox = new();

            inputBox.SearchButton.Click += (s, e) =>
            {
                endAction?.Invoke(inputBox.InputBoxField.Text);
                inputBox.Close();
            };

            inputBox.InputBoxField.Focus();
            inputBox.ShowDialog();
        }
    }
}
