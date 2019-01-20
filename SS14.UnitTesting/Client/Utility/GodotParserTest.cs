using System.IO;
using System.Linq;
using NUnit.Framework;
using SS14.Client.Utility;

namespace SS14.UnitTesting.Client.Utility
{
    [Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
    [TestFixture]
    [TestOf(typeof(GodotParser))]
    public class GodotParserTest
    {
        [Test]
        public void Test()
        {
            const string data = @"[gd_scene load_steps=2 format=2]

[ext_resource path=""res://Engine/Scenes/SS14Window/SS14Window.tscn"" type=""PackedScene"" id=1]

[node name=""SS14Window"" index=""0"" instance=ExtResource( 1 )]
rect_clip_content = false

[node name=""Contents"" parent=""."" index=""0""]
rect_clip_content = false

[node name=""HSplitContainer"" type=""HSplitContainer"" parent=""Contents"" index=""0""]
size_flags_vertical = 0
collapsed = false

[node name=""Recipes"" type=""VBoxContainer"" parent=""Contents/HSplitContainer"" index=""0""]
rect_pivot_offset = Vector2( 0, 0 )
_sections_unfolded = [ ""Grow Direction"", ""Size Flags"" ]

[node name=""Search"" type=""LineEdit"" parent=""Contents/HSplitContainer/Recipes"" index=""0""]
[node name=""Tree"" type=""Tree"" parent=""Contents/HSplitContainer/Recipes"" index=""1""]
[node name=""Guide"" type=""VBoxContainer"" parent=""Contents/HSplitContainer"" index=""1""]
[node name=""Info"" type=""HBoxContainer"" parent=""Contents/HSplitContainer/Guide"" index=""0""]
[node name=""TextureRect"" type=""TextureRect"" parent=""Contents/HSplitContainer/Guide/Info"" index=""0""]
[node name=""Label"" type=""Label"" parent=""Contents/HSplitContainer/Guide/Info"" index=""1""]
[node name=""Label"" type=""Label"" parent=""Contents/HSplitContainer/Guide"" index=""1""]
[node name=""StepsList"" type=""ItemList"" parent=""Contents/HSplitContainer/Guide"" index=""2""]
[node name=""Buttons"" type=""HBoxContainer"" parent=""Contents/HSplitContainer/Guide"" index=""3""]
[node name=""BuildButton"" type=""Button"" parent=""Contents/HSplitContainer/Guide/Buttons"" index=""0""]
[node name=""EraseButton"" type=""Button"" parent=""Contents/HSplitContainer/Guide/Buttons"" index=""1""]
[node name=""Header"" parent=""."" index=""1""]
rect_clip_content = false

[node name=""Header Text"" parent=""Header"" index=""0""]
rect_clip_content = false

[node name=""CloseButton"" parent=""Header"" index=""1""]
rect_clip_content = false
";
            var asset = (GodotAssetScene) GodotParser.Parse(new StringReader(data));

            Assert.That(asset.ExtResources, Has.Exactly(1).Items);
            Assert.That(asset.ExtResources[0].Type, Is.EqualTo("PackedScene"));

            Assert.That(asset.RootNode.Name, Is.EqualTo("SS14Window"));
            Assert.That(asset.RootNode.Instance, Is.EqualTo(new GodotAsset.TokenExtResource(1)));
            Assert.That(asset.Nodes.Where(n => n.Parent == "."), Has.Exactly(2).Items);
            Assert.That(asset.Nodes.Last(n => n.Parent == ".").Name, Is.EqualTo("Header"));
            Assert.That(asset.Nodes.Single(n => n.Parent == "Contents").Type, Is.EqualTo("HSplitContainer"));
        }
    }
}
