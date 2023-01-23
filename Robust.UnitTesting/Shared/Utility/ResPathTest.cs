using System;
using NUnit.Framework;
using Robust.Shared.Utility;

// ReSharper disable AccessToStaticMemberViaDerivedType


namespace Robust.UnitTesting.Shared.Utility;

[TestFixture]
[Parallelizable(ParallelScope.Fixtures | ParallelScope.All)]
[TestOf(typeof(ResPath))]
public sealed class ResPathTest
{
    // Tests whether input and output remains unchanged.
    [Test]
    [TestCase("/Textures", "/Textures")]
    [TestCase("Textures", "Textures")]
    [TestCase("Textures/", "Textures")]
    [TestCase("Textures/Laser.png", "Textures/Laser.png")]
    [TestCase("Textures//Laser.png", "Textures/Laser.png")]
    [TestCase("Textures/..//Radio.png/", "Textures/../Radio.png")]
    [TestCase("", ".")]
    [TestCase(".", ".")]
    [TestCase("./foo", "foo")]
    [TestCase("./foo/bar", "foo/bar")]
    [TestCase("foo/.", "foo")]
    [TestCase("foo/./bar", "foo/bar")]
    [TestCase("./", ".")]
    [TestCase("/.", "/")]
    [TestCase("/", "/")]
    [TestCase(" ", " ")] // Note the spaces here]
    [TestCase(" / ", " / ")]
    [TestCase(". ", ". ")]
    public void InputCleanTest(string input, string expected)
    {
        var resPath = ResPath.CreateWithSeparator(input);
        Assert.That(resPath.ToString(), Is.EqualTo(expected));
    }

    [Test]
    [TestCase("/Textures", "/Textures")]
    [TestCase("Textures", "Textures")]
    [TestCase("Textures/", "Textures")]
    [TestCase("Textures/Laser.png", "Textures/Laser.png")]
    [TestCase("Textures//Laser.png", "Textures/Laser.png")]
    [TestCase("Textures/..//Radio.png/", "Radio.png")]
    [TestCase("", "")]
    [TestCase(".", ".")]
    [TestCase("./foo", "foo")]
    [TestCase("./foo/bar", "foo/bar")]
    [TestCase("foo/.", "foo")]
    [TestCase("foo/./bar", "foo/bar")]
    [TestCase("./", ".")]
    [TestCase("/.", "/")]
    [TestCase("/", "/")]
    [TestCase(" ", " ")] // Note the spaces here]
    [TestCase(" / ", " / ")]
    [TestCase(". ", ". ")]
    public void UnsafeCleanTest(string input, string expected)
    {
        var resPath = ResPath.CreateUnsafePath(input).Clean();
        Assert.That(resPath.ToString(), Is.EqualTo(expected));
    }

    [Test]
    [TestCase(@"foo", ExpectedResult = @"")]
    [TestCase(@"foo.png", ExpectedResult = @"png")]
    [TestCase(@"test/foo.png", ExpectedResult = @"png")]
    [TestCase(@"..png", ExpectedResult = @"png")]
    [TestCase(@".bashrc", ExpectedResult = @"")]
    [TestCase(@"x.y.z", ExpectedResult = @"z")]
    public string ExtensionTest(string input)
    {
        return ResPath.CreateWithSeparator(input).Extension;
    }

    [Test]
    [TestCase(@"", ExpectedResult = @".")]
    [TestCase(@".", ExpectedResult = @".")]
    [TestCase(@"foo", ExpectedResult = @"foo")]
    [TestCase(@"foo.png", ExpectedResult = @"foo.png")]
    [TestCase(@"test/foo.png", ExpectedResult = @"foo.png")]
    [TestCase(@"derp/.bashrc", ExpectedResult = @".bashrc")]
    [TestCase(@"..png", ExpectedResult = @"..png")]
    [TestCase(@"x/y/z", ExpectedResult = @"z")]
    [TestCase(@"/bar", ExpectedResult = @"bar")]
    [TestCase(@"bar/", ExpectedResult = @"bar")] // Trailing / gets trimmed.
    public string FilenameTest(string input)
    {
        return ResPath.CreateWithSeparator(input).Filename;
    }


    [Test]
    [TestCase(@"", ExpectedResult = @".")]
    [TestCase(@".", ExpectedResult = @".")]
    [TestCase(@"foo", ExpectedResult = @"foo")]
    [TestCase(@"foo.png", ExpectedResult = @"foo")]
    [TestCase(@"test/foo.png", ExpectedResult = @"foo")]
    [TestCase(@"derp/.bashrc", ExpectedResult = @".bashrc")]
    [TestCase(@"..png", ExpectedResult = @".")]
    [TestCase(@"x.y.z", ExpectedResult = @"x.y")]
    public string FilenameWithoutExtension(string input)
    {
        return ResPath.CreateWithSeparator(input).FilenameWithoutExtension;
    }

    [Test]
    [TestCase(@"", ExpectedResult = @".")]
    [TestCase(@".", ExpectedResult = @".")]
    [TestCase(@"/foo/bar", ExpectedResult = @"/foo")]
    [TestCase(@"/foo/bar/", ExpectedResult = @"/foo")]
    [TestCase(@"/foo/bar/x", ExpectedResult = @"/foo/bar")]
    [TestCase(@"/foo/bar.txt", ExpectedResult = @"/foo")]
    public string DirectoryTest(string path)
    {
        return ResPath.CreateWithSeparator(path).Directory.ToString();
    }

    [Test]
    [TestCase(@"a/b/c", "👏", ExpectedResult = "a👏b👏c")]
    [TestCase(@"/a/b/c", "👏", ExpectedResult = "👏a👏b👏c")]
    [TestCase(@"/a/b/c", "\\", ExpectedResult = @"\a\b\c")]
    public string ChangeSeparatorTest(string input, string separator)
    {
        return ResPath.CreateWithSeparator(input).ChangeSeparator(separator);
    }

    [Test]
    [TestCase(@"a.b.c", ".")]
    [TestCase("\0a\0b\0c", "\0")]
    public void ChangeSeparatorTestException(string input, string separator)
    {
        Assert.Catch(typeof(ArgumentException), () => { ResPath.CreateUnsafePath(input).ChangeSeparator(separator); });
    }

    [Test]
    [TestCase("/a/b", "c/d.png", ExpectedResult = "/a/b/c/d.png")]
    [TestCase("/a/b", "z", ExpectedResult = "/a/b/z")]
    [TestCase("/a/b", "/z", ExpectedResult = "/z")]
    [TestCase("/a/b", ".", ExpectedResult = "/a/b")]
    public string CombineTest(string left, string right)
    {
        var pathDivRes = ResPath.CreateUnsafePath(left) / ResPath.CreateWithSeparator(right);
        var pathDivStr = ResPath.CreateUnsafePath(left) / right;
        Assert.That(pathDivRes, Is.EqualTo(pathDivStr));
        return pathDivRes.ToString();
    }

    [Test]
    [TestCase('.')]
    public void ResPathCtorFail(char separator)
    {
        Assert.Catch(typeof(ArgumentException), () =>
        {
            var _ = ResPath.CreateWithSeparator("/x/y", separator);
        });
    }

    [Test]
    [TestCase("//a/b/../c/./ss14.png", ExpectedResult = "/a/c/ss14.png")]
    [TestCase("../a", ExpectedResult = "../a")]
    [TestCase("../a/..", ExpectedResult = "..")]
    [TestCase("../..", ExpectedResult = "../..")]
    [TestCase("a/..", ExpectedResult = ".")]
    [TestCase("/../a", ExpectedResult = "/a")]
    [TestCase("/..", ExpectedResult = "/")]
    public string CleanTest(string input)
    {
        var path = ResPath.CreateWithSeparator(input);
        var cleaned = path.Clean();
        Assert.That(path.IsClean(), Is.EqualTo(path == cleaned));
        return path.Clean().ToString();
    }

    [Test]
    public void RootedConversionsTest()
    {
        var path = ResPath.CreateWithSeparator("/a/b");
        Assert.That(path.IsRooted);
        Assert.That(path.ToRootedPath(), Is.EqualTo(path));

        var relative = path.ToRelativePath();
        Assert.That(relative, Is.EqualTo(ResPath.CreateWithSeparator("a/b")));
        Assert.That(relative.IsRelative);

        Assert.That(relative.ToRelativePath(), Is.EqualTo(relative));
        Assert.That(relative.ToRootedPath(), Is.EqualTo(path));
    }

    [Test]
    [TestCase("/a/b", "/a", ExpectedResult = "b")]
    [TestCase("/a", "/", ExpectedResult = "a")]
    [TestCase("/a/b/c", "/", ExpectedResult = "a/b/c")]
    [TestCase("/a", "/a", ExpectedResult = ".")]
    [TestCase("a/b", "a", ExpectedResult = "b")]
    [TestCase("/Textures/Weapons/laser.png", "/Textures/", ExpectedResult = "Weapons/laser.png")]
    public string RelativeToTest(string source, string baseDir)
    {
        var path = ResPath.CreateWithSeparator(source);
        var basePath = ResPath.CreateWithSeparator(baseDir);
        return path.RelativeTo(basePath).ToString();
    }

    [Test]
    [TestCase("/a/b", "/b", false)]
    [TestCase("/a", "/c/d", false)]
    [TestCase("/a/b", "/a/d", false)]
    [TestCase(".", "/", false)]
    [TestCase("/", ".", false)]
    public void RelativeToFailTest(string left, string right, bool isRelative)
    {
        var path = ResPath.CreateWithSeparator(left);
        var basePath = ResPath.CreateWithSeparator(right);
        Assert.That(() => path.RelativeTo(basePath), Throws.ArgumentException);
    }


    [Test]
    [TestCase("/a/b", "/a/c", ExpectedResult = "/a")]
    [TestCase("a/b", "a/c", ExpectedResult = "a")]
    [TestCase("/usr", "/bin", ExpectedResult = "/")]
    [TestCase("/a", "/a", ExpectedResult = "/a")]
    [TestCase("a", "a", ExpectedResult = "a")]
    public string CommonBaseTest(string left, string right)
    {
        return ResPath.CreateWithSeparator(left).CommonBase(ResPath.CreateWithSeparator(right)).ToString();
    }

    [Test]
    public void CommonBaseFailTest()
    {
        var path = ResPath.CreateWithSeparator("a/b");
        var basePath = ResPath.CreateWithSeparator("b/a");
        Assert.That(() => path.CommonBase(basePath), Throws.ArgumentException);
    }

    [Test]
    public void WithNameTest()
    {
        var path = ResPath.CreateWithSeparator("/a/b");
        var modified = path.WithName("foo");
        Assert.That(modified.Filename, Is.EqualTo("foo"));
        modified = path.WithName("foo.exe");
        Assert.That(modified.Filename, Is.EqualTo("foo.exe"));
        Assert.That(modified.Extension, Is.EqualTo("exe"));
    }

    [Test]
    public void WithNameExceptionTest()
    {
        var path = ResPath.CreateWithSeparator("/a/b");
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
        var startPath = ResPath.CreateWithSeparator(start);
        Assert.That(startPath.WithExtension(newExt).ToString(), Is.EqualTo(expected));
        Assert.That(startPath.WithExtension(newExt).ToString(), Is.EqualTo(expected));
    }

    [Test]
    [TestCase("/.gitignore")]
    [TestCase("")]
    [TestCase("/")]
    [TestCase("")]
    public void WithExtensionExceptionTest(string ext)
    {
        var resPath = ResPath.CreateUnsafePath("/a/b");
        Assert.Catch(typeof(ArgumentException), () => { resPath.WithExtension(ext); });
    }

    [Test]
    public void RootToRelativeTest()
    {
        var path = ResPath.CreateWithSeparator("/");

        Assert.That(path.ToRelativePath(), Is.EqualTo(ResPath.CreateWithSeparator(".")));
    }

    [Test]
    public void TestEmptyEdgeCases()
    {
        ResPath? empty = ResPath.Empty;
        Assert.That(empty?.Extension, Is.EqualTo(""));
        Assert.That(empty?.Filename, Is.EqualTo("."));
        Assert.That(empty?.FilenameWithoutExtension, Is.EqualTo("."));
        Assert.False(empty.Equals(null));
    }

    [Test]
    [TestCase("a", "a", ExpectedResult = true)]
    [TestCase("a", "ab", ExpectedResult = false)]
    [TestCase("", "", ExpectedResult = true)]
    [TestCase("", ".", ExpectedResult = false)]
    [TestCase(".", "", ExpectedResult = false)]
    [TestCase("/bin", "/usr", ExpectedResult = false)]
    public bool TestHashAndEquals(string left, string right)
    {
        var pathA = ResPath.CreateUnsafePath(left);
        var pathB = ResPath.CreateUnsafePath(right);
        Assert.That(pathA.GetHashCode() == pathB.GetHashCode(), Is.EqualTo(pathA == pathB));
        Assert.That(pathA.GetHashCode() != pathB.GetHashCode(), Is.EqualTo(pathA != pathB));
        return pathA == pathB;
    }

    [Test]
    [TestCase(@"\a\b\c", "/a/b/c")]
    [TestCase(@"\a\c", "/a/c")]
    [TestCase(@".", ".")]
    public void TestRelativeSystemPaths(string systemIn, string canonStr)
    {
        // Prevents frivolous warning, will lead to test having OS specific fails
        // ReSharper disable once RedundantArgumentDefaultValue
        var systemPath = ResPath.FromRelativeSystemPath(systemIn, '\\');
        var canonPath = ResPath.CreateUnsafePath(canonStr);
        Assert.That(systemPath, Is.EqualTo(canonPath));
        Assert.That(systemPath.ToRelativeSystemPath(), Is.EqualTo(canonPath.ToRelativeSystemPath()));
    }
}
