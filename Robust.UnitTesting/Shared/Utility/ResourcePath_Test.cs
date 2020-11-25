using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Utility
{
    [TestFixture]
    [Parallelizable(ParallelScope.Fixtures | ParallelScope.All)]
    [TestOf(typeof(ResourcePath))]
    public class ResourcePath_Test
    {
        public static List<(string, string)> InputClean_Values = new()
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

        public static List<(string, string)> Extension_Values = new()
        {
            ("foo", ""),
            ("foo.png", "png"),
            ("test/foo.png", "png"),
            (".bashrc", ""),
            ("..png", "png"),
            ("x.y.z", "z")
        };

        [Test]
        public void Extension_Test([ValueSource(nameof(Extension_Values))] (string path, string expected) data)
        {
            var respath = new ResourcePath(data.path);
            Assert.That(respath.Extension, Is.EqualTo(data.expected));
        }

        public static List<(string, string)> Filename_Values = new()
        {
            ("foo", "foo"),
            ("foo.png", "foo.png"),
            ("x/y/z", "z"),
            ("/bar", "bar"),
            ("foo/", "foo") // Trailing / gets trimmed.
        };

        [Test]
        public void Filename_Test([ValueSource(nameof(Filename_Values))] (string path, string expected) data)
        {
            var respath = new ResourcePath(data.path);
            Assert.That(respath.Filename, Is.EqualTo(data.expected));
        }

        public static List<(string, string)> FilenameWithoutExtension_Values = new()
        {
            ("foo", "foo"),
            ("foo.png", "foo"),
            ("test/foo.png", "foo"),
            ("derp/.bashrc", ".bashrc"),
            ("..png", "."),
            ("x.y.z", "x.y")
        };

        [Test]
        public void FilenameWithoutExtension_Test([ValueSource(nameof(FilenameWithoutExtension_Values))] (string path, string expected) data)
        {
            var respath = new ResourcePath(data.path);
            Assert.That(respath.FilenameWithoutExtension, Is.EqualTo(data.expected));
        }

        [TestCase(@"", ExpectedResult = @".")]
        [TestCase(@".", ExpectedResult = @".")]
        [TestCase(@"/foo/bar", ExpectedResult = @"/foo")]
        [TestCase(@"/foo/bar.txt", ExpectedResult = @"/foo")]
        public string DirectoryTest(string path)
        {
            return new ResourcePath(path).Directory.ToString();
        }

        [Test]
        public void ChangeSeparator_Test()
        {
            var respath = new ResourcePath("a/b/c").ChangeSeparator("👏");
            Assert.That(respath.ToString(), Is.EqualTo("a👏b👏c"));
        }

        [Test]
        public void ChangeSeparatorRooted_Test()
        {
            var respath = new ResourcePath("/a/b/c").ChangeSeparator("👏");
            Assert.That(respath.ToString(), Is.EqualTo("👏a👏b👏c"));
        }

        [Test]
        public void Combine_Test()
        {
            var path1 = new ResourcePath("/a/b");
            var path2 = new ResourcePath("c/d.png");
            Assert.That((path1 / path2).ToString(), Is.EqualTo("/a/b/c/d.png"));
            Assert.That((path1 / "z").ToString(), Is.EqualTo("/a/b/z"));
        }

        public static List<(string, string)> Clean_Values = new()
        {
            ("//a/b/../c/./ss14.png", "/a/c/ss14.png"),
            ("../a", "../a"),
            ("../a/..", ".."),
            ("../..", "../.."),
            ("a/..", "."),
            ("/../a", "/a"),
            ("/..", "/"),
        };

        [Test]
        public void Clean_Test([ValueSource(nameof(Clean_Values))] (string path, string expected) data)
        {
            var path = new ResourcePath(data.path);
            var cleaned = path.Clean();
            Assert.Multiple(() =>
            {
                if (path == cleaned)
                {
                    Assert.That(path.IsClean());
                }
                Assert.That(path.Clean(), Is.EqualTo(new ResourcePath(data.expected)));
                Assert.That(cleaned.IsClean());
            });
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

        public static List<(string, string, string)> RelativeTo_Values = new()
        {
            ("/a/b", "/a", "b"),
            ("/a", "/", "a"),
            ("/a/b/c", "/", "a/b/c"),
            ("/a", "/a", "."),
            ("a/b", "a", "b"),
            ("/Textures/Weapons/laser.png", "/Textures/", "Weapons/laser.png")
        };

        [Test]
        public void RelativeTo_Test([ValueSource(nameof(RelativeTo_Values))] (string source, string basePath, string expected) value)
        {
            var path = new ResourcePath(value.source);
            var basePath = new ResourcePath(value.basePath);
            Assert.That(path.RelativeTo(basePath), Is.EqualTo(new ResourcePath(value.expected)));
        }

        public static List<(string, string)> RelativeToFail_Values = new()
        {
            ("/a/b", "/b"),
            ("/a", "/c/d"),
            ("/a/b", "/a/d"),
            (".", "/"),
            ("/", ".")
        };

        [Test]
        public void RelativeToFail_Test([ValueSource(nameof(RelativeToFail_Values))] (string source, string basePath) value)
        {
            var path = new ResourcePath(value.source);
            var basePath = new ResourcePath(value.basePath);
            Assert.That(() => path.RelativeTo(basePath), Throws.ArgumentException);
        }

        public static List<(string, string, string)> CommonBase_Values = new()
        {
            ("/a/b", "/a/c", "/a"),
            ("a/b", "a/c", "a"),
            ("/usr", "/bin", "/")
        };

        [Test]
        public void CommonBase_Test([ValueSource(nameof(CommonBase_Values))] (string a, string b, string expected) value)
        {
            var path = new ResourcePath(value.a);
            var basePath = new ResourcePath(value.b);
            Assert.That(path.CommonBase(basePath), Is.EqualTo(new ResourcePath(value.expected)));
        }

        [Test]
        public void CommonBaseFail_Test()
        {
            var path = new ResourcePath("a/b");
            var basePath = new ResourcePath("b/a");
            Assert.That(() => path.CommonBase(basePath), Throws.ArgumentException);
        }

        [Test]
        public void WithNameTest()
        {
            var path = new ResourcePath("/a/b");
            var modified = path.WithName("foo");
            Assert.That(modified.Filename, Is.EqualTo("foo"));
            modified = path.WithName("foo.exe");
            Assert.That(modified.Filename, Is.EqualTo("foo.exe"));
            Assert.That(modified.Extension, Is.EqualTo("exe"));
        }

        [Test]
        public void WithNameExceptionTest()
        {
            var path = new ResourcePath("/a/b");
            Assert.That(() => path.WithName("/foo"), Throws.ArgumentException);
            Assert.That(() => path.WithName("."), Throws.ArgumentException);
            Assert.That(() => path.WithName(""), Throws.ArgumentException);
            Assert.That(() => path.WithName(null!), Throws.ArgumentException);
        }

        [Test]
        [TestCase("/a/b.txt", "png", "/a/b.png")]
        [TestCase("/a/b.txt.bak", "png", "/a/b.txt.png")]
        public void WithExtensionTest(string start, string newExt, string expected)
        {
            var startPath = new ResourcePath(start);
            Assert.That(startPath.WithExtension(newExt).ToString(), Is.EqualTo(expected));
            Assert.That(startPath.WithExtension(newExt).ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void RootToRelativeTest()
        {
            var path = new ResourcePath("/");

            Assert.That(path.ToRelativePath(), Is.EqualTo(new ResourcePath(".")));
        }
    }
}
