using Robust.Client.Interfaces.UserInterface;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Robust.Client.State.States
{
    public class LauncherConnecting : State
    {
#pragma warning disable 649
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager;
#pragma warning restore 649

        private Control _control;

        public override void Startup()
        {
            _control = new CenterContainer
            {
                Children =
                {
                    new Label {Text = "Connecting to the server, hang tight!"}
                }
            };

            _userInterfaceManager.StateRoot.AddChild(_control);

            //_control.SetAnchorAndMarginPreset(Control.LayoutPreset.Wide);
        }

        public override void Shutdown()
        {
            _control.Dispose();
        }
    }
}
