namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(BoxContainer))]
    public abstract class BoxContainer : Container
    {
        public BoxContainer() : base()
        {
        }

        public BoxContainer(string name) : base(name)
        {
        }

        internal BoxContainer(Godot.BoxContainer sceneControl) : base(sceneControl)
        {
        }

        private int? _separationOverride;

        public int? SeparationOverride
        {
            get => _separationOverride ?? GetConstantOverride("separation");
            set => SetConstantOverride("separation", _separationOverride = value);
        }
    }
}
