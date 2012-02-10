using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace MessagingProfiler
{
    public class LogHolder
    {
        public ObservableCollection<LogItem> LogItems = new ObservableCollection<LogItem>();

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
    }
}
