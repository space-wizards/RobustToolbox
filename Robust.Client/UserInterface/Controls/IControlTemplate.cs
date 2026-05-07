using System;

namespace Robust.Client.UserInterface.Controls;

public interface IControlTemplate
{
    Control Instantiate(object? data);
}

public sealed class ControlTemplateDelegate(Func<object?, Control> @delegate) : IControlTemplate
{
    public Control Instantiate(object? data)
    {
        return @delegate(data);
    }
}
