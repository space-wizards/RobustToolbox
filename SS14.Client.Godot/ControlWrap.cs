using Godot;
using System;

namespace SS14.Client.GodotGlue
{
    /// <summary>
    ///     Wraps a Godot control so we get access to its virtual functions.
    /// </summary>
    public class ControlWrap : Godot.Control
    {
        public Func<Vector2> GetMinimumSizeOverride;
        public Func<Vector2, bool> HasPointOverride;
        public Action DrawOverride;

        public override Vector2 _GetMinimumSize()
        {
            return GetMinimumSizeOverride?.Invoke() ?? new Vector2();
        }

        public override bool HasPoint(Vector2 point)
        {
            return HasPointOverride?.Invoke(point) ?? false;
        }

        public override void _Draw()
        {
            DrawOverride?.Invoke();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            GetMinimumSizeOverride = null;
            HasPointOverride = null;
            DrawOverride = null;
        }

    }
}
