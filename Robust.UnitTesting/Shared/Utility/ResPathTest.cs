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
        var resPath = new ResPath(input);
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
        return new ResPath(input).Extension;
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
        return new ResPath(input).Filename;
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
         return new ResPath(input).FilenameWithoutExtension;

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
        return new ResPath(path).Directory.ToString();
    }

    [Test]
    [TestCase(@"a/b/c", "👏", ExpectedResult = "a👏b👏c")]
    [TestCase(@"/a/b/c", "👏", ExpectedResult = "👏a👏b👏c")]
    [TestCase(@"/a/b/c", "\\",  ExpectedResult= @"\a\b\c")]
    public string ChangeSeparatorTest(string input, string separator)
    {
        return new ResPath(input).ChangeSeparator(separator);
    }

    [Test]
    public void CombineTest()
    {
        var path1 = new ResPath("/a/b");
        var path2 = new ResPath("c/d.png");
        Assert.That((path1 / path2).ToString(), Is.EqualTo("/a/b/c/d.png"));
        Assert.That((path1 / "z").ToString(), Is.EqualTo("/a/b/z"));
    }

    [Test]
    [TestCase("//a/b/../c/./ss14.png", ExpectedResult = "/a/c/ss14.png")]
    [TestCase("../a", ExpectedResult =  "../a")]
    [TestCase("../a/..", ExpectedResult =  "..")]
    [TestCase("../..", ExpectedResult =  "../..")]
    [TestCase("a/..", ExpectedResult =  ".")]
    [TestCase("/../a",  ExpectedResult =  "/a")]
    [TestCase("/..", ExpectedResult =  "/")]
    public string CleanTest(string input)
    {
        var path = new ResPath(input);
        var cleaned = path.Clean();
        Assert.AreEqual(path == cleaned, path.IsClean());
        return path.Clean().ToString();
    }

    [Test]
    public void RootedConversionsTest()
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

    [Test]
    [TestCase("/a/b", "/a", ExpectedResult = "b")]
    [TestCase("/a", "/", ExpectedResult = "a")]
    [TestCase("/a/b/c", "/", ExpectedResult = "a/b/c")]
    [TestCase("/a", "/a", ExpectedResult = ".")]
    [TestCase("a/b", "a", ExpectedResult = "b")]
    [TestCase("/Textures/Weapons/laser.png", "/Textures/", ExpectedResult = "Weapons/laser.png")]
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
    public void RelativeToFailTest(string path1, string path2, bool isRelative)
    {
        var path = new ResPath(path1);
        var basePath = new ResPath(path2);
        Assert.That(() => path.RelativeTo(basePath), Throws.ArgumentException);
    }
    

    [Test]
    [TestCase("/a/b", "/a/c", ExpectedResult= "/a")]
    [TestCase("a/b", "a/c", ExpectedResult =  "a")]
    [TestCase("/usr", "/bin", ExpectedResult = "/")]
    public string CommonBaseTest(string a, string b)
    {
        return new ResPath(a).CommonBase(new ResPath(b)).ToString();
    }

    [Test]
    public void CommonBaseFailTest()
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