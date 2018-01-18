using SS14.Client.UserInterface.Controls;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.CustomControls
{
    internal class PlayerListTab : TabContainer
    {
        public ListPanel PlayerList { get; }

        public PlayerListTab(Vector2i size)
            : base(size)
        {
            PlayerList = new ListPanel();
            Container.AddControl(PlayerList);
        }
    }
}
