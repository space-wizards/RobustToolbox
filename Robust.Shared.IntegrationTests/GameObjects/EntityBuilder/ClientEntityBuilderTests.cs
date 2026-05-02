using NUnit.Framework;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;

namespace Robust.UnitTesting.Shared.GameObjects.EntityBuilder;

internal sealed class ClientEntityBuilderTests : OurRobustUnitTest
{
    private IEntityManager _entMan = default!;

    public override UnitTestProject Project => UnitTestProject.Client;

    [OneTimeSetUp]
    public void Setup()
    {
        IoCManager.Resolve<ISerializationManager>().Initialize();

        _entMan = IoCManager.Resolve<IEntityManager>();
    }

    [Test]
    [Description("Ensure that adding SpriteComponent to a builder doesn't immediately explode.")]
    public void CreateWithSprite()
    {
        Robust.Shared.GameObjects.EntityBuilders.EntityBuilder b = null!;
        // NUnit [RequiresThread] is old and crusty and doesn't work for us.
        // So we need to isolate IoC this way instead.
        var t = new Thread(() =>
        {
            b = _entMan.EntityBuilder()
                .AddComp<SpriteComponent>();
        });

        t.Start();
        t.Join();

        _entMan.Spawn(b);
    }
}
