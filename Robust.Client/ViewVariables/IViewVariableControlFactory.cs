using System;

namespace Robust.Client.ViewVariables;

public interface IViewVariableControlFactory
{
    VVPropEditor CreateFor(Type? type);
}
