namespace SS14.Client.GameObjects
{
    public class ExampleAction : PlayerAction
    {
        public ExampleAction(uint _uid, PlayerActionComp _parent)
            : base(_uid, _parent)
        {
            name = "Stab";
            description = "Stab someone with one of the many hidden knifes you carry around.";
            icon = "action_stab";
        }
    }
}