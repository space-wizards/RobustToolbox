using SS14.Server.AI;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Serialization;
using SS14.Shared.ViewVariables;

namespace SS14.Server.GameObjects.Components
{
    public class AiControllerComponent : Component, IMoverComponent
    {
        private string _logicName;
        private float _visionRadius;

        public override string Name => "AiController";

        public string LogicName => _logicName;
        public AiLogicProcessor Processor { get; set; }

        public float VisionRadius
        {
            get => _visionRadius;
            set => _visionRadius = value;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _logicName, "logic", null);
            serializer.DataField(ref _visionRadius, "vision", 8.0f);
        }
    }
}
