using System;

namespace Robust.Shared.GameObjects;

public sealed class DebugExceptionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DebugExceptionOnAddComponent, ComponentAdd>(OnCompAdd);
        SubscribeLocalEvent<DebugExceptionInitializeComponent, ComponentInit>((_, _, _) => throw new NotSupportedException());
        SubscribeLocalEvent<DebugExceptionStartupComponent, ComponentStartup>((_, _, _) => throw new NotSupportedException());
    }

    private void OnCompAdd(EntityUid uid, DebugExceptionOnAddComponent component, ComponentAdd args)
    {
        throw new NotSupportedException();
    }
}
