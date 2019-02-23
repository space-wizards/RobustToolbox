using System.IO;
using System.Text;
using NUnit.Framework;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Client.Utility;
using SS14.Shared.Interfaces.Resources;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Utility;

namespace SS14.UnitTesting.Client.UserInterface
{
    [TestFixture]
    [TestOf(typeof(Control))]
    public class ControlTest : SS14UnitTest
    {
        public override UnitTestProject Project => UnitTestProject.Client;

        private const string Data = @"[gd_scene format=2]
[node name=""Root"" type=""Control"" index=""0""]
[node name=""Child1"" type=""Panel"" parent=""."" index=""0""]
[node name=""Child2"" type=""Label"" parent=""."" index=""1""]
[node name=""Child11"" type=""VBoxContainer"" parent=""Child1"" index=""0""]
[node name=""Child12"" type=""LineEdit"" parent=""Child1"" index=""1""]
[node name=""Child21"" type=""Button"" parent=""Child2"" index=""0""]
";

        [OneTimeSetUp]
        public void Setup()
        {
            var cache = IoCManager.Resolve<IResourceManagerInternal>();
            var data = Encoding.UTF8.GetBytes(Data);
            var stream = new MemoryStream(data);
            cache.MountStreamAt(stream, new ResourcePath("/Scenes/Test/TestScene.tscn"));
            IoCManager.Resolve<IUserInterfaceManagerInternal>().InitializeTesting();
        }

        [Test]
        public void TestManualSpawn()
        {
            var asset = (GodotAssetScene) GodotParser.Parse(new StringReader(Data));
            var control = Control.ManualSpawnFromScene(asset);

            Assert.That(control.Name, Is.EqualTo("Root"));

            var child1 = control.GetChild<Panel>("Child1");
            var child11 = child1.GetChild<VBoxContainer>("Child11");
            var child12 = child1.GetChild<LineEdit>("Child12");
            var child2 = control.GetChild<Label>("Child2");
            var child21 = child2.GetChild<Button>("Child21");
        }

        [Test]
        public void TestSceneSpawn()
        {
            var control = new TestControl();
            var child1 = control.GetChild<Panel>("Child1");
            var child11 = child1.GetChild<VBoxContainer>("Child11");
            var child12 = child1.GetChild<LineEdit>("Child12");
            var child2 = control.GetChild<Label>("Child2");
            var child21 = child2.GetChild<Button>("Child21");
        }

        [Test]
        public void TestMarginLayoutBasic()
        {
            var control = new Control {Size = new Vector2(100, 100)};
            var child = new Control
            {
                MarginRight = 5,
                MarginBottom = 5,
            };
            control.AddChild(child);
            Assert.That(child.Size, Is.EqualTo(new Vector2(5, 5)));
            Assert.That(child.Position, Is.EqualTo(Vector2.Zero));

            child.MarginTop = 3;
            child.MarginLeft = 3;
            Assert.That(child.Size, Is.EqualTo(new Vector2(2, 2)));
            Assert.That(child.Position, Is.EqualTo(new Vector2(3, 3)));
        }

        [Test]
        public void TestAnchorLayoutBasic()
        {
            var control = new Control {Size = new Vector2(100, 100)};
            var child = new Control {AnchorRight = 1, AnchorBottom = 1};
            control.AddChild(child);
            Assert.That(child.Size, Is.EqualTo(new Vector2(100, 100)));
            Assert.That(child.Position, Is.EqualTo(Vector2.Zero));

            child.AnchorLeft = 0.5f;
            Assert.That(child.Position, Is.EqualTo(new Vector2(50, 0)));
            Assert.That(child.Size, Is.EqualTo(new Vector2(50, 100)));
            child.AnchorTop = 0.5f;

            Assert.That(child.Position, Is.EqualTo(new Vector2(50, 50)));
            Assert.That(child.Size, Is.EqualTo(new Vector2(50, 50)));
        }

        [Test]
        public void TestMarginLayoutMinimumSize()
        {
            var control = new Control {Size = new Vector2(100, 100)};
            var child = new Control
            {
                CustomMinimumSize = new Vector2(50, 50),
                MarginRight = 20,
                MarginBottom = 20
            };

            control.AddChild(child);
            Assert.That(child.Size, Is.EqualTo(new Vector2(50, 50)));
            Assert.That(child.MarginRight, Is.EqualTo(20));
            Assert.That(child.MarginBottom, Is.EqualTo(20));
        }

        [Test]
        public void TestMarginAnchorLayout()
        {
            var control = new Control {Size = new Vector2(100, 100)};
            var child = new Control
            {
                MarginRight = -10,
                MarginBottom = -10,
                MarginTop = 10,
                MarginLeft = 10,
                AnchorRight = 1,
                AnchorBottom = 1
            };

            control.AddChild(child);
            Assert.That(child.Position, Is.EqualTo(new Vector2(10, 10)));
            Assert.That(child.Size, Is.EqualTo(new Vector2(80, 80)));
        }

        [Test]
        public void TestLayoutSet()
        {
            var control = new Control {Size = new Vector2(100, 100)};
            var child = new Control();

            control.AddChild(child);

            Assert.That(child.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(child.Size, Is.EqualTo(Vector2.Zero));

            child.Size = new Vector2(50, 50);
            Assert.That(child.Size, Is.EqualTo(new Vector2(50, 50)));
            Assert.That(child.Position, Is.EqualTo(Vector2.Zero));

            Assert.That(child.MarginTop, Is.EqualTo(0));
            Assert.That(child.MarginLeft, Is.EqualTo(0));
            Assert.That(child.MarginRight, Is.EqualTo(50));
            Assert.That(child.MarginBottom, Is.EqualTo(50));

            child.Position = new Vector2(50, 50);
            Assert.That(child.Size, Is.EqualTo(new Vector2(50, 50)));
            Assert.That(child.Position, Is.EqualTo(new Vector2(50, 50)));

            Assert.That(child.MarginTop, Is.EqualTo(50));
            Assert.That(child.MarginLeft, Is.EqualTo(50));
            Assert.That(child.MarginRight, Is.EqualTo(100));
            Assert.That(child.MarginBottom, Is.EqualTo(100));
        }

        /// <summary>
        ///     Test that you can't parent a control to its (grand)child.
        /// </summary>
        [Test]
        public void TestNoRecursion()
        {
            var control1 = new Control();
            var control2 = new Control();
            var control3 = new Control();

            control1.AddChild(control2);
            // Test direct parent/child.
            Assert.That(() => control2.AddChild(control1), Throws.ArgumentException);

            control2.AddChild(control3);
            // Test grand child.
            Assert.That(() => control3.AddChild(control1), Throws.ArgumentException);
        }

        private class TestControl : Control
        {
            protected override ResourcePath ScenePath => new ResourcePath("/Scenes/Test/TestScene.tscn");
        }
    }
}
