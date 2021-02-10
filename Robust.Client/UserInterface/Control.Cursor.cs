using System;
using Robust.Client.Graphics;

namespace Robust.Client.UserInterface
{
    public partial class Control
    {
        private CursorShape _cursorShape;
        private ICursor? _customCursor;

        /// <summary>
        ///     Default common cursor shapes available in the UI.
        /// </summary>
        public enum CursorShape: byte
        {
            Arrow,
            IBeam,
            Crosshair,
            Hand,
            HResize,
            VResize,
            /// <summary>
            ///     Special cursor shape indicating that <see cref="CustomCursorShape"/> is set and being used.
            /// </summary>
            Custom,
        }

        /// <summary>
        ///     The shape the cursor will get when being over this control.
        /// </summary>
        public CursorShape DefaultCursorShape
        {
            get => _cursorShape;
            set
            {
                if (value == CursorShape.Custom)
                {
                    throw new ArgumentException(
                        "Cannot set to CursorShape.Custom directly. Set CustomCursorShape instead.");
                }

                _cursorShape = value;
                _customCursor = null;
                UserInterfaceManagerInternal.CursorChanged(this);
            }
        }

        /// <summary>
        ///     Custom cursor shape to use.
        /// </summary>
        public ICursor? CustomCursorShape
        {
            get => _customCursor;
            set
            {
                _customCursor = value;
                _cursorShape = value == null ? CursorShape.Arrow : CursorShape.Custom;

                UserInterfaceManagerInternal.CursorChanged(this);
            }
        }
    }
}
