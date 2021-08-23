using System;

namespace Robust.Client.CEF
{
    public interface IBrowserWindow : IBrowserControl, IDisposable
    {
        bool Closed { get; }
    }
}
