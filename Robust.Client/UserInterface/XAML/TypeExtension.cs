using System;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Robust.Client.UserInterface.XAML.XNamespace
{
    // x:Type
    [UsedImplicitly]
    public sealed class TypeExtension
    {
        public TypeExtension(string _)
        {
            throw new InvalidOperationException(
                "This type only exists to make Rider work and should never be instantiated.");
        }

        public static object ProvideValue()
        {
            throw new InvalidOperationException();
        }
    }
}
