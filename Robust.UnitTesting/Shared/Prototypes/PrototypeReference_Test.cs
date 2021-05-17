using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.UnitTesting.Shared.Prototypes
{
    public class PrototypeReference_Test : RobustUnitTest
    {
        [Prototype("holder")]
        private class ReferenceHolderPrototype : IPrototype
        {
            [DataField("id")] public string ID { get; } = default!;

            [DataField("ref")]
            public PrototypeReference<ReferencedPrototype> PrototypeReference { get; } = default!;
        }

        [Prototype("ref")]
        private class ReferencedPrototype : IPrototype
        {
            [DataField("id")] public string ID { get; } = default!;
        }

        private string PROTOTYPES = @"- type: ref
  id: refId

- type: holder
  id: holderId
  ref: refId
";

        [OneTimeSetUp]
        public void Setup()
        {
            IoCManager.Resolve<ISerializationManager>().Initialize();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.LoadString(PROTOTYPES);
        }

        [Test]
        public void PrototypeReferenceTest()
        {
            var pM = IoCManager.Resolve<IPrototypeManager>();
            Assert.That(pM.TryIndex<ReferencedPrototype>("refId", out var referencedPrototype));

            Assert.That(pM.TryIndex<ReferenceHolderPrototype>("holderId", out var referenceHolderPrototype));

            Assert.That(referenceHolderPrototype!.PrototypeReference.Prototype, Is.EqualTo(referencedPrototype));
        }
    }
}
