using Robust.Client.Graphics;
using Robust.Shared;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface.Controls
{
    public sealed class WindowRoot : UIRoot
    {
        internal WindowRoot(IClydeWindow window)
        {
            Window = window;
        }
        public override float UIScale => UIScaleSet;
        internal float UIScaleSet { get; set; }

        /// <summary>
        /// Set after the window is resized, to batch up UI scale updates on window resizes.
        /// </summary>
        internal bool UIScaleUpdateNeeded { get; set; }

        public override IClydeWindow Window { get; }

        /// <summary>
        /// Disable automatic scaling of window <see cref="UIScale"/> based on resolution.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Disabled by default for non-main windows as those most likely are smaller popup windows,
        /// that won't make sense with the default parameters.
        /// </para>
        /// </remarks>
        /// <seealso cref="CVars.ResAutoScaleEnabled"/>
        public bool DisableAutoScaling { get; set; } = true;
    }
}
