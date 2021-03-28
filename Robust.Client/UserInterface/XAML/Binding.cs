using System;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Avalonia.Data
{
    [UsedImplicitly]
    public class Binding
    {
        public Binding()
        {
            throw new InvalidOperationException(
                "Data binding is not currently supported and this type exists only to make Rider work.");
        }
    }
}
