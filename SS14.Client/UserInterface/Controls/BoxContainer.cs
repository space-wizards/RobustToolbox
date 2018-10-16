namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap("BoxContainer")]
    public abstract class BoxContainer : Container
    {
        public BoxContainer() : base()
        {
        }

        public BoxContainer(string name) : base(name)
        {
        }

        #if GODOT
        internal BoxContainer(Godot.BoxContainer sceneControl) : base(sceneControl)
        {
        }
        #endif

        private int? _separationOverride;

        public int? SeparationOverride
        {
            get => _separationOverride ?? GetConstantOverride("separation");
            set => SetConstantOverride("separation", _separationOverride = value);
        }
    }
}
