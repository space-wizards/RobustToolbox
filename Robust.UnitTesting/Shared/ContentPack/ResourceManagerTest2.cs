using System;
using NUnit.Framework;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.ContentPack;

[TestFixture]
[TestOf(typeof(ResourceManager))]
[Parallelizable(ParallelScope.All)]
public sealed class ResourceManagerTest2
{
    [Test]
    public void TestMemoryRootReadFiles()
    {
        var (_, res) = CreateRes();

        var testRoot = new MemoryContentRoot();
        testRoot.AddOrUpdateFile(new ResPath("/a.txt"), "a"u8.ToArray());
        testRoot.AddOrUpdateFile(new ResPath("/b.txt"), "b"u8.ToArray());
        testRoot.AddOrUpdateFile(new ResPath("/c.txt"), "c"u8.ToArray());
        testRoot.AddOrUpdateFile(new ResPath("/d/foo.txt"), "foo"u8.ToArray());

        res.AddRoot(new ResPath("/"), testRoot);

        Assert.Multiple(() =>
        {
            Assert.That(res.ContentFileReadAllText("/a.txt"), Is.EqualTo("a"));
            Assert.That(res.ContentFileReadAllText("/b.txt"), Is.EqualTo("b"));
            Assert.That(res.ContentFileReadAllText("/c.txt"), Is.EqualTo("c"));
            Assert.That(res.ContentFileReadAllText("/d/foo.txt"), Is.EqualTo("foo"));
        });
    }

    [Test]
    public void TestMemoryRootGetDirectoryEntries()
    {
        var (_, res) = CreateRes();

        var testRoot = new MemoryContentRoot();
        testRoot.AddOrUpdateFile(new ResPath("/a.txt"), "a"u8.ToArray());
        testRoot.AddOrUpdateFile(new ResPath("/b.txt"), "b"u8.ToArray());
        testRoot.AddOrUpdateFile(new ResPath("/c.txt"), "c"u8.ToArray());
        testRoot.AddOrUpdateFile(new ResPath("/d/foo.txt"), "foo"u8.ToArray());

        res.AddRoot(new ResPath("/"), testRoot);

        Assert.Multiple(() =>
        {
            Assert.That(res.ContentGetDirectoryEntries(new ResPath("/")), Is.EquivalentTo(new[]
            {
                "a.txt",
                "b.txt",
                "c.txt",
                "d/"
            }));

            // Listing should work both with and without a trailing /
            Assert.That(res.ContentGetDirectoryEntries(new ResPath("/d")), Is.EquivalentTo(new[]
            {
                "foo.txt"
            }));

            Assert.That(res.ContentGetDirectoryEntries(new ResPath("/d/")), Is.EquivalentTo(new[]
            {
                "foo.txt"
            }));
        });
    }

    /// <summary>
    /// Test that a mount path is properly shown as a directory entry.
    /// </summary>
    [Test]
    public void TestGetDirectoryEntriesTwoMounts()
    {
        var (_, res) = CreateRes();

        var testRoot = new MemoryContentRoot();
        testRoot.AddOrUpdateFile(new ResPath("foo.txt"), Array.Empty<byte>());
        testRoot.AddOrUpdateFile(new ResPath("bar/foo.txt"), Array.Empty<byte>());

        res.AddRoot(new ResPath("/"), testRoot);
        res.AddRoot(new ResPath("/second"), testRoot);
        res.AddRoot(new ResPath("/third/fourth"), testRoot);

        Assert.That(res.ContentGetDirectoryEntries(new ResPath("/")), Is.EquivalentTo(new []
        {
            "foo.txt",
            "bar/",
            "second/",
            "third/"
        }));
    }

    private static (IDependencyCollection, IResourceManagerInternal) CreateRes()
    {
        var deps = new DependencyCollection();
        deps.Register<IResourceManager, ResourceManager>();
        deps.Register<IResourceManagerInternal, ResourceManager>();
        deps.Register<IConfigurationManager, ConfigurationManager>();
        deps.Register<ILogManager, LogManager>();
        deps.Register<IGameTiming, GameTiming>();

        deps.BuildGraph();
        return (deps, deps.Resolve<IResourceManagerInternal>());
    }
}
