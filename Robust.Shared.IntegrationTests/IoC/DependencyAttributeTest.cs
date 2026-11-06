using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.UnitTesting;

namespace Robust.Shared.IntegrationTests.IoC;

[Parallelizable(ParallelScope.All)]
[TestFixture]
[TestOf(typeof(ConfigurationIntegrationTest))]
internal sealed partial class ConfigurationIntegrationTest : RobustIntegrationTest
{
    internal interface IDepA;

    public sealed class DepA;
    public sealed class DepB;
    public sealed class DepC;
    public sealed class DepD;

    public sealed partial class Test1
    {
        [Dependency] public DepA A = null!;
        [Dependency(IoCMode.Optional)] public DepB? B = null!;
        [Dependency(IoCMode.Differed)] public DepC C = null!;
        [Dependency(IoCMode.DifferedOptional)] public DepD? D = null!;
    }

    [Test]
    public async Task TestSaveNoWarningServer()
    {
        DependencyCollection deps = new();
    }
}
