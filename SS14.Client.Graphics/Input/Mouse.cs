using SS14.Client.Graphics.Utility;
using SS14.Shared.Maths;
using SMouse = SFML.Window.Mouse;

namespace SS14.Client.Graphics.Input
{
    public static class Mouse
    {
        // MOUSE BUTTONS
        public enum Button
        {
            // Must match SFML ones to allow for casts.
            Left,
            Right,
            Middle,
            // Extra mouse buttons.
            XButton1,
            XButton2,
            ButtonCount
        }

        internal static Button Convert(this SMouse.Button button)
        {
            return (Button)button;
        }

        internal static SMouse.Button Convert(this Button button)
        {
            return (SMouse.Button)button;
        }

        // MOUSE WHEELS
        public enum Wheel
        {
            // Must match SFML for casts.
            VerticalWheel,
            HorizontalWheel
        }

        internal static Wheel Convert(this SMouse.Wheel button)
        {
            return (Wheel)button;
        }

        internal static SMouse.Wheel Convert(this Wheel button)
        {
            return (SMouse.Wheel)button;
        }

        public static bool IsButtonPressed(Button button) => SMouse.IsButtonPressed(button.Convert());

        /// <summary>
        /// Gets the current mouse position, relative to the DESKTOP, not the current window.
        /// </summary>
        public static Vector2i GetDesktopPosition()
        {
            return SMouse.GetPosition().Convert();
        }

        public static void SetDesktopPosition(Vector2i newPosition)
        {
            SMouse.SetPosition(newPosition.Convert());
        }
    }
}
