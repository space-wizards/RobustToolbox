using SS14.Shared.GameObjects;
using SS14.Shared.Input;
using SS14.Shared.Serialization;

namespace SS14.Client.GameObjects.Components
{
    class InputComponent : Component
    {
        public override string Name => "Input";

        public string ContextName { get; set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction("context", InputContextContainer.DefaultContextName, value => ContextName = value, () => ContextName);
        }
    }
}
