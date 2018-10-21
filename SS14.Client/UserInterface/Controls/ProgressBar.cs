using System;

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

        internal ProgressBar(Godot.ProgressBar control) : base(control)
        {
        }

        /// <summary>
        ///     True if the percentage label on top of the progress bar is visible.
        /// </summary>
        public bool PercentVisible
        {
            get => GameController.OnGodot ? SceneControl.PercentVisible : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.PercentVisible = value;
                }
            }
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.ProgressBar();
        }

        private protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(SceneControl = (Godot.ProgressBar) control);
        }
    }
}
