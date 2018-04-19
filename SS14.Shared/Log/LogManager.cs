using SS14.Shared.Interfaces.Log;
using System.Collections.Generic;

namespace SS14.Shared.Log
{
    // Sealed. New functionality should be added with handlers.
    public partial class LogManager : ILogManager
    {
        public const string ROOT = "root";
        private readonly Sawmill rootSawmill;
        public ISawmill RootSawmill => rootSawmill;

        public ISawmill GetSawmill(string name)
        {
            if (sawmills.TryGetValue(name, out var sawmill))
            {
                return sawmill;
            }

            var index = name.LastIndexOf('.');
            string parentname;
            if (index == -1)
            {
                parentname = ROOT;
            }
            else
            {
                parentname = name.Substring(0, index);
            }

            var parent = (Sawmill)GetSawmill(parentname);
            sawmill = new Sawmill(parent, name);
            sawmills[name] = sawmill;
            return sawmill;
        }

        private Dictionary<string, Sawmill> sawmills = new Dictionary<string, Sawmill>();

        public LogManager()
        {
            rootSawmill = new Sawmill(null, ROOT)
            {
                Level = LogLevel.Debug,
            };
            sawmills[ROOT] = rootSawmill;
        }
    }
}
