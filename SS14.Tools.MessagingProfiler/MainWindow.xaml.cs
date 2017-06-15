using SS14.Shared;
using SS14.Shared.GameObjects;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SS14.Tools.MessagingProfiler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MessageLoggerServer loggerServer;
        private CollectionView _logView;
        public ObservableCollection<LogItem> LogItems
        {
            get { return LogHolder.Singleton.LogItems; }
        }
        private ComponentFamily _componentFamilyFilter = ComponentFamily.Null;
        private EntityMessage _entityMessageFilter = EntityMessage.Null;
        private ComponentMessageType _componentMessageTypeFilter = ComponentMessageType.Null;
        private LogItem.LogMessageType _logMessageTypeFilter = LogItem.LogMessageType.None;
        private string _booleanFilter = "any";

        public MainWindow()
        {
            InitializeComponent();

            //dataGrid1.DataContext = LogHolder.Singleton.LogItems;
            //dataGrid1.ItemsSource = CollectionViewSource.GetDefaultView(LogItems);
            UpdateItemsView();
            SelectCFamily.ItemsSource = Enum.GetValues(typeof (ComponentFamily));
            SelectEMsgType.ItemsSource = Enum.GetValues(typeof (EntityMessage));
            SelectMsgType.ItemsSource = Enum.GetValues(typeof (ComponentMessageType));
            SelectSource.ItemsSource = Enum.GetValues(typeof(LogItem.LogMessageType));

            InitializeLogging();
        }

        private void UpdateItemsView()
        {
            _logView = CollectionViewSource.GetDefaultView(LogItems) as CollectionView;
            _logView.Filter = new Predicate<object>(MessageFilter);
            dataGrid1.ItemsSource = _logView;
        }

        private void InitializeLogging()
        {
            loggerServer = new MessageLoggerServer();
            loggerServer.Initialize();
            loggerServer.Start();
        }

        private bool MessageFilter(object item)
        {
            bool selected = false;
            LogItem i = item as LogItem;
            if (i == null)
                return selected;
            if (_entityMessageFilter == EntityMessage.Null
                && _componentFamilyFilter == ComponentFamily.Null
                && _logMessageTypeFilter == LogItem.LogMessageType.None
                && _componentMessageTypeFilter == ComponentMessageType.Null)
                selected = true;
            else
            {
                if (_booleanFilter == "any")
                {
                    if ((i.EntityMessageType == _entityMessageFilter && _entityMessageFilter != EntityMessage.Null)
                        || (i.ComponentFamily == _componentFamilyFilter && _componentFamilyFilter != ComponentFamily.Null)
                        || (i.MessageSource == _logMessageTypeFilter && _logMessageTypeFilter != LogItem.LogMessageType.None)
                        || (i.MessageType == _componentMessageTypeFilter && _componentMessageTypeFilter != ComponentMessageType.Null))
                        selected = true;
                }
                else
                {
                    if ((_entityMessageFilter == EntityMessage.Null || i.EntityMessageType == _entityMessageFilter)
                        && (_componentFamilyFilter == ComponentFamily.Null || i.ComponentFamily == _componentFamilyFilter)
                        && (_logMessageTypeFilter == LogItem.LogMessageType.None || i.MessageSource == _logMessageTypeFilter)
                        && (_componentMessageTypeFilter == ComponentMessageType.Null || i.MessageType == _componentMessageTypeFilter))
                        selected = true;
                }
            }
            return selected;
        }

        private void SelectFilterMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (ComboBoxItem) SelectFilterMode.SelectedItem;
            _booleanFilter = item.Name;
            UpdateItemsView();
        }

        private void SelectEMsgType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _entityMessageFilter = (EntityMessage)SelectEMsgType.SelectedItem;
            UpdateItemsView();
        }

        private void SelectCFamily_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _componentFamilyFilter = (ComponentFamily)SelectCFamily.SelectedItem;
            UpdateItemsView();
        }

        private void SelectSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _logMessageTypeFilter = (LogItem.LogMessageType) SelectSource.SelectedItem;
            UpdateItemsView();
        }

        private void SelectMsgType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _componentMessageTypeFilter = (ComponentMessageType) SelectMsgType.SelectedItem;
            UpdateItemsView();
        }


    }
}
