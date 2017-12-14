using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.Console
{
    interface IClientConsole : IDisposable
    {
        /// <summary>
        ///     Initializes the console into a useable state.
        /// </summary>
        void Initialize();

        /// <summary>
        ///     Resets the console to a post-initialized state.
        /// </summary>
        void Reset();
    }
}
