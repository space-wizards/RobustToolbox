using NUnit.Framework;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.UnitTesting.Shared.Utility
{
    [TestFixture]
    [Parallelizable]
    [TestOf(typeof(ResourcePath))]
    public class ResourcePath_Test
    {
        public static List<(string, string)> InputClean_Values = new List<(string, string)>
        {
            ("/Textures", "/Textures"),
            ("Textures", "Textures"),
            ("Textures/", "Textures"),
            ("Textures/Laser.png", "Textures/Laser.png"),
            ("Textures//Laser.png", "Textures/Laser.png"),
            ("Textures/..//Radio.png/", "Textures/../Radio.png"),
            ("", "."),
            (".", "."),
            ("./foo", "foo"),
            ("foo/.", "foo"),
            ("foo/./bar", "foo/bar"),
            ("./", "."),
            ("/.", "/"),
            ("/", "/"),
            (" ", " "), // Note the spaces here.
            (" / ", " / "),
            (". ", ". ")
        };

        // Tests whether input and output remains unchanged.
        [Test]
        public void InputClean_Test([ValueSource(nameof(InputClean_Values))] (string input, string expected) path)
        {
            var respath = new ResourcePath(path.input);
            Assert.That(respath.ToString(), Is.EqualTo(path.expected));
        }

        public static List<(string, string)> Extension_Values = new List<(string, string)>
        {
            ("foo", ""),
            ("foo.png", "png"),
            ("test/foo.png", "png"),
            (".bashrc", ""),
            ("..png", "png"),
            ("x.y.z", "z")
        };

        [Sequential]
        [Test]
        public void Extension_Test([ValueSource(nameof(Extension_Values))] (string path, string expected) data)
        {
            var respath = new ResourcePath(data.path);
            Assert.That(respath.Extension, Is.EqualTo(data.expected));
        }

        public static List<(string, string)> Filename_Values = new List<(string, string)>
        {
            ("foo", "foo"),
            ("foo.png", "foo.png"),
            ("x/y/z", "z"),
            ("/bar", "bar"),
            ("foo/", "foo")
        };

        [Sequential]
        [Test]
        public void Filename_Test([ValueSource(nameof(Filename_Values))] (string path, string expected) data)
        {
            var respath = new ResourcePath(data.path);
            Assert.That(respath.Filename, Is.EqualTo(data.expected));
        }

        public static List<(string, string)> FilenameWithoutExtension_Values = new List<(string, string)>
        {
            ("foo", "foo"),
            ("foo.png", "foo"),
            ("test/foo.png", "foo"),
            ("derp/.bashrc", ".bashrc"),
            ("..png", "."),
            ("x.y.z", "x.y")
        };

        [Sequential]
        [Test]
        public void FilenameWithoutExtension_Test([ValueSource(nameof(FilenameWithoutExtension_Values))] (string path, string expected) data)
        {
            var respath = new ResourcePath(data.path);
            Assert.That(respath.FilenameWithoutExtension, Is.EqualTo(data.expected));
        }

        [Test]
        public void ChangeSeparator_Test()
        {
            var respath = new ResourcePath("a/b/c").ChangeSeparator("👏");
            Assert.That(respath.ToString(), Is.EqualTo("a👏b👏c"));
        }

        [Test]
        public void Combine_Test()
        {
            var path1 = new ResourcePath("/a/b");
            var path2 = new ResourcePath("c/d.png");
            Assert.That((path1/path2).ToString(), Is.EqualTo("/a/b/c/d.png"));
            Assert.That((path1/"z").ToString(), Is.EqualTo("/a/b/z"));
        }

        [Test]
        public void Clean_Test()
        {
            var path = new ResourcePath("//a/b/../c/./ss14.png");
            Assert.That(path.Clean(), Is.EqualTo(new ResourcePath("/a/c/ss14.png")));
            Assert.That(path.IsClean(), Is.False);
            Assert.That(new ResourcePath("/a/c/ss14.png").IsClean(), Is.True);
        }

        [Test]
        public void RootedConversions_Test()
        {
            var path = new ResourcePath("/a/b");
            Assert.That(path.IsRooted);
            Assert.That(path.ToRootedPath(), Is.EqualTo(path));

            var relative = path.ToRelativePath();
            Assert.That(relative, Is.EqualTo(new ResourcePath("a/b")));
            Assert.That(relative.IsRelative);

            Assert.That(relative.ToRelativePath(), Is.EqualTo(relative));
            Assert.That(relative.ToRootedPath(), Is.EqualTo(path));
        }
    }
}
