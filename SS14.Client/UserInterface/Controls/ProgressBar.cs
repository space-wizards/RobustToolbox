using System;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.ProgressBar))]
    public class ProgressBar : Range
    {
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
            get => GameController.OnGodot ? (bool)SceneControl.Get("percent_visible") : default;
            set
            {
                if (GameController.OnGodot) SceneControl.Set("percent_visible", value);
            }
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.ProgressBar();
        }
    }
}
