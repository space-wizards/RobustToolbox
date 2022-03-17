using Robust.Client.Graphics;
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

        /// <summary>
        ///     Enable the UI autoscale system, this will scale down the UI for lower resolutions
        /// </summary>
        [ViewVariables]
        public bool AutoScale { get; set; } = false;

        /// <summary>
        ///     Minimum resolution to start clamping autoscale to 1
        /// </summary>
        [ViewVariables]
        public Vector2i AutoScaleUpperCutoff { get; set; } = new Vector2i(1080, 720);

        /// <summary>
        ///     Maximum resolution to start clamping autos scale to autoscale minimum
        /// </summary>
        [ViewVariables]
        public Vector2i AutoScaleLowerCutoff { get; set; } = new Vector2i(520, 520);

        /// <summary>
        ///     The minimum ui scale value that autoscale will scale to
        /// </summary>
        [ViewVariables]
        public float AutoScaleMinimum { get; set; } = 0.5f;

        public override float UIScale => UIScaleSet;
        internal float UIScaleSet { get; set; }
        public override IClydeWindow Window { get; }
    }
}
