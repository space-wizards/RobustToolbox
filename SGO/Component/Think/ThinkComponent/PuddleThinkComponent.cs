using ServerServices;

namespace SGO.Component.Think.ThinkComponent
{
    public class PuddleThinkComponent : ThinkComponent
    {
        public override void OnBump(object sender, params object[] list)
        {
            base.OnBump(sender, list);
            LogManager.Log("Puddle Bumped!");
        }
    }
}