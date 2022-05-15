using System;

namespace Robust.Shared.Console.ReflectionCommands;

public sealed class RegisterCommandAttribute : Attribute
{
    public readonly string Command;

    public RegisterCommandAttribute(string command)
    {
        Command = command;
    }
}
