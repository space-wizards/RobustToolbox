using System.Reflection;

namespace Robust.UnitTesting.Shared;

internal abstract class OurRobustUnitTest : RobustUnitTest
{
    protected override Assembly[] GetContentAssemblies()
    {
        return [typeof(OurRobustUnitTest).Assembly];
    }
}
