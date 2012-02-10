using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MessagingProfiler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MessageLoggerServer loggerServer;
        public List<LogItem> logItems = new List<LogItem>();

        public MainWindow()
        {
            InitializeComponent();

            dataGrid1.DataContext = LogHolder.Singleton.LogItems;

            InitializeLogging();
        }

        private void InitializeLogging()
        {
            loggerServer = new MessageLoggerServer();
            loggerServer.Initialize();
            loggerServer.Start();
        }
    }
}
