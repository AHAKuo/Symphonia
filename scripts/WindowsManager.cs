using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symphonia.scripts
{
    /// <summary>
    /// Handles constant.
    /// </summary>
    internal class WindowsManager
    {
        public static MainWindow mainWindowInstance; // set by the main menu on start.

        public static void InitAll(MainWindow mainWindow)
        {
            mainWindowInstance = mainWindow;
        }
    }
}
