using System;

namespace Robust.Shared.GameObjects;

public sealed class DebugExceptionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DebugExceptionOnAddComponent, ComponentAdd>((_, _, _) => throw new NotSupportedException());
        SubscribeLocalEvent<DebugExceptionInitializeComponent, ComponentInit>((_, _, _) => throw new NotSupportedException());
        SubscribeLocalEvent<DebugExceptionStartupComponent, ComponentStartup>((_, _, _) => throw new NotSupportedException());
    }
}
