using SFML.Graphics;
using SS14.Client.Graphics;
using SS14.Client.UserInterface.Components;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface
{
    /// <summary>
    ///     UI Base screen that holds all of the other controls.
    /// </summary>
    public class Screen : GuiComponent
    {
        /// <summary>
        ///     Background sprite of the entire screen.
        /// </summary>
        public Sprite Background { get; set; }

        /// <inheritdoc />
        public override void Render()
        {
            Background?.SetTransformToRect(_clientArea.Translated(_screenPos));
            Background?.Draw();

            base.Render();
        }

        /// <inheritdoc />
        protected override void OnCalcRect()
        {
            _clientArea = Box2i.FromDimensions(0, 0, Width, Height);
        }
    }
}
