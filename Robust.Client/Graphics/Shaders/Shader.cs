using System.Collections.Generic;

namespace Robust.Client.Graphics.Shaders
{
    public sealed class Shader
    {
        internal readonly int ClydeHandle = -1;

        // We intentionally leak shaders to work around Godot issue #24108
        // ReSharper disable once CollectionNeverQueried.Local
        private static readonly List<Shader> LeakyLeaky = new List<Shader>();

        internal Shader()
        {

        }

        internal Shader(int clydeHandle)
        {
            ClydeHandle = clydeHandle;
        }
    }
}
