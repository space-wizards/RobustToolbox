using Robust.Shared.Timing;
using Robust.Client.Graphics;

namespace Robust.Client.UserInterface.CustomControls
{
    /// <summary>
    ///     A viewport container shows a viewport.
    ///     This one is particularly gnarly because it has the code for the main viewport stuff.
    /// </summary>
    public class MainViewportContainer : ViewportContainer
    {
        private readonly IEyeManager _eyeManager;

        public MainViewportContainer(IEyeManager eyeManager)
        {
            _eyeManager = eyeManager;
        }

        protected override void Update(FrameEventArgs args)
        {
            base.Update(args);

            if(Viewport != null)
                Viewport.Eye = _eyeManager.CurrentEye;
        }
    }
}
