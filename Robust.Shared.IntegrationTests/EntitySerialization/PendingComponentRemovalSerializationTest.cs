using NUnit.Framework;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.UnitTesting.Shared.EntitySerialization;

[TestFixture]
internal sealed partial class PendingComponentRemovalSerializationTest : RobustIntegrationTest
{
    private const string InputComponentName = "PendingRemovalInput";
    private const string ActiveComponentName = "PendingRemovalActive";
    private const string PrototypeId = "PendingRemovalSerializationPrototype";
    private const string TestPrototype = $"""
        - type: entity
          id: {PrototypeId}
          components:
          - type: {InputComponentName}
        """;

    [Test]
    public async Task PendingComponentAfterPausedMapLoadIsNotSerialized()
    {
        var options = new ServerIntegrationOptions
        {
            Pool = false,
            ExtraPrototypes = TestPrototype,
        };
        options.BeforeRegisterComponents += () =>
        {
            var factory = IoCManager.Resolve<IComponentFactory>();
            factory.RegisterClass<PendingRemovalInputComponent>();
            factory.RegisterClass<PendingRemovalActiveComponent>();
        };
        options.BeforeStart += () =>
        {
            var systemManager = IoCManager.Resolve<IEntitySystemManager>();
            systemManager.LoadExtraSystemType<PendingRemovalSerializationSystem>();
        };

        var server = StartServer(options);
        await server.WaitIdleAsync();

        var mapSystem = server.System<SharedMapSystem>();
        var loader = server.System<MapLoaderSystem>();
        var testSystem = server.System<PendingRemovalSerializationSystem>();
        var source = string.Empty;

        await server.WaitAssertion(() =>
        {
            var map = mapSystem.CreateMap(out var mapId, runMapInit: false);
            var uid = server.EntMan.SpawnEntity(PrototypeId, new MapCoordinates(0, 0, mapId));

            // Pre-map-init entities are paused, so the active component is absent from the original save.
            Assert.That(server.EntMan.GetComponent<MetaDataComponent>(uid).EntityPaused, Is.True);
            Assert.That(server.EntMan.HasComponent<PendingRemovalActiveComponent>(uid), Is.False);

            using var writer = new StringWriter();
            Assert.That(loader.TrySaveMap(map, writer), Is.True);
            source = writer.ToString();

            server.EntMan.DeleteEntity(map);
        });

        MappingDataNode serialized = default!;
        await server.WaitAssertion(() =>
        {
            using var reader = new StringReader(source);
            Assert.That(loader.TryLoadMap(reader, nameof(PendingComponentAfterPausedMapLoadIsNotSerialized),
                out var loadedMap, out _), Is.True);

            // Loading starts the entity before restoring its paused state. This temporarily adds the active component,
            // then EntityPausedEvent queues it for deferred removal.
            Assert.That(testSystem.ActiveComponentsAdded, Is.EqualTo(1));
            Assert.That(testSystem.ActiveComponentsQueuedForRemoval, Is.EqualTo(1));

            (serialized, _) = loader.SerializeEntitiesRecursive([loadedMap!.Value.Owner]);
        });

        Assert.That(HasSerializedComponent(serialized, ActiveComponentName), Is.False);
    }

    private static bool HasSerializedComponent(MappingDataNode serialized, string componentName)
    {
        return serialized.Get<SequenceDataNode>("entities")
            .Cast<MappingDataNode>()
            .SelectMany(group => group.Get<SequenceDataNode>("entities").Cast<MappingDataNode>())
            .Any(entity => entity.TryGet<SequenceDataNode>("components", out var components)
                && components.Cast<MappingDataNode>()
                    .Any(component => component.Get<ValueDataNode>("type").Value == componentName));
    }

    [Reflect(false)]
    private sealed partial class PendingRemovalSerializationSystem : EntitySystem
    {
        public int ActiveComponentsAdded { get; private set; }
        public int ActiveComponentsQueuedForRemoval { get; private set; }

        [SubscribeLocalEvent]
        private void OnStartup(Entity<PendingRemovalInputComponent> entity, ref ComponentStartup args)
        {
            if (Paused(entity))
                return;

            EnsureComp<PendingRemovalActiveComponent>(entity);
            ActiveComponentsAdded++;
        }

        [SubscribeLocalEvent]
        private void OnPaused(Entity<PendingRemovalActiveComponent> entity, ref EntityPausedEvent args)
        {
            RemCompDeferred<PendingRemovalActiveComponent>(entity);
            ActiveComponentsQueuedForRemoval++;
        }
    }

    [ComponentProtoName(InputComponentName)]
    private sealed partial class PendingRemovalInputComponent : Component;

    [ComponentProtoName(ActiveComponentName)]
    private sealed partial class PendingRemovalActiveComponent : Component;
}
