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
        /// <seealso cref="StandardCursorShape"/>
        public enum CursorShape: byte
        {
            /// <summary>
            /// Corresponds to <see cref="StandardCursorShape.Arrow"/>
            /// </summary>
            Arrow,

            /// <summary>
            /// Corresponds to <see cref="StandardCursorShape.IBeam"/>
            /// </summary>
            IBeam,

            /// <summary>
            /// Corresponds to <see cref="StandardCursorShape.Text"/>
            /// </summary>
            Text = IBeam,

            /// <summary>
            /// Corresponds to <see cref="StandardCursorShape.Crosshair"/>
            /// </summary>
            Crosshair,

            /// <summary>
            /// Corresponds to <see cref="StandardCursorShape.Hand"/>
            /// </summary>
            Hand,

            /// <summary>
            /// Corresponds to <see cref="StandardCursorShape.Pointer"/>
            /// </summary>
            Pointer = Hand,

            /// <summary>
            /// Corresponds to <see cref="StandardCursorShape.HResize"/>
            /// </summary>
            HResize,

            /// <summary>
            /// Corresponds to <see cref="StandardCursorShape.EWResize"/>
            /// </summary>
            EWResize = HResize,

            /// <summary>
            /// Corresponds to <see cref="StandardCursorShape.VResize"/>
            /// </summary>
            VResize,

            /// <summary>
            /// Corresponds to <see cref="StandardCursorShape.NSResize"/>
            /// </summary>
            NSResize = VResize,

            /// <summary>
            /// Corresponds to <see cref="StandardCursorShape.Progress"/>
            /// </summary>
            Progress,

            /// <summary>
            /// Corresponds to <see cref="StandardCursorShape.NWSEResize"/>
            /// </summary>
            NWSEResize,

            /// <summary>
            /// Corresponds to <see cref="StandardCursorShape.NESWResize"/>
            /// </summary>
            NESWResize,

            /// <summary>
            /// Corresponds to <see cref="StandardCursorShape.Move"/>
            /// </summary>
            Move,

            /// <summary>
            /// Corresponds to <see cref="StandardCursorShape.NotAllowed"/>
            /// </summary>
            NotAllowed,

            /// <summary>
            /// Corresponds to <see cref="StandardCursorShape.NWResize"/>
            /// </summary>
            NWResize,

            /// <summary>
            /// Corresponds to <see cref="StandardCursorShape.NResize"/>
            /// </summary>
            NResize,

            /// <summary>
            /// Corresponds to <see cref="StandardCursorShape.NEResize"/>
            /// </summary>
            NEResize,

            /// <summary>
            /// Corresponds to <see cref="StandardCursorShape.EResize"/>
            /// </summary>
            EResize,

            /// <summary>
            /// Corresponds to <see cref="StandardCursorShape.SEResize"/>
            /// </summary>
            SEResize,

            /// <summary>
            /// Corresponds to <see cref="StandardCursorShape.SResize"/>
            /// </summary>
            SResize,

            /// <summary>
            /// Corresponds to <see cref="StandardCursorShape.SWResize"/>
            /// </summary>
            SWResize,

            /// <summary>
            /// Corresponds to <see cref="StandardCursorShape.WResize"/>
            /// </summary>
            WResize,

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
