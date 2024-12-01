using System;
using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Reflection;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Toolshed;

public sealed class ToolshedValidationTest : ToolshedTest
{
#if DEBUG
    [Test]
    public void TestMethodValidation()
    {
        // All if these commands are invalid for some reason, and should trip debug asserts
        // Theres probably a better way to do this, but these need to have IReflectionManager discoverability disabled
        Type[] types =
        [
            typeof(TestInvalid1Command),
            typeof(TestInvalid2Command),
            typeof(TestInvalid3Command),
            typeof(TestInvalid4Command),
            typeof(TestInvalid5Command),
            typeof(TestInvalid6Command),
            typeof(TestInvalid7Command),
            typeof(TestInvalid8Command),
            typeof(TestInvalid9Command),
            typeof(TestInvalid10Command),
            typeof(TestInvalid11Command),
            typeof(TestInvalid12Command),
            typeof(TestInvalid13Command),
            typeof(TestInvalid14Command),
            typeof(TestInvalid15Command),
            typeof(TestInvalid16Command),
            typeof(TestInvalid17Command),
            typeof(TestInvalid18Command)
        ];

        Assert.Multiple(() =>
        {
            foreach (var type in types)
            {
                var instance = (ToolshedCommand)Activator.CreateInstance(type)!;
                Assert.Throws<InvalidCommandImplementation>(instance.Init, $"{type.PrettyName()} did not throw a {nameof(InvalidCommandImplementation)} exception");
            }
        });
    }
#endif
}


#region InvalidCommands
// Not enough type argument parsers
[ToolshedCommand, Reflect(false)]
public sealed class TestInvalid1Command : ToolshedCommand
{
    public override Type[] TypeParameterParsers => [typeof(TypeTypeParser)];
    [CommandImplementation] public void Impl<T1, T2>() {}
}

// too many type argument parsers
[ToolshedCommand, Reflect(false)]
public sealed class TestInvalid2Command : ToolshedCommand
{
    public override Type[] TypeParameterParsers => [typeof(TypeTypeParser), typeof(TypeTypeParser)];
    [CommandImplementation]
    public void Impl<T1>() {}
}

// The generic has to be the LAST entry, not the first
[ToolshedCommand, Reflect(false)]
public sealed class TestInvalid3Command : ToolshedCommand
{
    public override Type[] TypeParameterParsers => [typeof(TypeTypeParser)];
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public void Impl<T1, T2>([PipedArgument] T1 i) {}
}

[ToolshedCommand, Reflect(false)]
public sealed class TestInvalid4Command : ToolshedCommand
{
    public override Type[] TypeParameterParsers => [typeof(TypeTypeParser)];
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public void Impl<T1, T2>([PipedArgument] IEnumerable<T1> i) {}
}

// [TakesPipedTypeAsGeneric] without a [PipedArgument]
[ToolshedCommand, Reflect(false)]
public sealed class TestInvalid5Command : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public void Impl<T>() {}
}

// Duplicate [PipedArgument]
[ToolshedCommand, Reflect(false)]
public sealed class TestInvalid6Command : ToolshedCommand
{
    [CommandImplementation]
    public void Impl([PipedArgument] int arg1, [PipedArgument] int arg2) {}
}

// Conflicting arguments
[ToolshedCommand, Reflect(false)]
public sealed class TestInvalid7Command : ToolshedCommand
{
    [CommandImplementation]
    public void Impl([CommandArgument, PipedArgument] int arg1) {}
}

// Conflicting arguments
[ToolshedCommand, Reflect(false)]
public sealed class TestInvalid8Command : ToolshedCommand
{
    [CommandImplementation]
    public void Impl([CommandInvocationContext, PipedArgument] int arg1) {}
}

// wrong [CommandInvocationContext] type
[ToolshedCommand, Reflect(false)]
public sealed class TestInvalid9Command : ToolshedCommand
{
    [CommandImplementation]
    public void Impl([CommandInvocationContext] int arg1) {}
}

// wrong [CommandInverted] type
[ToolshedCommand, Reflect(false)]
public sealed class TestInvalid10Command : ToolshedCommand
{
    [CommandImplementation]
    public void Impl([CommandInverted] int arg1) {}
}

// duplicate [CommandInverted]
[ToolshedCommand, Reflect(false)]
public sealed class TestInvalid11Command : ToolshedCommand
{
    [CommandImplementation]
    public void Impl([CommandInverted] bool arg1, [CommandInverted] bool arg2) {}
}

// duplicate [CommandInvocationContext]
[ToolshedCommand, Reflect(false)]
public sealed class TestInvalid12Command : ToolshedCommand
{
    [CommandImplementation]
    public void Impl([CommandInvocationContext] IInvocationContext arg1, [CommandInvocationContext] IInvocationContext arg2) {}
}

// Too few type parsers, along with a TakesPipedTypeAsGeneric
[ToolshedCommand, Reflect(false)]
public sealed class TestInvalid13Command : ToolshedCommand
{
    public override Type[] TypeParameterParsers => [typeof(TypeTypeParser)];
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public void Impl<T1, T2, T3>([PipedArgument] T3 i) {}
}

// Too many type parsers, along with a TakesPipedTypeAsGeneric
[ToolshedCommand, Reflect(false)]
public sealed class TestInvalid14Command : ToolshedCommand
{
    public override Type[] TypeParameterParsers => [typeof(TypeTypeParser), typeof(TypeTypeParser)];
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public void Impl<T1, T2>([PipedArgument] T2 i) {}
}

// [TakesPipedTypeAsGeneric] on a non-generic metod
[ToolshedCommand, Reflect(false)]
public sealed class TestInvalid15Command : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public void Impl([PipedArgument] int i) {}
}

// type arguments on a non-generic metod
[ToolshedCommand, Reflect(false)]
public sealed class TestInvalid16Command : ToolshedCommand
{
    public override Type[] TypeParameterParsers => [typeof(TypeTypeParser)];
    [CommandImplementation] public void Impl() {}
}

// Duplicate mixed explicit/implicit [CommandInvocationContext]
[ToolshedCommand, Reflect(false)]
public sealed class TestInvalid17Command : ToolshedCommand
{
    [CommandImplementation]
    public void Impl([CommandInvocationContext] IInvocationContext arg1, IInvocationContext arg2) {}
}


// Duplicate implicit [CommandInvocationContext]
[ToolshedCommand, Reflect(false)]
public sealed class TestInvalid18Command : ToolshedCommand
{
    [CommandImplementation]
    public void Impl(IInvocationContext arg1, IInvocationContext arg2) {}
}

#endregion


