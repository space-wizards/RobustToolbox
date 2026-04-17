using System;
using Robust.Shared.IoC;

namespace Robust.Client.PiShockHook;

[AttributeUsage(AttributeTargets.Assembly)]
internal sealed class PiShockManagerImplAttribute : Attribute
{
    public readonly Type ImplementationType;

    public PiShockManagerImplAttribute(Type implementationType)
    {
        ImplementationType = implementationType;
    }
}

internal interface IPiShockManagerHook
{
    void PreInitialize(IDependencyCollection dependencies);
    void Initialize();
}
