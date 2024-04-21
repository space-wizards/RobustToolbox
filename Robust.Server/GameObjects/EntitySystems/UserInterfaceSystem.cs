using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Robust.Server.GameObjects
{
    public sealed class UserInterfaceSystem : SharedUserInterfaceSystem
    {


        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<BoundUIWrapMessage>(OnMessageReceived);

            _ignoreUIRangeQuery = GetEntityQuery<IgnoreUIRangeComponent>();
        }
    }
}
