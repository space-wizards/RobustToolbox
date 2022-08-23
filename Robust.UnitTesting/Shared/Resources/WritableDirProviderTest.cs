using System;
using System.IO;
using NUnit.Framework;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Resources
{
    [TestFixture]
    [TestOf(typeof(WritableDirProvider))]
    public sealed class WritableDirProviderTest
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

            _dirProvider = new WritableDirProvider(Directory.CreateDirectory(subDir));
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
            Assert.That(() => _dirProvider.ReadAllText(new ResourcePath("/../dummy")),
                Throws.InstanceOf<FileNotFoundException>());
        }

        [Test]
        public void TestNotRooted()
        {
            // Path must be rooted.
            Assert.That(() => _dirProvider.OpenRead(new ResourcePath("foo.bar")),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void TestParentAccessClamped()
        {
            File.WriteAllText(Path.Combine(_testDirPath, "dummy"), "foobar");

            _dirProvider.WriteAllText(new ResourcePath("/dummy"), "pranked");

            // ../ should get clamped to /.
            Assert.That(_dirProvider.ReadAllText(new ResourcePath("/../dummy")), Is.EqualTo("pranked"));
        }
    }
}
