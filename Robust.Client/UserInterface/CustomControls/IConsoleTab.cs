using System;
using Robust.Client.UserInterface.Controls;

namespace Robust.Client.UserInterface.CustomControls;

internal interface IConsoleTab
{
    LineEdit CommandBar { get; }
    event Action<IConsoleTab> Focused;
}

