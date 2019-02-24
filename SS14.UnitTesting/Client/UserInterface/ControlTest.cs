using System.IO;
using System.Text;
using NUnit.Framework;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Client.Utility;
using SS14.Shared.Interfaces.Resources;
using SS14.Shared.IoC;
using SS14.Shared.Utility;

namespace SS14.UnitTesting.Client.UserInterface
{
    [TestFixture]
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
        }

        [Test]
        public void TestManualSpawn()
        {
            var asset = (GodotAssetScene)GodotParser.Parse(new StringReader(Data));
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

        private class TestControl : Control
        {
            protected override ResourcePath ScenePath => new ResourcePath("/Scenes/Test/TestScene.tscn");
        }
    }
}
