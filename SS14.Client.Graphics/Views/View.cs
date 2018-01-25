using SS14.Client.Graphics.Utility;
using SS14.Shared.Maths;
using System;
using SView = SFML.Graphics.View;

namespace SS14.Client.Graphics.Views
{
    /// <summary>
    ///     Wrapper around SFML's View type.
    /// </summary>
    public class View : IDisposable
    {
        internal readonly SView SFMLView;

        public View()
        {
            SFMLView = new SView();
        }

        public View(Box2 box)
        {
            SFMLView = new SView(box.Rect());
        }

        public View(Vector2 center, Vector2 size)
        {
            SFMLView = new SView(center.Convert(), size.Convert());
        }

        /// <summary>
        ///     Absorbs an SFML View.
        ///     Since destruction of this object disposes the internal SFML view,
        ///     this should not be called with views managed by other things.
        /// </summary>
        internal View(SView view)
        {
            SFMLView = view;
        }

        /// <summary>
        ///     Makes a new camera, copying from another one.
        /// </summary>
        /// <param name="camera">The camera to copy from.</param>
        public View(View camera)
        {
            SFMLView = new SView(camera.SFMLView);
        }

        ~View()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                SFMLView.Dispose();
            }
        }

        public Vector2 Center
        {
            get => SFMLView.Center.Convert();
            set => SFMLView.Center = value.Convert();
        }

        public Vector2 Size
        {
            get => SFMLView.Size.Convert();
            set => SFMLView.Size = value.Convert();
        }

        public Angle Rotation
        {
            get => Angle.FromDegrees(SFMLView.Rotation);
            set => SFMLView.Rotation = SFMLView.Rotation = (float)value.Degrees;
        }

        public Box2 Rectangle
        {
            get => new Box2(Center - Size / 2, Center + Size / 2);
            set => SFMLView.Reset(value.Convert());
        }

        public Box2 Viewport
        {
            get => SFMLView.Viewport.ToBox();
            set => SFMLView.Viewport = value.Convert();
        }

        public void Move(Vector2 offset)
        {
            SFMLView.Move(offset.Convert());
        }

        public void Rotate(Angle angle)
        {
            SFMLView.Rotate((float)MathHelper.RadiansToDegrees(angle));
        }

        public void Zoom(float factor)
        {
            SFMLView.Zoom(factor);
        }
    }
}
