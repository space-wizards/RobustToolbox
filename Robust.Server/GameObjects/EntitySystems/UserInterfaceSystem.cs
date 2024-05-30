using System.Collections.Generic;
using Robust.Server.GameStates;
using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects;

public sealed class UserInterfaceSystem : SharedUserInterfaceSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UserInterfaceUserComponent, ExpandPvsEvent>(OnBuiUserExpand);
    }

    private void OnBuiUserExpand(Entity<UserInterfaceUserComponent> ent, ref ExpandPvsEvent args)
    {
        var buis = ent.Comp.OpenInterfaces.Keys;

        if (buis.Count == 0)
            return;

        args.Entities ??= new List<EntityUid>(buis.Count);

        foreach (var ui in buis)
        {
            args.Entities.Add(ui);
        }
    }
}
