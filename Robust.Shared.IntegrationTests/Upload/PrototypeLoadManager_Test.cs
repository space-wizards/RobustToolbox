using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Upload;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Upload;

[TestFixture]
internal sealed class PrototypeLoadManager_Test : OurRobustUnitTest
{
    private const string FirstId = "first";
    private const string SecondId = "second";

    private IPrototypeManager _prototype = default!;
    private TestPrototypeLoadManager _prototypeLoad = default!;

    [OneTimeSetUp]
    public void Setup()
    {
        IoCManager.Resolve<ISerializationManager>().Initialize();
        IoCManager.Resolve<ILocalizationManager>().Initialize();
        _prototype = IoCManager.Resolve<IPrototypeManager>();
        _prototype.RegisterKind(typeof(PrototypeUploadTestPrototype));

        _prototypeLoad = new TestPrototypeLoadManager();
        IoCManager.InjectDependencies(_prototypeLoad);
        _prototypeLoad.Initialize();
    }

    [Test]
    public void TestBadPrototypeUploadIsDropped()
    {
        const string badFirstPrototype = @"- type: prototypeUploadTest
  id: first
  number: not an integer";

        const string firstPrototype = @"- type: prototypeUploadTest
  id: first
  number: 5";

        const string badSecondPrototype = @"- type: prototypeUploadTest
  id: second
  number: not an integer";

        const string invalidPathPrototype = @"- type: prototypeUploadTest
  id: second
  path: Textures/not-a-real-upload-test-file.png";

        const string secondPrototype = @"- type: prototypeUploadTest
  id: second";

        Assert.That(_prototypeLoad.TryLoad(badFirstPrototype), Is.False);
        Assert.That(_prototypeLoad.LoadedPrototypes, Is.Empty);
        Assert.That(_prototype.HasIndex<PrototypeUploadTestPrototype>(FirstId), Is.False);

        Assert.That(_prototypeLoad.TryLoad(firstPrototype), Is.True);
        Assert.That(_prototypeLoad.LoadedPrototypes, Has.Count.EqualTo(1));
        Assert.That(_prototype.HasIndex<PrototypeUploadTestPrototype>(FirstId), Is.True);
        Assert.That(_prototype.Index<PrototypeUploadTestPrototype>(FirstId).Number, Is.EqualTo(5));

        Assert.That(_prototypeLoad.TryLoad(badFirstPrototype), Is.False);
        Assert.That(_prototypeLoad.LoadedPrototypes, Has.Count.EqualTo(1));
        Assert.That(_prototype.HasIndex<PrototypeUploadTestPrototype>(FirstId), Is.True);
        Assert.That(_prototype.Index<PrototypeUploadTestPrototype>(FirstId).Number, Is.EqualTo(5));

        Assert.That(_prototypeLoad.TryLoad(badSecondPrototype), Is.False);
        Assert.That(_prototypeLoad.LoadedPrototypes, Has.Count.EqualTo(1));
        Assert.That(_prototype.HasIndex<PrototypeUploadTestPrototype>(FirstId), Is.True);
        Assert.That(_prototype.HasIndex<PrototypeUploadTestPrototype>(SecondId), Is.False);

        Assert.That(_prototypeLoad.TryLoad(invalidPathPrototype), Is.False);
        Assert.That(_prototypeLoad.LoadedPrototypes, Has.Count.EqualTo(1));
        Assert.That(_prototype.HasIndex<PrototypeUploadTestPrototype>(FirstId), Is.True);
        Assert.That(_prototype.HasIndex<PrototypeUploadTestPrototype>(SecondId), Is.False);

        Assert.That(_prototypeLoad.TryLoad(secondPrototype), Is.True);
        Assert.That(_prototypeLoad.LoadedPrototypes, Has.Count.EqualTo(2));
        Assert.That(_prototype.HasIndex<PrototypeUploadTestPrototype>(FirstId), Is.True);
        Assert.That(_prototype.HasIndex<PrototypeUploadTestPrototype>(SecondId), Is.True);
    }

    private sealed class TestPrototypeLoadManager : SharedPrototypeLoadManager
    {
        public bool TryLoad(string prototype) => TryLoadPrototypeData(prototype);

        public override void SendGamePrototype(string prototype)
        {
            TryLoadPrototypeData(prototype);
        }
    }
}

[Prototype]
internal sealed partial class PrototypeUploadTestPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public int Number { get; private set; }

    [DataField]
    public ResPath? Path { get; private set; }
}
