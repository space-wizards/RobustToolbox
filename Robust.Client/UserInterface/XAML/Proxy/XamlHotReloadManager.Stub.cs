#if !TOOLS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Robust.Shared.Asynchronous;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Robust.Client.UserInterface.XAML.Proxy
{
    /// <summary>
    /// A stub implementation of XamlHotReloadManager.
    ///
    /// Its behavior is to do nothing.
    /// </summary>
    internal sealed class XamlHotReloadManager : IXamlHotReloadManager
    {
        /// <summary>
        /// Do nothing.
        /// </summary>
        public void Initialize()
        {
        }
    }
}
#endif
