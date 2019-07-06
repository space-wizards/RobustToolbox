using System;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics.Shaders
{
    public abstract class ShaderInstance : IDisposable
    {
        public abstract void SetParameter(string name, float value);
        public abstract void SetParameter(string name, Vector2 value);
        public abstract void SetParameter(string name, Vector3 value);
        public abstract void SetParameter(string name, Vector4 value);

        public abstract void SetParameter(string name, int value);
        public abstract void SetParameter(string name, Vector2i value);

        public abstract void SetParameter(string name, bool value);

        public abstract void SetParameter(string name, in Matrix3 matrix);
        public abstract void SetParameter(string name, in Matrix4 matrix);

        public abstract void SetParameter(string name, Texture texture);

        public abstract void Dispose();
    }
}
