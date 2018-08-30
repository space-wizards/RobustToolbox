namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.ProgressBar))]
    public class ProgressBar : Range
    {
        private new Godot.ProgressBar SceneControl;

        public ProgressBar()
        {
        }

        public ProgressBar(string name) : base(name)
        {
        }

        public ProgressBar(Godot.ProgressBar control) : base(control)
        {
        }

        /// <summary>
        ///     True if the percentage label on top of the progress bar is visible.
        /// </summary>
        public bool PercentVisible
        {
            get => SceneControl.PercentVisible;
            set => SceneControl.PercentVisible = value;
        }

        protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.ProgressBar();
        }

        protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(SceneControl = (Godot.ProgressBar)control);
        }
    }
}
