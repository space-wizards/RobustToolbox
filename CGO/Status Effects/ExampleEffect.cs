using GameObject;

namespace CGO
{
    public class ExampleEffect : StatusEffect
    {
        public ExampleEffect(uint _uid, Entity _affected) //Do not add more parameters to the constructors or bad things happen.
            : base(_uid, _affected)
        {
            name = "Example Effect";
            description = "This is an example...";
            icon = "status_example";
            isDebuff = false;
        }

        public override void OnAdd()
        {
        }

        public override void OnRemove()
        {
        }

        public override void OnUpdate()
        {
        }
    }
}
