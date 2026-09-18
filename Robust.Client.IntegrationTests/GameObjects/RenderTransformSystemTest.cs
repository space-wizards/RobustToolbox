using System.Numerics;
using NUnit.Framework;
using Robust.Client.GameObjects;
using Robust.Client.Timing;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.UnitTesting;

namespace Robust.Client.IntegrationTests.GameObjects;

[TestFixture, NonParallelizable]
public sealed class RenderTransformSystemTest : RobustUnitTest
{
    public override UnitTestProject Project => UnitTestProject.Client;

    private IEntityManager _entities = default!;
    private ContainerSystem _containers = default!;
    private IConfigurationManager _configuration = default!;
    private SharedMapSystem _maps = default!;
    private TransformSystem _transforms = default!;
    private SpriteSystem _sprites = default!;
    private EyeSystem _eyes = default!;
    private IClientGameTiming _timing = default!;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _entities = IoCManager.Resolve<IEntityManager>();
        _configuration = IoCManager.Resolve<IConfigurationManager>();
        _containers = _entities.System<ContainerSystem>();
        _maps = _entities.System<SharedMapSystem>();
        _transforms = _entities.System<TransformSystem>();
        _sprites = _entities.System<SpriteSystem>();
        _eyes = _entities.System<EyeSystem>();
        _timing = IoCManager.Resolve<IClientGameTiming>();
    }

    [SetUp]
    public void Setup()
    {
        _transforms.ResetRenderTransforms();
        _timing.TickRemainder = TimeSpan.Zero;
        _timing.TickTimingAdjustment = 0f;
        ((GameTiming)_timing).FreezeTickTimingAdjustment();
        _timing.CurTick = new GameTick(_timing.CurTick.Value + 1);
        _timing.LastRealTick = _timing.CurTick;
    }

    [Test]
    public void ComponentStateApplicationInterpolatesToTheFinalSimulationPose()
    {
        var (map, mapId) = CreateMap();
        var uid = _entities.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
        var xform = _entities.GetComponent<TransformComponent>(uid);
        MakeRemote(xform);
        var state = new TransformComponentState(
            Vector2.UnitX,
            Angle.Zero,
            _entities.GetNetEntity(map),
            false,
            false);
        var nextState = new TransformComponentState(
            new Vector2(1.5f, 0f),
            Angle.FromDegrees(45),
            _entities.GetNetEntity(map),
            false,
            false);
        var handleState = new ComponentHandleState(state, nextState);

        // Component state application stores both simulation endpoints used for render interpolation.
        using (_timing.StartStateApplicationArea())
        {
            _entities.EventBus.RaiseComponentEvent(uid, xform, ref handleState);
        }

        // The real client runs prediction after state application and before FrameUpdate.
        _timing.CurTick = new GameTick(_timing.LastRealTick.Value + 12);
        SetHalfTick();
        _transforms.FrameUpdate(0f);

        Assert.Multiple(() =>
        {
            AssertVector(xform.LocalPosition, Vector2.UnitX);
            AssertVector(_transforms.GetWorldPosition(uid), Vector2.UnitX);
            AssertVector(_transforms.GetRenderWorldPosition(uid), new Vector2(0.5f, 0f));
        });
    }

    [Test]
    public void SameParentInterpolationSamplesWholeTickWithoutMutatingSimulation()
    {
        var (_, mapId) = CreateMap();
        var uid = _entities.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
        var xform = _entities.GetComponent<TransformComponent>(uid);
        MakeRemote(xform);

        var moveEvents = 0;

        void CountMove(ref MoveEvent args)
        {
            if (args.Sender == uid)
                moveEvents++;
        }

        _transforms.OnGlobalMoveEvent += CountMove;
        try
        {
            ApplyRemote(() => _transforms.SetLocalPositionRotation(uid, Vector2.One, Angle.FromDegrees(90), xform));
            var countBeforeFrame = moveEvents;
            var parent = xform.ParentUid;
            var localPosition = xform.LocalPosition;
            var localRotation = xform.LocalRotation;
            var lastModified = xform.LastModifiedTick;
            var broadphase = xform.Broadphase;

            // Render interpolation should only affect render poses, not the server transform state.
            SetTickAlpha(0f);
            _transforms.FrameUpdate(0f);

            Assert.Multiple(() =>
            {
                AssertVector(xform.LocalPosition, Vector2.One);
                AssertVector(_transforms.GetWorldPosition(uid), Vector2.One);
                AssertVector(_transforms.GetRenderWorldPosition(uid), Vector2.Zero);
                Assert.That(_transforms.GetRenderWorldRotation(uid).Degrees, Is.EqualTo(0).Within(0.001));
            });

            SetTickAlpha(0.5f);
            _transforms.FrameUpdate(0f);

            Assert.Multiple(() =>
            {
                AssertVector(xform.LocalPosition, Vector2.One);
                Assert.That(xform.ParentUid, Is.EqualTo(parent));
                Assert.That(xform.LocalRotation, Is.EqualTo(localRotation));
                Assert.That(xform.LastModifiedTick, Is.EqualTo(lastModified));
                Assert.That(xform.Broadphase, Is.EqualTo(broadphase));
                Assert.That(moveEvents, Is.EqualTo(countBeforeFrame));
                AssertVector(_transforms.GetWorldPosition(uid), Vector2.One);
                AssertVector(_transforms.GetRenderWorldPosition(uid), new Vector2(0.5f));
                Assert.That(_transforms.GetRenderWorldRotation(uid).Degrees, Is.EqualTo(45).Within(0.001));
            });

            SetTickAlpha(1f);
            _transforms.FrameUpdate(0f);

            Assert.Multiple(() =>
            {
                AssertVector(xform.LocalPosition, Vector2.One);
                AssertVector(_transforms.GetWorldPosition(uid), Vector2.One);
                AssertVector(_transforms.GetRenderWorldPosition(uid), Vector2.One);
                Assert.That(_transforms.GetRenderWorldRotation(uid).Degrees, Is.EqualTo(90).Within(0.001));
                Assert.That(moveEvents, Is.EqualTo(countBeforeFrame));
            });
        }
        finally
        {
            _transforms.OnGlobalMoveEvent -= CountMove;
        }
    }

    [TestCase(ParentTransition.GridToMap, false)]
    [TestCase(ParentTransition.MapToGrid, false)]
    [TestCase(ParentTransition.GridToGrid, false)]
    public void CrossParentTransitionsInterpolateInRenderSpace(ParentTransition transition, bool predicted)
    {
        var (map, mapId) = CreateMap();
        var gridA = _maps.CreateGridEntity(mapId).Owner;
        var gridB = _maps.CreateGridEntity(mapId).Owner;
        _transforms.SetLocalPosition(gridB, Vector2.UnitX);

        var source = transition == ParentTransition.MapToGrid ? map : gridA;
        var target = transition == ParentTransition.GridToMap ? map : gridB;
        var targetLocal = transition == ParentTransition.GridToMap ? Vector2.UnitX : Vector2.Zero;
        var uid = _entities.SpawnEntity(null, new EntityCoordinates(source, Vector2.Zero));
        var xform = _entities.GetComponent<TransformComponent>(uid);
        // Disable grid traversal so this test controls the parent transitions directly.
        xform.GridTraversal = false;
        MakeRemote(xform);

        void Move() => _transforms.SetCoordinates(
            uid,
            xform,
            new EntityCoordinates(target, targetLocal),
            Angle.Zero,
            unanchor: false);

        if (predicted)
        {
            _timing.CurTick = new GameTick(_timing.LastRealTick.Value + 1);
            Move();
        }
        else
        {
            ApplyRemote(Move);
        }

        SetTickAlpha(0f);
        _transforms.FrameUpdate(0f);
        Assert.Multiple(() =>
        {
            Assert.That(xform.ParentUid, Is.EqualTo(target));
            AssertVector(_transforms.GetWorldPosition(uid), Vector2.UnitX);
            AssertVector(_transforms.GetRenderWorldPosition(uid), Vector2.Zero);
        });

        SetTickAlpha(0.5f);
        _transforms.FrameUpdate(0f);

        Assert.Multiple(() =>
        {
            Assert.That(xform.ParentUid, Is.EqualTo(target));
            AssertVector(_transforms.GetWorldPosition(uid), Vector2.UnitX);
            AssertVector(_transforms.GetRenderWorldPosition(uid), new Vector2(0.5f, 0f));
        });

        if (predicted)
        {
            using (_timing.StartStateApplicationArea())
            {
                _transforms.SetCoordinates(uid, xform, new EntityCoordinates(source, Vector2.Zero), Angle.Zero, false);
            }

            using (_timing.StartPastPredictionArea())
            {
                _transforms.SetCoordinates(uid, xform, new EntityCoordinates(target, targetLocal), Angle.Zero, false);
            }

            Assert.Multiple(() =>
            {
                Assert.That(xform.ParentUid, Is.EqualTo(target));
                AssertVector(_transforms.GetWorldPosition(uid), Vector2.UnitX);
                AssertVector(_transforms.GetRenderWorldPosition(uid), new Vector2(0.5f, 0f),
                    "cross-parent prediction rollback must preserve the displayed midpoint");
            });
        }

        SetTickAlpha(1f);
        _transforms.FrameUpdate(0f);
        Assert.Multiple(() =>
        {
            Assert.That(xform.ParentUid, Is.EqualTo(target));
            AssertVector(_transforms.GetWorldPosition(uid), Vector2.UnitX);
            AssertVector(_transforms.GetRenderWorldPosition(uid), Vector2.UnitX);
        });
    }

    [Test]
    public void CrossParentEndpointsFollowMovingParentsAndNestedChildren()
    {
        var (map, mapId) = CreateMap();
        var parentA = _entities.SpawnEntity(null, new EntityCoordinates(map, Vector2.Zero));
        var parentB = _entities.SpawnEntity(null, new EntityCoordinates(map, Vector2.UnitX));
        var uid = _entities.SpawnEntity(null, new EntityCoordinates(parentA, new Vector2(0.5f, 0f)));
        var nested = _entities.SpawnEntity(null, new EntityCoordinates(uid, new Vector2(0.25f, 0f)));
        var xform = _entities.GetComponent<TransformComponent>(uid);
        xform.GridTraversal = false;
        MakeRemote(parentA, parentB, uid);

        ApplyRemote(() =>
        {
            _transforms.SetCoordinates(uid, xform, new EntityCoordinates(parentB, new Vector2(0.5f, 0f)), Angle.Zero, false);
            _transforms.SetLocalPositionRotation(parentA, new Vector2(0f, 0.2f), Angle.FromDegrees(90));
            _transforms.SetLocalPositionRotation(parentB, new Vector2(1f, 0.2f), Angle.FromDegrees(-90));
        });

        // The endpoints are sampled through the moving parents before the final cross-parent lerp.
        SetTickAlpha(0f);
        _transforms.FrameUpdate(0f);
        Assert.Multiple(() =>
        {
            AssertVector(_transforms.GetRenderWorldPosition(uid), new Vector2(0.5f, 0f));
            AssertVector(_transforms.GetRenderWorldPosition(nested), new Vector2(0.75f, 0f));
            AssertVector(_transforms.GetWorldPosition(uid), new Vector2(1f, -0.3f));
        });

        SetTickAlpha(0.5f);
        _transforms.FrameUpdate(0f);

        var halfRoot = new Vector2(MathF.Sqrt(0.125f), 0.1f + MathF.Sqrt(0.125f));
        var halfTarget = new Vector2(1f + MathF.Sqrt(0.125f), 0.1f - MathF.Sqrt(0.125f));
        var expected = Vector2.Lerp(halfRoot, halfTarget, 0.5f);
        var pose = _transforms.GetRenderWorldTransform(uid);
        var nestedExpected = pose.Position + pose.Rotation.RotateVec(new Vector2(0.25f, 0f));

        Assert.Multiple(() =>
        {
            AssertVector(pose.Position, expected);
            AssertVector(_transforms.GetRenderWorldPosition(nested), nestedExpected);
            AssertVector(_transforms.GetWorldPosition(uid), new Vector2(1f, -0.3f));
        });

        SetTickAlpha(1f);
        _transforms.FrameUpdate(0f);
        var finalPose = _transforms.GetRenderWorldTransform(uid);
        Assert.Multiple(() =>
        {
            AssertVector(finalPose.Position, new Vector2(1f, -0.3f));
            Assert.That(finalPose.Rotation.Degrees, Is.EqualTo(-90).Within(0.001));
            AssertVector(_transforms.GetRenderWorldPosition(nested), new Vector2(1f, -0.55f));
        });
    }

    [Test]
    public void SpriteAndEyeCoordinatesUseTheSameRenderTransform()
    {
        var (_, mapId) = CreateMap();
        var uid = _entities.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
        var xform = _entities.GetComponent<TransformComponent>(uid);
        var sprite = _entities.AddComponent<SpriteComponent>(uid);
        var eye = _entities.AddComponent<EyeComponent>(uid);
        MakeRemote(xform);

        ApplyRemote(() => _transforms.SetLocalPosition(uid, Vector2.UnitX, xform));
        SetHalfTick();
        _transforms.FrameUpdate(0f);
        _eyes.FrameUpdate(0f);

        var renderCoordinates = _transforms.GetRenderMapCoordinates((uid, xform));
        Assert.Multiple(() =>
        {
            AssertVector(_sprites.GetSpriteWorldPosition((uid, sprite, xform)), renderCoordinates.Position);
            AssertVector(eye.Eye.Position.Position, renderCoordinates.Position);
            Assert.That(eye.Eye.Position.MapId, Is.EqualTo(renderCoordinates.MapId));
            Assert.That(renderCoordinates.MapId, Is.EqualTo(mapId));
            AssertVector(renderCoordinates.Position, new Vector2(0.5f, 0f));
        });
    }

    [TestCase(-0.1f)]
    [TestCase(0.1f)]
    public void MovementAt5TpsUsesAdjustedTickPhase(float tickTimingAdjustment)
    {
        var oldTickRate = _timing.TickRate;
        var oldTimingAdjustment = _timing.TickTimingAdjustment;

        try
        {
            _timing.SetTickRateAt(5, _timing.CurTick);
            _timing.TickTimingAdjustment = tickTimingAdjustment;
            ((GameTiming) _timing).FreezeTickTimingAdjustment();
            var adjustedPeriod = (float) _timing.CalcAdjustedTickPeriod().TotalSeconds;
            const float frameTime = 1f / 119f;
            var (_, mapId) = CreateMap();
            var uid = _entities.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
            var xform = _entities.GetComponent<TransformComponent>(uid);
            MakeRemote(xform);

            var accumulator = 0f;
            var tick = 0;
            float? previousPosition = null;
            var velocities = new List<float>();

            ApplyRemote(() => _transforms.SetLocalPosition(uid, Vector2.UnitX, xform));

            // Advance with a render frame rate that does not divide the tick rate cleanly.
            for (var frame = 0; tick < 10; frame++)
            {
                accumulator += frameTime;
                while (accumulator >= adjustedPeriod)
                {
                    accumulator -= adjustedPeriod;
                    tick++;
                    if (tick >= 10)
                        break;

                    _timing.LastRealTick = new GameTick(_timing.LastRealTick.Value + 1);
                    _timing.CurTick = _timing.LastRealTick;
                    ApplyRemote(() => _transforms.SetLocalPosition(uid, new Vector2(tick + 1, 0f), xform));
                }

                if (tick >= 10)
                    break;

                _timing.TickRemainder = TimeSpan.FromSeconds(accumulator);
                _transforms.FrameUpdate(frameTime);

                var position = _transforms.GetRenderWorldPosition(uid).X;
                if (previousPosition is { } previous)
                    velocities.Add((position - previous) / frameTime);

                previousPosition = position;
            }

            var expectedVelocity = 1f / adjustedPeriod;
            Assert.Multiple(() =>
            {
                Assert.That(velocities.Min(), Is.EqualTo(expectedVelocity).Within(0.08f));
                Assert.That(velocities.Max(), Is.EqualTo(expectedVelocity).Within(0.08f));
            });
        }
        finally
        {
            _timing.SetTickRateAt(oldTickRate, _timing.CurTick);
            _timing.TickTimingAdjustment = oldTimingAdjustment;
            ((GameTiming) _timing).FreezeTickTimingAdjustment();
            _timing.TickRemainder = TimeSpan.Zero;
        }
    }

    [Test]
    public void TimingAdjustmentChangeMidTickDoesNotMoveRenderTransform()
    {
        var oldTickRate = _timing.TickRate;
        var oldTimingAdjustment = _timing.TickTimingAdjustment;

        try
        {
            _timing.SetTickRateAt(30, _timing.CurTick);
            _timing.TickTimingAdjustment = 0f;
            ((GameTiming) _timing).FreezeTickTimingAdjustment();
            var (_, mapId) = CreateMap();
            var uid = _entities.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
            var xform = _entities.GetComponent<TransformComponent>(uid);
            MakeRemote(xform);

            ApplyRemote(() => _transforms.SetLocalPosition(uid, Vector2.UnitX, xform));
            SetHalfTick();
            _transforms.FrameUpdate(0f);

            var before = _transforms.GetRenderWorldPosition(uid);
            var phaseBefore = _timing.TickPhase;

            _timing.TickTimingAdjustment = 0.1f;
            _transforms.FrameUpdate(0f);

            Assert.Multiple(() =>
            {
                Assert.That(_timing.TickPhase, Is.EqualTo(phaseBefore).Within(0.00001f));
                AssertVector(_transforms.GetRenderWorldPosition(uid), before);
            });
        }
        finally
        {
            _timing.SetTickRateAt(oldTickRate, _timing.CurTick);
            _timing.TickTimingAdjustment = oldTimingAdjustment;
            ((GameTiming) _timing).FreezeTickTimingAdjustment();
            _timing.TickRemainder = TimeSpan.Zero;
        }
    }

    [Test]
    public void TeleportsNullspaceContainersSnap()
    {
        var (mapA, mapAId) = CreateMap();
        var (mapB, _) = CreateMap();
        var uid = _entities.SpawnEntity(null, new EntityCoordinates(mapA, Vector2.Zero));
        var xform = _entities.GetComponent<TransformComponent>(uid);
        xform.GridTraversal = false;
        MakeRemote(xform);

        ApplyRemote(() => _transforms.SetCoordinates(uid, xform, new EntityCoordinates(mapB, Vector2.UnitX), Angle.Zero, false));
        AssertVector(_transforms.GetRenderWorldPosition(uid), Vector2.UnitX, "unrelated maps must snap");

        ApplyRemote(() => _transforms.SetCoordinates(uid, xform, new EntityCoordinates(mapA, Vector2.Zero), Angle.Zero, false));
        ApplyRemote(() => _transforms.SetLocalPosition(uid, Vector2.UnitX, xform));
        _transforms.SnapRenderTransform(uid);
        AssertVector(_transforms.GetRenderWorldPosition(uid), Vector2.UnitX, "explicit teleport must snap");

        ApplyRemote(() => _transforms.SetLocalPosition(uid, new Vector2(10f, 0f), xform));
        AssertVector(_transforms.GetRenderWorldPosition(uid), new Vector2(10f, 0f), "large deltas must snap");

        var containerOwner = _entities.SpawnEntity(null, new EntityCoordinates(mapA, new Vector2(10.5f, 0f)));
        var container = _containers.EnsureContainer<Container>(containerOwner, "test");
        var inserted = false;
        ApplyRemote(() => inserted = _containers.Insert(uid, container, force: true));
        Assert.That(inserted, Is.True);
        AssertVector(_transforms.GetRenderWorldPosition(uid),
            new Vector2(10.5f, 0f),
            "container transitions must snap");

        ApplyRemote(() => _containers.Remove(uid, container, reparent: false, force: true));
        ApplyRemote(() => _transforms.DetachEntity(uid, xform));
        Assert.That(xform.MapID, Is.EqualTo(MapId.Nullspace));
        Assert.That(_transforms.GetRenderWorldTransform(uid).CoordinateSpace.IsValid(), Is.False);
        Assert.That(mapAId, Is.Not.EqualTo(MapId.Nullspace));
    }

    [Test]
    public void LargeMovementSnapsWithExistingRenderState()
    {
        var (_, mapId) = CreateMap();
        var uid = _entities.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
        var xform = _entities.GetComponent<TransformComponent>(uid);
        MakeRemote(xform);

        ApplyRemote(() => _transforms.SetLocalPosition(uid, Vector2.UnitX, xform));
        SetHalfTick();
        _transforms.FrameUpdate(0f);

        Assert.Multiple(() =>
        {
            Assert.That(_transforms.TryGetRenderTransformDebugData(uid, out var data), Is.True);
            Assert.That(data.Type, Is.EqualTo(RenderInterpolationType.NetworkInterpolation));
            AssertVector(_transforms.GetRenderWorldPosition(uid), new Vector2(0.5f, 0f));
        });

        var maxDistance = _configuration.GetCVar(CVars.NetInterpMaxDistance);
        var snapTarget = new Vector2(maxDistance + 2f, 0f);
        ApplyRemote(() => _transforms.SetLocalPosition(uid, snapTarget, xform));

        Assert.Multiple(() =>
        {
            AssertVector(_transforms.GetWorldPosition(uid), snapTarget);
            AssertVector(_transforms.GetRenderWorldPosition(uid), snapTarget,
                "large movement must not be interpolated over an active render state");
            Assert.That(_transforms.TryGetRenderTransformDebugData(uid, out _), Is.False);
        });
    }

    [Test]
    public void LargeMovementSnapsWithExistingCorrection()
    {
        var (_, mapId) = CreateMap();
        var uid = _entities.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
        var xform = _entities.GetComponent<TransformComponent>(uid);

        _timing.CurTick = new GameTick(_timing.LastRealTick.Value + 2);
        _transforms.SetLocalPosition(uid, Vector2.UnitX, xform);
        _transforms.SnapRenderTransform(uid, true);
        var predictionTick = _timing.CurTick;
        _transforms.RecordPredictionSample(uid, predictionTick, 0);
        _transforms.BeginPredictionRollback(uid, predictionTick);

        using (_timing.StartStateApplicationArea())
            _transforms.SetLocalPosition(uid, Vector2.Zero, xform);

        _transforms.CompletePredictionRollback(predictionTick);
        _transforms.FinishPredictionRollback();

        Assert.That(_transforms.TryGetRenderTransformDebugData(uid, out var correction), Is.True);
        Assert.That(correction.CorrectionTranslation.LengthSquared(), Is.GreaterThan(0f));

        var maxDistance = _configuration.GetCVar(CVars.NetInterpMaxDistance);
        var snapTarget = new Vector2(maxDistance + 2f, 0f);

        using (_timing.StartStateApplicationArea())
            _transforms.SetLocalPosition(uid, snapTarget, xform);

        Assert.Multiple(() =>
        {
            AssertVector(_transforms.GetWorldPosition(uid), snapTarget);
            AssertVector(_transforms.GetRenderWorldPosition(uid),
                snapTarget,
                "large movement must snap even when a prediction correction is active");
            Assert.That(_transforms.TryGetRenderTransformDebugData(uid, out _), Is.False);
        });
    }

    [Test]
    public void LargeMovementSnapsDuringPredictionHandoff()
    {
        var (_, mapId) = CreateMap();
        var uid = _entities.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
        var xform = _entities.GetComponent<TransformComponent>(uid);

        _timing.CurTick = _timing.LastRealTick + 1;
        _transforms.SetLocalPosition(uid, Vector2.UnitX, xform);
        SetHalfTick();
        _transforms.FrameUpdate(0f);
        _transforms.EndPrediction(uid);

        var maxDistance = _configuration.GetCVar(CVars.NetInterpMaxDistance);
        var snapTarget = new Vector2(maxDistance + 2f, 0f);
        ApplyRemote(() => _transforms.SetLocalPosition(uid, snapTarget, xform));

        Assert.Multiple(() =>
        {
            AssertVector(_transforms.GetWorldPosition(uid), snapTarget);
            AssertVector(_transforms.GetRenderWorldPosition(uid),
                snapTarget,
                "large movement must snap during a prediction-to-network handoff");
            Assert.That(_transforms.TryGetRenderTransformDebugData(uid, out _), Is.False);
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ContainerInsertionRemovalSnap(bool handSlot)
    {
        var (map, mapId) = CreateMap();
        var holder = _entities.SpawnEntity(null, new EntityCoordinates(map, Vector2.UnitX));
        var item = _entities.SpawnEntity(null, new EntityCoordinates(map, Vector2.Zero));
        var holderXform = _entities.GetComponent<TransformComponent>(holder);
        var itemXform = _entities.GetComponent<TransformComponent>(item);
        MakeRemote(holderXform);
        MakeRemote(itemXform);

        BaseContainer container = handSlot
            ? _containers.EnsureContainer<ContainerSlot>(holder, "hand")
            : _containers.EnsureContainer<Container>(holder, "container");

        ApplyRemote(() => _transforms.SetLocalPosition(item, new Vector2(0.5f, 0f), itemXform));
        SetHalfTick();
        _transforms.FrameUpdate(0f);
        AssertVector(_transforms.GetRenderWorldPosition(item), new Vector2(0.25f, 0f));

        var inserted = false;
        ApplyRemote(() => inserted = _containers.Insert(item, container, force: true));
        Assert.Multiple(() =>
        {
            Assert.That(inserted, Is.True);
            Assert.That(itemXform.ParentUid, Is.EqualTo(holder));
            AssertVector(_transforms.GetWorldPosition(item), Vector2.UnitX);
            AssertVector(_transforms.GetRenderWorldPosition(item),
                Vector2.UnitX,
                "insertion must discard the active render lerp");
        });

        ApplyRemote(() => _transforms.SetLocalPosition(holder, new Vector2(1.5f, 0f), holderXform));
        SetHalfTick();
        _transforms.FrameUpdate(0f);
        AssertVector(_transforms.GetRenderWorldPosition(item), new Vector2(1.25f, 0f));

        var removed = false;
        ApplyRemote(() => removed = _containers.Remove(
            item,
            container,
            force: true,
            destination: new EntityCoordinates(map, new Vector2(2f, 0f))));
        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(itemXform.ParentUid, Is.EqualTo(map));
            AssertVector(_transforms.GetWorldPosition(item), new Vector2(2f, 0f));
            AssertVector(_transforms.GetRenderWorldPosition(item),
                new Vector2(2f, 0f),
                "removal must discard the inherited render offset");
            Assert.That(itemXform.MapID, Is.EqualTo(mapId));
        });
    }

    [Test]
    public void CompatibilityHookAllowsCrossMapInterpolation()
    {
        // z-level prep work fam.
        var (mapA, mapAId) = CreateMap();
        var (mapB, _) = CreateMap();
        var uid = _entities.SpawnEntity(null, new EntityCoordinates(mapA, Vector2.Zero));
        var child = _entities.SpawnEntity(null, new EntityCoordinates(uid, new Vector2(0.25f, 0f)));
        var xform = _entities.GetComponent<TransformComponent>(uid);
        xform.GridTraversal = false;
        MakeRemote(xform);

        void Compatible(ref RenderSpaceCompatibilityEvent args)
        {
            if ((args.First == mapA && args.Second == mapB) || (args.First == mapB && args.Second == mapA))
                args.CommonSpace = mapA;
        }

        _transforms.RenderSpaceCompatibility += Compatible;
        try
        {
            ApplyRemote(() => _transforms.SetCoordinates(uid, xform, new EntityCoordinates(mapB, Vector2.UnitX), Angle.Zero, false));
            _timing.TickRemainder = TimeSpan.FromTicks(_timing.TickPeriod.Ticks / 4);
            _transforms.FrameUpdate(0f);
            AssertVector(_transforms.GetRenderWorldPosition(uid), new Vector2(0.25f, 0f));
            AssertVector(_transforms.GetRenderWorldPosition(child), new Vector2(0.5f, 0f));
            var pose = _transforms.GetRenderWorldTransform(child);
            Assert.That(pose.CoordinateSpace, Is.EqualTo(mapA));
            Assert.That(pose.SourceRenderSpace, Is.EqualTo(mapA));
            Assert.That(pose.TargetRenderSpace, Is.EqualTo(mapB));
            Assert.That(pose.RenderSpaceAlpha, Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(_transforms.GetRenderMapCoordinates(child).MapId, Is.EqualTo(mapAId));

            SetTickAlpha(0.75f);
            _transforms.FrameUpdate(0f);
            pose = _transforms.GetRenderWorldTransform(child);
            Assert.That(pose.CoordinateSpace, Is.EqualTo(mapA), "the coordinate space must not switch mid-lerp");
            Assert.That(pose.SourceRenderSpace, Is.EqualTo(mapA));
            Assert.That(pose.TargetRenderSpace, Is.EqualTo(mapB));
            Assert.That(pose.RenderSpaceAlpha, Is.EqualTo(0.75f).Within(0.001f));
            Assert.That(_transforms.GetRenderMapCoordinates(child).MapId, Is.EqualTo(mapAId));
        }
        finally
        {
            _transforms.RenderSpaceCompatibility -= Compatible;
        }
    }

    [Test]
    public void RenderCullingBoundsIncludeTheSimulationTreeEntry()
    {
        // Basic sanity check.
        // Because xforms no longer update on frameupdate need to sanity check they still get returned by tree queries.
        var (_, mapId) = CreateMap();
        var uid = _entities.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
        var xform = _entities.GetComponent<TransformComponent>(uid);
        MakeRemote(xform);

        ApplyRemote(() => _transforms.SetLocalPosition(uid, new Vector2(1.5f, 0f), xform));
        SetHalfTick();
        _transforms.FrameUpdate(0f);

        var viewport = new Box2Rotated(new Box2(-0.1f, -0.1f, 0.85f, 0.1f), Angle.Zero, Vector2.Zero);
        var queryBounds = _transforms.GetRenderCullingBounds(mapId, viewport);
        Assert.Multiple(() =>
        {
            Assert.That(viewport.CalcBoundingBox().Contains(_transforms.GetRenderWorldPosition(uid)), Is.True);
            Assert.That(viewport.CalcBoundingBox().Contains(_transforms.GetWorldPosition(uid)), Is.False);
            Assert.That(queryBounds.Contains(_transforms.GetWorldPosition(uid)), Is.True);
        });
    }

    [TestCase(30)]
    [TestCase(60)]
    public void PredictionRollbackDoesNotDisturbConstantRenderVelocity(int tickRate)
    {
        // The real sanity check of "please god just make the entity move from point A to point B cleanly".
        var oldTickRate = _timing.TickRate;
        try
        {
            const float renderFps = 144f;
            _timing.SetTickRateAt((ushort)tickRate, _timing.CurTick);
            var tickPeriod = (float)_timing.TickPeriod.TotalSeconds;
            var frameTime = 1f / renderFps;
            const float speed = 4.5f;
            var tickDistance = speed / tickRate;
            var (_, mapId) = CreateMap();
            var uid = _entities.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
            var xform = _entities.GetComponent<TransformComponent>(uid);
            var accumulator = 0f;
            var simulationTick = 0;
            float? previousRender = null;
            var velocities = new List<float>();
            var simulationDisplacements = new List<float>();

            _timing.CurTick = _timing.LastRealTick + 1;
            _transforms.SetLocalPosition(uid, new Vector2(tickDistance, 0f), xform);
            AssertNoCorrection(uid);
            var previousSimulationEndpoint = _transforms.GetWorldPosition(uid).X;

            // Repeated rollback should update the destination without restarting visible motion.
            for (var frame = 0; simulationTick < 24; frame++)
            {
                accumulator += frameTime;

                while (accumulator >= tickPeriod)
                {
                    accumulator -= tickPeriod;
                    simulationTick++;
                    RollbackPredictionTick(simulationTick);
                    AssertNoCorrection(uid);
                    var simulationEndpoint = _transforms.GetWorldPosition(uid).X;
                    simulationDisplacements.Add(simulationEndpoint - previousSimulationEndpoint);
                    previousSimulationEndpoint = simulationEndpoint;
                }

                _timing.TickRemainder = TimeSpan.FromSeconds(accumulator);
                _transforms.FrameUpdate(frameTime);
                AssertNoCorrection(uid);

                var render = _transforms.GetRenderWorldPosition(uid).X;
                if (previousRender is { } previous && simulationTick >= 2)
                    velocities.Add((render - previous) / frameTime);

                previousRender = render;
            }

            Assert.Multiple(() =>
            {
                Assert.That(simulationDisplacements.Min(), Is.EqualTo(tickDistance).Within(0.0001f));
                Assert.That(simulationDisplacements.Max(), Is.EqualTo(tickDistance).Within(0.0001f));
                Assert.That(velocities.Min(), Is.EqualTo(speed).Within(0.0001f));
                Assert.That(velocities.Max(), Is.EqualTo(speed).Within(0.0001f));
            });

            void RollbackPredictionTick(int realTickIndex)
            {
                var previousRealTick = _timing.LastRealTick;
                var nextRealTick = previousRealTick + 1;

                using (_timing.StartStateApplicationArea())
                    _transforms.SetLocalPosition(uid, new Vector2((realTickIndex - 1) * tickDistance, 0f), xform);
                AssertNoCorrection(uid);

                xform.LastModifiedTick = previousRealTick;

                _timing.CurTick = _timing.LastRealTick = nextRealTick;
                using (_timing.StartStateApplicationArea())
                    _transforms.SetLocalPosition(uid, new Vector2(realTickIndex * tickDistance, 0f), xform);
                AssertNoCorrection(uid);

                _timing.CurTick = nextRealTick + 1;
                using (_timing.StartPastPredictionArea())
                    _transforms.SetLocalPosition(uid, new Vector2((realTickIndex + 1) * tickDistance, 0f), xform);
                AssertNoCorrection(uid);
            }
        }
        finally
        {
            _timing.SetTickRateAt(oldTickRate, _timing.CurTick);
            _timing.TickRemainder = TimeSpan.Zero;
        }
    }

    [Test]
    public void SnappedLocalMispredictionUsesCorrection()
    {
        var (_, mapId) = CreateMap();
        var uid = _entities.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
        var xform = _entities.GetComponent<TransformComponent>(uid);

        _timing.CurTick = new GameTick(_timing.LastRealTick.Value + 2);
        _transforms.SetLocalPosition(uid, Vector2.UnitX, xform);
        _transforms.SnapRenderTransform(uid, true);
        var predictionTick = _timing.CurTick;
        _transforms.RecordPredictionSample(uid, predictionTick, 0);
        _transforms.BeginPredictionRollback(uid, predictionTick);

        AssertVector(_transforms.GetWorldPosition(uid), Vector2.UnitX);
        AssertVector(_transforms.GetRenderWorldPosition(uid), Vector2.UnitX);

        using (_timing.StartStateApplicationArea())
            _transforms.SetLocalPosition(uid, Vector2.Zero, xform);

        _transforms.CompletePredictionRollback(predictionTick);
        _transforms.FinishPredictionRollback();

        Assert.Multiple(() =>
        {
            AssertVector(_transforms.GetWorldPosition(uid), Vector2.Zero);
            AssertVector(_transforms.GetRenderWorldPosition(uid), Vector2.UnitX);
            Assert.That(_transforms.TryGetRenderTransformDebugData(uid, out var data), Is.True);
            Assert.That(data.Type, Is.EqualTo(RenderInterpolationType.PredictionInterpolation));
            Assert.That(data.CorrectionTranslation.LengthSquared(), Is.GreaterThan(0f));
        });

        _transforms.FrameUpdate(CorrectionHalfLifeForTest);
        AssertVector(_transforms.GetRenderWorldPosition(uid), new Vector2(0.5f, 0f));
    }

    [Test]
    public void PredictionRollbackPreservesProgressAndUpdatesDestination()
    {
        var (_, mapId) = CreateMap();
        var uid = _entities.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
        var xform = _entities.GetComponent<TransformComponent>(uid);

        _timing.CurTick = new GameTick(_timing.LastRealTick.Value + 1);
        _transforms.SetLocalPosition(uid, Vector2.UnitX, xform);
        SetTickAlpha(0.25f);
        _transforms.FrameUpdate(0f);
        AssertVector(_transforms.GetRenderWorldPosition(uid), new Vector2(0.25f, 0f));

        using (_timing.StartStateApplicationArea())
            _transforms.SetLocalPosition(uid, Vector2.Zero, xform);
        using (_timing.StartPastPredictionArea())
            _transforms.SetLocalPosition(uid, Vector2.UnitX, xform);

        AssertVector(_transforms.GetRenderWorldPosition(uid), new Vector2(0.25f, 0f));

        SetTickAlpha(0.5f);
        _transforms.FrameUpdate(0f);
        AssertVector(_transforms.GetRenderWorldPosition(uid), new Vector2(0.5f, 0f));

        using (_timing.StartStateApplicationArea())
        {
            _transforms.SetLocalPosition(uid, Vector2.Zero, xform);
        }

        using (_timing.StartPastPredictionArea())
        {
            _transforms.SetLocalPosition(uid, new Vector2(2f, 0f), xform);
        }

        Assert.Multiple(() =>
        {
            AssertVector(xform.LocalPosition, new Vector2(2f, 0f));
            AssertVector(_transforms.GetRenderWorldPosition(uid), new Vector2(0.5f, 0f));
        });

        SetTickAlpha(0.75f);
        _transforms.FrameUpdate(0f);
        AssertVector(_transforms.GetRenderWorldPosition(uid), new Vector2(1.25f, 0f));

        SetTickAlpha(1f);
        _transforms.FrameUpdate(0f);
        AssertVector(_transforms.GetRenderWorldPosition(uid), new Vector2(2f, 0f));
    }

    private (EntityUid Uid, MapId Id) CreateMap()
    {
        var uid = _maps.CreateMap(out var id);
        return (uid, id);
    }

    private void ApplyRemote(Action action)
    {
        // Remote updates enter through state application, matching how client snapshots are applied.
        using (_timing.StartStateApplicationArea())
        {
            action();
        }
    }

    private void SetHalfTick()
    {
        SetTickAlpha(0.5f);
    }

    private void SetTickAlpha(float alpha)
    {
        _timing.TickRemainder = TimeSpan.FromTicks((long) (_timing.TickPeriod.Ticks * alpha));
    }

    private float CorrectionHalfLifeForTest => _configuration.GetCVar(CVars.NetInterpCorrectionHalfLife);

    private void AssertNoCorrection(EntityUid uid)
    {
        if (!_transforms.TryGetRenderTransformDebugData(uid, out var data))
            return;

        Assert.Multiple(() =>
        {
            Assert.That(data.CorrectionTranslation, Is.EqualTo(Vector2.Zero));
            Assert.That(data.CorrectionRotation, Is.EqualTo(Angle.Zero));
        });
    }

    private void MakeRemote(params EntityUid[] entities)
    {
        foreach (var uid in entities)
        {
            MakeRemote(_entities.GetComponent<TransformComponent>(uid));
        }
    }

    private void MakeRemote(TransformComponent xform)
    {
        // Mark the current transform as a previous server-authored endpoint.
        xform.LastModifiedTick = _timing.LastRealTick;
    }

    private static void AssertVector(Vector2 actual, Vector2 expected, string? message = null)
    {
        Assert.That(actual.X, Is.EqualTo(expected.X).Within(0.001f), message);
        Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(0.001f), message);
    }

    public enum ParentTransition : byte
    {
        GridToMap,
        MapToGrid,
        GridToGrid,
    }
}
