using JetBrains.Annotations;
using SS14.Client.Graphics.Drawing;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.PanelContainer))]
    public class PanelContainer : Container
    {
        public const string StylePropertyPanel = "panel";

        public PanelContainer()
        {
        }

        public PanelContainer(string name) : base(name)
        {
        }

        internal PanelContainer(Godot.PanelContainer container) : base(container)
        {
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.PanelContainer();
        }

        private StyleBox _panelOverride;

        public StyleBox PanelOverride
        {
            get => _panelOverride ?? GetStyleBoxOverride("panel");
            set => SetStyleBoxOverride("panel", _panelOverride = value);
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            if (GameController.OnGodot)
            {
                return;
            }

            var style = _getStyleBox();
            style?.Draw(handle, SizeBox);
        }

        protected override void SortChildren()
        {
            base.SortChildren();

            var contentBox = _getStyleBox()?.GetContentBox(SizeBox) ?? SizeBox;

            foreach (var child in Children)
            {
                FitChildInBox(child, contentBox);
            }
        }

        protected override Vector2 CalculateMinimumSize()
        {
            if (GameController.OnGodot)
            {
                return Vector2.Zero;
            }

            var styleSize = _getStyleBox()?.MinimumSize ?? Vector2.Zero;
            var childSize = Vector2.Zero;
            foreach (var child in Children)
            {
                childSize = Vector2.ComponentMax(childSize, child.CombinedMinimumSize);
            }

            return styleSize + childSize;
        }

        [System.Diagnostics.Contracts.Pure]
        [CanBeNull]
        private StyleBox _getStyleBox()
        {
            if (_panelOverride != null)
            {
                return _panelOverride;
            }

            TryGetStyleProperty(StylePropertyPanel, out StyleBox box);
            return box;
        }
    }
}
