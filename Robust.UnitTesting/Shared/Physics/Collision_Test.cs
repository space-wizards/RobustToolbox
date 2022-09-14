// MIT License

// Copyright (c) 2020 Erin Catto

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;

namespace Robust.UnitTesting.Shared.Physics;

[TestFixture]
public sealed class Collision_Test
{
    [Test]
    public void TestCollision()
    {
        var center = new Vector2(100.0f, -50.0f);
        const float hx = 0.5f, hy = 1.5f;
        const float angle1 = 0.25f;

        // Data from issue #422. Not used because the data exceeds accuracy limits.
        //const b2Vec2 center(-15000.0f, -15000.0f);
        //const float hx = 0.72f, hy = 0.72f;
        //const float angle1 = 0.0f;

        PolygonShape polygon1 = new();
        polygon1.SetAsBox(hx, hy, center, angle1);

        const float absTol = 2.0f * float.Epsilon;
        const float relTol = 2.0f * float.Epsilon;

        Assert.That(Math.Abs(polygon1.Centroid.X - center.X), Is.LessThan(absTol + relTol * Math.Abs(center.X)));
        Assert.That(Math.Abs(polygon1.Centroid.Y - center.Y), Is.LessThan(absTol + relTol * Math.Abs(center.Y)));

        Span<Vector2> vertices = stackalloc Vector2[4];
        vertices[0] = new Vector2(center.X - hx, center.Y - hy);
        vertices[1] = new Vector2(center.X + hx, center.Y - hy);
        vertices[2] = new Vector2(center.X - hx, center.Y + hy);
        vertices[3] = new Vector2(center.X + hx, center.Y + hy);

        PolygonShape polygon2 = new();
        polygon2.SetVertices(vertices, true);

        Assert.That(Math.Abs(polygon2.Centroid.X - center.X), Is.LessThan(absTol + relTol * Math.Abs(center.X)));
        Assert.That(Math.Abs(polygon2.Centroid.Y - center.Y), Is.LessThan(absTol + relTol * Math.Abs(center.Y)));

        const float mass = 4.0f * hx * hy;
        var inertia = (mass / 3.0f) * (hx * hx + hy * hy) + mass * Vector2.Dot(center, center);

        var massData1 = FixtureSystem.GetMassData(polygon1, 1f);

        Assert.That(MathF.Abs(massData1.Center.X - center.X), Is.LessThan(absTol + relTol * Math.Abs(center.X)));
        Assert.That(MathF.Abs(massData1.Center.Y - center.Y), Is.LessThan(absTol + relTol * Math.Abs(center.Y)));
        // TODO: How the hell is this rounding enough that this test fails with the angle???
        // Assert.That(MathF.Abs(massData1.Mass - mass), Is.LessThan(20.0f * (absTol + relTol * mass)));
        // Assert.That(MathF.Abs(massData1.I - inertia), Is.LessThan(40.0f * (absTol + relTol * inertia)));

        var massData2 = FixtureSystem.GetMassData(polygon2, 1f);

        Assert.That(MathF.Abs(massData2.Center.X - center.X), Is.LessThan(absTol + relTol * Math.Abs(center.X)));
        Assert.That(MathF.Abs(massData2.Center.Y - center.Y), Is.LessThan(absTol + relTol * Math.Abs(center.Y)));
        Assert.That(MathF.Abs(massData2.Mass - mass), Is.LessThan(20.0f * (absTol + relTol * mass)));
        Assert.That(MathF.Abs(massData2.I - inertia), Is.LessThan(40.0f * (absTol + relTol * inertia)));
    }
}
