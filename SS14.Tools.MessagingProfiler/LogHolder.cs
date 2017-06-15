using System.Collections.ObjectModel;

namespace SS14.Tools.MessagingProfiler
{
    public class LogHolder
    {
        public ObservableCollection<LogItem> LogItems = new ObservableCollection<LogItem>();
        private object _lock;
        private int _nextId = 0;
        public int NextId
        {
            get { return _nextId++; }
        }

        public static LogHolder Singleton
        {
            get
            {
                if (_singleton == null)
                    _singleton = new LogHolder();
                return _singleton;
            }
            set
            {}
        }
        private static LogHolder _singleton;

        public void Add(LogItem i)
        {
            lock (_lock)
            {
                LogItems.Add(i);
            }
        }
    }
}
