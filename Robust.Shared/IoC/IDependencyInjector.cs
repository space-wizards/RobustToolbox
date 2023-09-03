using System;

namespace Robust.Shared.IoC;

public interface IDependencyInjector
{
    Type[] ReportDependencies();
    void InjectDependencies(ReadOnlySpan<object> dependencies);
}
