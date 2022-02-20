using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Robust.Shared.Console;

public class CommandBuffer
{
    private const string DelayMarker = "-DELAY-";

    private int _tickrate = 0;
    private int _delay = 0;

    private readonly LinkedList<string> _commandBuffer = new();

    public void Append(string command)
    {
        _commandBuffer.AddLast(command);
    }

    public void Insert(string command)
    {
        _commandBuffer.AddFirst(command);
    }

    public void Tick(byte tickRate)
    {
        _tickrate = tickRate;

        if (_delay > 0)
        {
            _delay -= 1;
        }
    }

    public bool TryGetCommand([MaybeNullWhen(false)]out string command)
    {
        var next = _commandBuffer.First;

        if (next is null) // nothing to do here
        {
            command = null;
            return false;
        }

        if (next.Value.Equals(DelayMarker))
        {
            if (_delay == 0) // just finished
            {
                _commandBuffer.RemoveFirst();
                return TryGetCommand(out command);
            }
            else // currently counting down delay
            {
                command = null;
                return false;
            }
        }

        if (next.Value.StartsWith("wait "))
        {
            var sTicks = next.Value.Substring(5);
            _commandBuffer.RemoveFirst();
            if (string.IsNullOrWhiteSpace(sTicks) || !int.TryParse(sTicks, out var ticks)) // messed up command
            {
                return TryGetCommand(out command);
            }

            // Setup Timing
            _commandBuffer.AddFirst(DelayMarker);
            _delay = ticks;

            command = null;
            return false;
        }

        // normal command
        _commandBuffer.RemoveFirst();
        command = next.Value;
        return true;
    }
}
