using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Client.Sprite;

public sealed class SpriteBoundsTest : RobustIntegrationTest
{
        private static readonly string Prototypes = @"
- type: entity
  id: spriteBoundsTest1
  components:
  - type: Sprite
    sprite: debugRotation.rsi
    layers:
    - state: direction1

- type: entity
  id: spriteBoundsTest2
  components:
  - type: Sprite
    sprite: debugRotation.rsi
    layers:
    - state: direction1
    - state: direction1
      rotation: 0.1

- type: entity
  id: spriteBoundsTest3
  components:
  - type: Sprite
    sprite: debugRotation.rsi
    layers:
    - state: direction1
    - state: direction1
      rotation: 0.1
      visible: false
";

    [Test]
    public async Task TestSpriteBounds()
    {
        var client = StartClient(new ClientIntegrationOptions {ExtraPrototypes = Prototypes});
        await client.WaitIdleAsync();
        var baseClient = client.Resolve<IBaseClient>();

        await client.WaitPost(() => baseClient.StartSinglePlayer());
        await client.WaitIdleAsync();

        var entMan = client.EntMan;
        var sys = client.System<SpriteSystem>();

        EntityUid uid1 = default;
        EntityUid uid2 = default;
        EntityUid uid3 = default;

        await client.WaitPost(() =>
        {
            uid1 = entMan.Spawn("spriteBoundsTest1");
            uid2 = entMan.Spawn("spriteBoundsTest2");
            uid3 = entMan.Spawn("spriteBoundsTest3");
        });

        var ent1 = new Entity<SpriteComponent>(uid1, entMan.GetComponent<SpriteComponent>(uid1));
        var ent2 = new Entity<SpriteComponent>(uid2, entMan.GetComponent<SpriteComponent>(uid2));
        var ent3 = new Entity<SpriteComponent>(uid3, entMan.GetComponent<SpriteComponent>(uid3));

        // None of the entities have empty bounding boxes
        var box1 = sys.GetLocalBounds(ent1);
        var box2 = sys.GetLocalBounds(ent2);
        var box3 = sys.GetLocalBounds(ent3);
        Assert.That(!box1.EqualsApprox(Box2.Empty));
        Assert.That(!box2.EqualsApprox(Box2.Empty));
        Assert.That(!box3.EqualsApprox(Box2.Empty));

        // ent2 should have a larger bb than ent1 as it has a visible rotated layer
        // ents 1 & 3 should have the same bounds, as the rotated layer is invisible in ent3
        Assert.That(box1.EqualsApprox(box3));
        Assert.That(!box1.EqualsApprox(box2));
        Assert.That(box2.EqualsApprox(ent2.Comp.Layers[1].Bounds));
        Assert.That(box2.Size.X, Is.GreaterThan(box1.Size.X));
        Assert.That(box2.Size.Y, Is.GreaterThan(box1.Size.Y));

        // Toggling layer visibility updates the bounds
        sys.LayerSetVisible(ent2!, 1, false);
        sys.LayerSetVisible(ent3!, 1, true);

        var newBox2 = sys.GetLocalBounds(ent2);
        var newBox3 = sys.GetLocalBounds(ent3);
        Assert.That(!newBox2.EqualsApprox(Box2.Empty));
        Assert.That(!newBox3.EqualsApprox(Box2.Empty));

        Assert.That(box1.EqualsApprox(newBox2));
        Assert.That(!box1.EqualsApprox(newBox3));
        Assert.That(newBox3.EqualsApprox(ent3.Comp.Layers[1].Bounds));
        Assert.That(newBox3.Size.X, Is.GreaterThan(box1.Size.X));
        Assert.That(newBox3.Size.Y, Is.GreaterThan(box1.Size.Y));

        // Changing the rotation, offset, scale, all trigger a bounds updatge
        sys.LayerSetRotation(ent3!, 1, Angle.Zero);
        var box = sys.GetLocalBounds(ent3);
        Assert.That(box1.EqualsApprox(box));

        // scale
        sys.LayerSetScale(ent3!, 1, Vector2.One * 2);
        box = sys.GetLocalBounds(ent3);
        Assert.That(box1.EqualsApprox(box.Scale(0.5f)));
        sys.LayerSetScale(ent3!, 1, Vector2.One);
        box = sys.GetLocalBounds(ent3);
        Assert.That(box1.EqualsApprox(box));

        // offset
        Assert.That(box.Center, Is.Approximately(Vector2.Zero));
        sys.LayerSetOffset(ent3!, 1, Vector2.One);
        box = sys.GetLocalBounds(ent3);
        Assert.That(box.Size.X, Is.GreaterThan(box1.Size.X));
        Assert.That(box.Size.Y, Is.GreaterThan(box1.Size.Y));
        Assert.That(box.Center.X, Is.GreaterThan(0));
        Assert.That(box.Center.Y, Is.GreaterThan(0));

        await client.WaitPost(() =>
        {
            entMan.DeleteEntity(uid1);
            entMan.DeleteEntity(uid2);
            entMan.DeleteEntity(uid3);
        });
    }
}
