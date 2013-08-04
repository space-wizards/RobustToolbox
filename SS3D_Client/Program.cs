using System;
using System.Windows.Forms;

namespace SS13
{
    public class Program
    {
        /************************************************************************/
        /* program starts here                                                  */
        /************************************************************************/

        [STAThread]
        private static void Main()
        {
            Application.Run(new MainWindow());
        }
    }
}