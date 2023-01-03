using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.Tracing.Parsers.FrameworkEventSource;
using NUnit.Framework;
using Robust.Shared.Utility;
// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.Utility;

[TestFixture]
[Parallelizable(ParallelScope.Fixtures | ParallelScope.All)]
[TestOf(typeof(ResPath))]
public sealed class ResPathTest
{
    [Test]
    [TestCase("/Textures", new[]{"Textures"}, new[]{"Textures"})]
    [TestCase("/Textures/", new[]{"Textures"}, new[]{"Textures"})]
    public void IteratorTest(string input, string[] expectedSegments, string[] expectedRevSegments)
    {
        var res = new ResPath(input);
        var segment = res.Segments().ToArray();
        var reverseSegment = res.ReverseSegments().ToArray();
        Assert.That(segment, Is.EqualTo(expectedSegments));
        Assert.That(reverseSegment, Is.EqualTo(expectedRevSegments));
    }
    
    
    
    public static List<(string, string)> InputCleanValues = new()
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
    public void InputClean_Test([ValueSource(nameof(InputCleanValues))] (string input, string expected) path)
    {
        var resPath = new ResPath(path.input);
        Assert.That(resPath.ToString(), Is.EqualTo(path.expected));
    }

    public static List<(string, string)> ExtensionValues = new()
    {
        ("foo", ""),
        ("foo.png", "png"),
        ("test/foo.png", "png"),
        (".bashrc", ""),
        ("..png", "png"),
        ("x.y.z", "z")
    };

    [Test]
    public void Extension_Test([ValueSource(nameof(ExtensionValues))] (string path, string expected) data)
    {
        var respath = new ResPath(data.path);
        Assert.That(respath.Extension, Is.EqualTo(data.expected));
    }

    public static List<(string, string)> FilenameValues = new()
    {
        ("foo", "foo"),
        ("foo.png", "foo.png"),
        ("x/y/z", "z"),
        ("/bar", "bar"),
        ("foo/", "foo") // Trailing / gets trimmed.
    };

    [Test]
    public void Filename_Test([ValueSource(nameof(FilenameValues))] (string path, string expected) data)
    {
        var respath = new ResPath(data.path);
        Assert.That(respath.Filename, Is.EqualTo(data.expected));
    }

    public static List<(string, string)> FilenameWithoutExtensionValues = new()
    {
        ("foo", "foo"),
        ("foo.png", "foo"),
        ("test/foo.png", "foo"),
        ("derp/.bashrc", ".bashrc"),
        ("..png", "."),
        ("x.y.z", "x.y")
    };

    [Test]
    public void FilenameWithoutExtension_Test([ValueSource(nameof(FilenameWithoutExtensionValues))] (string path, string expected) data)
    {
        var respath = new ResPath(data.path);
        Assert.That(respath.FilenameWithoutExtension, Is.EqualTo(data.expected));
    }

    [TestCase(@"", ExpectedResult = @".")]
    [TestCase(@".", ExpectedResult = @".")]
    [TestCase(@"/foo/bar", ExpectedResult = @"/foo")]
    [TestCase(@"/foo/bar.txt", ExpectedResult = @"/foo")]
    public string DirectoryTest(string path)
    {
        return new ResPath(path).Directory;
    }

    [Test]
    public void ChangeSeparator_Test()
    {
        var respath = new ResPath("a/b/c").ChangeSeparator("👏");
        Assert.That(respath, Is.EqualTo("a👏b👏c"));
    }

    [Test]
    public void ChangeSeparatorRooted_Test()
    {
        var respath = new ResPath("/a/b/c").ChangeSeparator("👏");
        Assert.That(respath, Is.EqualTo("👏a👏b👏c"));
    }

    [Test]
    public void Combine_Test()
    {
        var path1 = new ResPath("/a/b");
        var path2 = new ResPath("c/d.png");
        Assert.That((path1 / path2).ToString(), Is.EqualTo("/a/b/c/d.png"));
        Assert.That((path1 / "z").ToString(), Is.EqualTo("/a/b/z"));
    }

    public static List<(string, string)> CleanValues = new()
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
    public void Clean_Test([ValueSource(nameof(CleanValues))] (string path, string expected) data)
    {
        var path = new ResPath(data.path);
        var cleaned = path.Clean();
        Assert.Multiple(() =>
        {
            if (path == cleaned)
            {
                Assert.That(path.IsClean());
            }
            Assert.That(path.Clean(), Is.EqualTo(new ResPath(data.expected)));
            Assert.That(cleaned.IsClean());
        });
    }

    [Test]
    public void RootedConversions_Test()
    {
        var path = new ResPath("/a/b");
        Assert.That(path.IsRooted);
        Assert.That(path.ToRootedPath(), Is.EqualTo(path));

        var relative = path.ToRelativePath();
        Assert.That(relative, Is.EqualTo(new ResPath("a/b")));
        Assert.That(relative.IsRelative);

        Assert.That(relative.ToRelativePath(), Is.EqualTo(relative));
        Assert.That(relative.ToRootedPath(), Is.EqualTo(path));
    }

    public static List<(string, string, string)> RelativeToValues = new()
    {
        ("/a/b", "/a", "b"),
        ("/a", "/", "a"),
        ("/a/b/c", "/", "a/b/c"),
        ("/a", "/a", "."),
        ("a/b", "a", "b"),
        ("/Textures/Weapons/laser.png", "/Textures/", "Weapons/laser.png")
    };

    [Test]
    public void RelativeTo_Test([ValueSource(nameof(RelativeToValues))] (string source, string basePath, string expected) value)
    {
        var path = new ResPath(value.source);
        var basePath = new ResPath(value.basePath);
        Assert.That(path.RelativeTo(basePath), Is.EqualTo(new ResPath(value.expected)));
    }

    public static List<(string, string)> RelativeToFailValues = new()
    {
        ("/a/b", "/b"),
        ("/a", "/c/d"),
        ("/a/b", "/a/d"),
        (".", "/"),
        ("/", ".")
    };

    [Test]
    public void RelativeToFail_Test([ValueSource(nameof(RelativeToFailValues))] (string source, string basePath) value)
    {
        var path = new ResPath(value.source);
        var basePath = new ResPath(value.basePath);
        Assert.That(() => path.RelativeTo(basePath), Throws.ArgumentException);
    }

    public static List<(string, string, string)> CommonBaseValues = new()
    {
        ("/a/b", "/a/c", "/a"),
        ("a/b", "a/c", "a"),
        ("/usr", "/bin", "/")
    };

    [Test]
    public void CommonBase_Test([ValueSource(nameof(CommonBaseValues))] (string a, string b, string expected) value)
    {
        var path = new ResPath(value.a);
        var basePath = new ResPath(value.b);
        Assert.That(path.CommonBase(basePath), Is.EqualTo(new ResPath(value.expected)));
    }

    [Test]
    public void CommonBaseFail_Test()
    {
        var path = new ResPath("a/b");
        var basePath = new ResPath("b/a");
        Assert.That(() => path.CommonBase(basePath), Throws.ArgumentException);
    }

    [Test]
    public void WithNameTest()
    {
        var path = new ResPath("/a/b");
        var modified = path.WithName("foo");
        Assert.That(modified.Filename, Is.EqualTo("foo"));
        modified = path.WithName("foo.exe");
        Assert.That(modified.Filename, Is.EqualTo("foo.exe"));
        Assert.That(modified.Extension, Is.EqualTo("exe"));
    }

    [Test]
    public void WithNameExceptionTest()
    {
        var path = new ResPath("/a/b");
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
        var startPath = new ResPath(start);
        Assert.That(startPath.WithExtension(newExt).ToString(), Is.EqualTo(expected));
        Assert.That(startPath.WithExtension(newExt).ToString(), Is.EqualTo(expected));
    }

    [Test]
    public void RootToRelativeTest()
    {
        var path = new ResPath("/");

        Assert.That(path.ToRelativePath(), Is.EqualTo(new ResPath(".")));
    }
}