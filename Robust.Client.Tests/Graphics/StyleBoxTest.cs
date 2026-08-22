using System;
using System.Collections.Generic;
using System.Numerics;
using NUnit.Framework;
using Robust.Client.Graphics;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.Tests.Graphics
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    [TestOf(typeof(StyleBox))]
    internal sealed class StyleBoxTest
    {
        [Test]
        public void TestGetEnvelopBox()
        {
            var styleBox = new StyleBoxFlat();

            Assert.That(
                styleBox.GetEnvelopBox(Vector2.Zero, new Vector2(50, 50), 1),
                Is.EqualTo(new UIBox2(0, 0, 50, 50)));

            styleBox.ContentMarginLeftOverride = 3;
            styleBox.ContentMarginTopOverride = 5;
            styleBox.ContentMarginRightOverride = 7;
            styleBox.ContentMarginBottomOverride = 11;

            Assert.That(
                styleBox.GetEnvelopBox(Vector2.Zero, new Vector2(50, 50), 1),
                Is.EqualTo(new UIBox2(0, 0, 60, 66)));

            Assert.That(
                styleBox.GetEnvelopBox(new Vector2(10, 10), new Vector2(50, 50), 1),
                Is.EqualTo(new UIBox2(10, 10, 70, 76)));

            Assert.That(
                styleBox.GetEnvelopBox(new Vector2(10, 10), new Vector2(50, 50), 2.0f),
                Is.EqualTo(new UIBox2(10, 10, 80, 92)));
        }

        [Test]
        public void TestGetContentBoxClampsWhenMarginsExceedBaseBox()
        {
            var styleBox = new StyleBoxFlat
            {
                ContentMarginLeftOverride = 10,
                ContentMarginTopOverride = 20,
                ContentMarginRightOverride = 30,
                ContentMarginBottomOverride = 40,
            };

            var contentBox = styleBox.GetContentBox(new UIBox2(0, 0, 5, 5), 1);

            Assert.That(contentBox, Is.EqualTo(new UIBox2(10, 20, 10, 20)));
        }

        [Test]
        public void TestTextureDrawClampsAsymmetricMarginsOnSmallBox()
        {
            var texture = new TestTexture(new Vector2i(64, 64));
            var handle = new TestDrawingHandle(texture);
            var styleBox = new StyleBoxTexture
            {
                Texture = texture,
                PatchMarginLeft = 30,
                PatchMarginTop = 20,
                PatchMarginRight = 10,
                PatchMarginBottom = 5,
                Padding = new Thickness(10),
                ExpandMarginLeft = -10,
                ExpandMarginTop = -10,
                ExpandMarginRight = -10,
                ExpandMarginBottom = -10,
            };

            Assert.DoesNotThrow(() => styleBox.Draw(handle, new UIBox2(0, 0, 5, 5), 1));
            // If we have negatively sized boxes then 0 draw calls emitted.
            Assert.That(handle.Rects, Is.Empty);
        }

        private sealed class TestTexture(Vector2i size) : Texture(size)
        {
            public override Color GetPixel(int x, int y) => default;
        }

        private sealed class TestDrawingHandle(Texture white) : DrawingHandleScreen(white)
        {
            public List<UIBox2> Rects { get; } = new();

            public override void SetTransform(in Matrix3x2 matrix)
            {
            }

            public override Matrix3x2 GetTransform() => Matrix3x2.Identity;

            public override void UseShader(ShaderInstance? shader)
            {
            }

            public override ShaderInstance? GetShader() => null;

            public override void DrawPrimitives(DrawPrimitiveTopology primitiveTopology, Texture texture,
                ReadOnlySpan<DrawVertexUV2DColor> vertices)
            {
            }

            public override void DrawPrimitives(DrawPrimitiveTopology primitiveTopology, Texture texture,
                ReadOnlySpan<ushort> indices, ReadOnlySpan<DrawVertexUV2DColor> vertices)
            {
            }

            public override void DrawCircle(Vector2 position, float radius, Color color, bool filled = true)
            {
            }

            public override void DrawLine(Vector2 from, Vector2 to, Color color)
            {
            }

            public override void RenderInRenderTarget(IRenderTarget target, Action a, Color? clearColor)
            {
            }

            public override void DrawTexture(Texture texture, Vector2 position, Color? modulate = null)
            {
            }

            public override void DrawRect(UIBox2 rect, Color color, bool filled = true)
            {
                Rects.Add(rect);
            }

            public override void DrawTextureRectRegion(Texture texture, UIBox2 rect, UIBox2? subRegion = null,
                Color? modulate = null)
            {
                Rects.Add(rect);
            }

            public override void DrawEntity(EntityUid entity, Vector2 position, Vector2 scale, Angle? worldRot,
                Angle eyeRotation = default, Direction? overrideDirection = null, SpriteComponent? sprite = null,
                TransformComponent? xform = null, SharedTransformSystem? xformSystem = null)
            {
            }
        }
    }
}
