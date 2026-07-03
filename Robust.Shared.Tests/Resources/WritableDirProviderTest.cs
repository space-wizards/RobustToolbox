using NUnit.Framework;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Robust.Shared.Tests.Resources
{
    [TestFixture]
    [TestOf(typeof(WritableDirProvider))]
    internal sealed class WritableDirProviderTest
    {
        private string _testDirPath = default!;
        private DirectoryInfo _testDir = default!;
        private WritableDirProvider _dirProvider = default!;

        [OneTimeSetUp]
        public void Setup()
        {
            var tmpPath = Path.GetTempPath();
            var guid = Guid.NewGuid();
            _testDirPath = Path.Combine(tmpPath, guid.ToString());

            _testDir = Directory.CreateDirectory(_testDirPath);
            var subDir = Path.Combine(_testDirPath, "writable");

            _dirProvider = new WritableDirProvider(Directory.CreateDirectory(subDir), hideRootDir: false);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _testDir.Delete(true);
        }

        [Test]
        public void TestNoParentAccess()
        {
            File.WriteAllText(Path.Combine(_testDirPath, "dummy"), "foobar");

            // No, ../ does not work to read stuff in the parent dir.
            Assert.That(() => _dirProvider.ReadAllText(new ResPath("/../dummy")),
                Throws.InstanceOf<FileNotFoundException>());
        }

        [Test]
        public void TestNotRooted()
        {
            // Path must be rooted.
            Assert.That(() => _dirProvider.OpenRead(new ResPath("foo.bar")),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void TestParentAccessClamped()
        {
            File.WriteAllText(Path.Combine(_testDirPath, "dummy"), "foobar");

            _dirProvider.WriteAllText(new ResPath("/dummy"), "pranked");

            // ../ should get clamped to /.
            Assert.That(_dirProvider.ReadAllText(new ResPath("/../dummy")), Is.EqualTo("pranked"));
        }

        [Test]
        public void TestVirtualGetFullPath()
        {
            IWritableDirProvider provider = new VirtualWritableDirProvider();

            Assert.Multiple(() =>
            {
                Assert.That(provider.GetFullPath(ResPath.Root), Is.EqualTo("/"));
                Assert.That(provider.GetFullPath(new ResPath("/../foo/./bar")), Is.EqualTo("/foo/bar"));
                Assert.That(() => provider.GetFullPath(new ResPath("foo/bar")), Throws.ArgumentException);
            });
        }

        [Test]
        public void TestVirtualParentAccessClamped()
        {
            var provider = new VirtualWritableDirProvider();

            provider.WriteAllText(new ResPath("/dummy"), "pranked");

            Assert.That(provider.ReadAllText(new ResPath("/../dummy")), Is.EqualTo("pranked"));
        }

        [Test]
        public void TestVirtualFileMethodsUseFullPath()
        {
            var provider = new VirtualWritableDirProvider();

            provider.CreateDir(new ResPath("/foo/../bar"));
            provider.WriteAllText(new ResPath("/bar/file.txt"), "contents");

            Assert.That(provider.Exists(new ResPath("/foo/../bar/file.txt")), Is.True);
            Assert.That(provider.IsDir(new ResPath("/foo/../bar")), Is.True);
            Assert.That(provider.DirectoryEntries(new ResPath("/foo/../bar")), Is.EquivalentTo(new[] {"file.txt"}));

            provider.Rename(new ResPath("/foo/../bar/file.txt"), new ResPath("/foo/../bar/renamed.txt"));
            Assert.That(provider.ReadAllText(new ResPath("/bar/renamed.txt")), Is.EqualTo("contents"));

            provider.Delete(new ResPath("/foo/../bar/renamed.txt"));
            Assert.That(provider.Exists(new ResPath("/bar/renamed.txt")), Is.False);
        }

        [Test]
        public void TestVirtualOpenSubdirectoryUsesFullPath()
        {
            var provider = new VirtualWritableDirProvider();
            provider.CreateDir(new ResPath("/bar"));

            var subdirectory = provider.OpenSubdirectory(new ResPath("/foo/../bar"));
            subdirectory.WriteAllText(new ResPath("/file.txt"), "contents");

            Assert.That(provider.ReadAllText(new ResPath("/bar/file.txt")), Is.EqualTo("contents"));
        }
    }
}
