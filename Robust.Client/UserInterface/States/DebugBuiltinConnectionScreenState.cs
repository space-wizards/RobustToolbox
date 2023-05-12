using System;
using Robust.Client.State;
using Robust.Client.UserInterface.Screens;
using Robust.Shared.ContentPack;

namespace Robust.Client.UserInterface.States;

[ContentAccessAllowed]
public sealed class DebugBuiltinConnectionScreenState : State.State
{
    protected override Type? LinkedScreenType { get; } = typeof(DebugBuiltinConnectionScreen);

    protected override void Startup()
    {
    }

    protected override void Shutdown()
    {
    }
}
