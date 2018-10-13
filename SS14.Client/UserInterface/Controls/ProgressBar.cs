using System;

namespace SS14.Client.UserInterface.Controls
{
    #if GODOT
    [ControlWrap(typeof(Godot.ProgressBar))]
    #endif
    public class ProgressBar : Range
    {
        #if GODOT
        private new Godot.ProgressBar SceneControl;
        #endif

        public ProgressBar()
        {
        }

        public ProgressBar(string name) : base(name)
        {
        }

        #if GODOT
        internal ProgressBar(Godot.ProgressBar control) : base(control)
        {
        }
        #endif

        /// <summary>
        ///     True if the percentage label on top of the progress bar is visible.
        /// </summary>
        public bool PercentVisible
        {
            #if GODOT
            get => SceneControl.PercentVisible;
            set => SceneControl.PercentVisible = value;
            #else
            get => default;
            set { }
            #endif
        }

        #if GODOT
        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.ProgressBar();
        }

        private protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(SceneControl = (Godot.ProgressBar)control);
        }
        #endif
    }
}
