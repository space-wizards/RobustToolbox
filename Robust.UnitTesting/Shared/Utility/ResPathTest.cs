using System;
using NUnit.Framework;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Utility;

[TestFixture]
[Parallelizable(ParallelScope.Fixtures | ParallelScope.All)]
[TestOf(typeof(ResPath))]
public sealed class ResPathTest
{
    [Test]
    [TestCase("foo", ExpectedResult = "")]
    [TestCase("foo.png", ExpectedResult = "png")]
    [TestCase("test/foo.png", ExpectedResult = "png")]
    [TestCase("..png", ExpectedResult = "png")]
    [TestCase(".bashrc", ExpectedResult = "")]
    [TestCase("x.y.z", ExpectedResult = "z")]
    public string ExtensionTest(string input)
    {
        var resPathExt = new ResPath(input).Extension;
        var resourceExt = new ResPath(input).Extension;
        Assert.That(resPathExt, Is.EqualTo(resourceExt),
            message: "Found discrepancy between ResPath and ResourcePath Extension");
        return resPathExt;
    }

    [Test]
    [TestCase("", ExpectedResult = ".")]
    [TestCase(".", ExpectedResult = ".")]
    [TestCase("foo", ExpectedResult = "foo")]
    [TestCase("foo.png", ExpectedResult = "foo.png")]
    [TestCase("test/foo.png", ExpectedResult = "foo.png")]
    [TestCase("derp/.bashrc", ExpectedResult = ".bashrc")]
    [TestCase("..png", ExpectedResult = "..png")]
    [TestCase("x/y/z", ExpectedResult = "z")]
    [TestCase("/bar", ExpectedResult = "bar")]
    [TestCase("bar/", ExpectedResult = "bar")] // Trailing / gets trimmed.
    public string FilenameTest(string input)
    {
        var resPathFilename = new ResPath(input).Filename;
        return resPathFilename;
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
        var resPathFileNoExt = new ResPath(input).FilenameWithoutExtension;
        return resPathFileNoExt;
    }

    [Test]
    [TestCase(@"", ExpectedResult = @".")]
    [TestCase(@".", ExpectedResult = @".")]
    [TestCase(@"/foo/bar", ExpectedResult = @"/foo")]
    [TestCase(@"/foo/bar/", ExpectedResult = @"/foo")]
    [TestCase(@"/foo/bar/x", ExpectedResult = @"/foo/bar")]
    [TestCase(@"/foo/bar.txt", ExpectedResult = @"/foo")]
    [TestCase(@"/bar.txt", ExpectedResult = @"/")]
    public string DirectoryTest(string path)
    {
        var resPathDirectory = new ResPath(path).Directory.ToString();
        return resPathDirectory;
    }

    [Test]
    [TestCase(@"a/b/c", "👏", ExpectedResult = "a👏b👏c")]
    [TestCase(@"/a/b/c", "👏", ExpectedResult = "👏a👏b👏c")]
    [TestCase(@"/a/b/c", "\\", ExpectedResult = @"\a\b\c")]
    public string ChangeSeparatorTest(string input, string separator)
    {
        return new ResPath(input).ChangeSeparator(separator);
    }

    [Test]
    [TestCase(@"a.b.c", ".")]
    [TestCase("\0a\0b\0c", "\0")]
    public void ChangeSeparatorTestException(string input, string separator)
    {
        Assert.Catch(typeof(ArgumentException), () => {new ResPath(input).ChangeSeparator(separator); });
    }

    [Test]
    [TestCase("/a/b", "c/d.png", ExpectedResult = "/a/b/c/d.png")]
    [TestCase("/a/b", "z", ExpectedResult = "/a/b/z")]
    [TestCase("/a/b", "/z", ExpectedResult = "/z")]
    [TestCase("/a/b", ".", ExpectedResult = "/a/b")]
    [TestCase("/", "/a", ExpectedResult = "/a")]
    [TestCase("/", "a", ExpectedResult = "/a")]
    public string CombineTest(string left, string right)
    {
        var pathDivRes = new ResPath(left) / new ResPath(right);
        var pathDivStr = new ResPath(left) / right;
        Assert.That(pathDivRes, Is.EqualTo(pathDivStr));
        return pathDivRes.ToString();
    }

    [Test]
    public void RootedConversionsTest()
    {
        var path = new ResPath("/a/b");
        Assert.That(path.IsRooted);
        Assert.That(path.ToRootedPath(), Is.EqualTo(path));

        var relative = path.ToRelativePath();
        Assert.That(relative, Is.EqualTo( new ResPath("a/b")));
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
    [TestCase("/bar/", "/", ExpectedResult = "bar")]
    [TestCase("/Textures/Weapons/laser.png", "/Textures/", ExpectedResult = "Weapons/laser.png")]
    [TestCase("foo.txt", ".", ExpectedResult = "foo.txt")]
    public string RelativeToTest(string source, string baseDir)
    {
        var path = new ResPath(source);
        var basePath = new ResPath(baseDir);
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
        var path = new ResPath(left);
        var basePath = new ResPath(right);
        Assert.That(() => path.RelativeTo(basePath), Throws.ArgumentException);
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
        ResPath path = new("/a/b");
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
    [TestCase("/.gitignore")]
    [TestCase("")]
    [TestCase("/")]
    [TestCase("")]
    public void WithExtensionExceptionTest(string ext)
    {
        var resPath = new ResPath("/a/b");
        Assert.Catch(typeof(ArgumentException), () => { resPath.WithExtension(ext); });
    }

    [Test]
    public void RootToRelativeTest()
    {
        var path = new ResPath("/");

        Assert.That(path.ToRelativePath(), Is.EqualTo(new ResPath(".")));
    }

    [Test]
    public void TestEmptyEdgeCases()
    {
        ResPath? empty = ResPath.Empty;
        Assert.That(empty?.Extension, Is.EqualTo(""));
        Assert.That(empty?.Filename, Is.EqualTo("."));
        Assert.That(empty?.FilenameWithoutExtension, Is.EqualTo("."));
        Assert.That(empty.Equals(null), Is.False);
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
        var pathA = new ResPath(left);
        var pathB = new ResPath(right);
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
        var canonPath = new ResPath(canonStr);
        Assert.That(systemPath, Is.EqualTo(canonPath));
        Assert.That(systemPath.ToRelativeSystemPath(), Is.EqualTo(canonPath.ToRelativeSystemPath()));
    }
}
