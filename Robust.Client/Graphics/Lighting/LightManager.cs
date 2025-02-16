﻿using Robust.Shared.Maths;

namespace Robust.Client.Graphics
{
    public sealed class LightManager : ILightManager
    {
        public bool Enabled { get; set; } = true;
        public bool DrawShadows { get; set; } = true;
        public bool DrawHardFov { get; set; } = true;
        public bool DrawLighting { get; set; } = true;
        public bool LockConsoleAccess { get; set; } = false;
    }
}
