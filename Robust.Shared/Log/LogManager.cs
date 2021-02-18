using System;
using System.Collections.Generic;
using System.Threading;

namespace Robust.Shared.Log
{
    // Sealed. New functionality should be added with handlers.
    public partial class LogManager : ILogManager, IDisposable
    {
        public const string SawmillProperty = "Sawmill";
        public const string ROOT = "root";
        private readonly Sawmill rootSawmill;
        public ISawmill RootSawmill => rootSawmill;

        private readonly Dictionary<string, Sawmill> sawmills = new();
        private readonly ReaderWriterLockSlim _sawmillsLock = new();

        public ISawmill GetSawmill(string name)
        {
            _sawmillsLock.EnterReadLock();
            try
            {
                if (sawmills.TryGetValue(name, out var sawmill))
                {
                    return sawmill;
                }
            }
            finally
            {
                _sawmillsLock.ExitReadLock();
            }

            _sawmillsLock.EnterWriteLock();
            try
            {
                return _getSawmillUnlocked(name);
            }
            finally
            {
                _sawmillsLock.ExitWriteLock();
            }
        }

        private Sawmill _getSawmillUnlocked(string name)
        {
            if (sawmills.TryGetValue(name, out var sawmill))
            {
                return sawmill;
            }

            var index = name.LastIndexOf('.');
            string parentName;
            if (index == -1)
            {
                parentName = ROOT;
            }
            else
            {
                parentName = name.Substring(0, index);
            }

            var parent = _getSawmillUnlocked(parentName);
            sawmill = new Sawmill(parent, name);
            sawmills.Add(name, sawmill);
            return sawmill;
        }

        public LogManager()
        {
            rootSawmill = new Sawmill(null, ROOT)
            {
                Level = LogLevel.Debug,
            };
            sawmills[ROOT] = rootSawmill;
        }

        public void Dispose()
        {
            foreach (Sawmill p in sawmills.Values)
            {
                p.Dispose();
            }
        }
    }
}
