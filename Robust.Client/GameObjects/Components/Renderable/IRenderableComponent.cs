﻿using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.GameObjects
{
    [Obsolete]
    public partial interface IRenderableComponent : IComponent
    {
        int DrawDepth { get; set; }
        float Bottom { get; }
        Box2 LocalAABB { get; }
        Box2 AverageAABB { get; }
        MapId MapID { get; }
    }
}
