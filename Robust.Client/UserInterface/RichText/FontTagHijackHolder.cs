using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Robust.Client.UserInterface.RichText;

/// <returns>The font to replace the lookup with. Return null to fall back to default behavior.</returns>
/// <seealso cref="FontTagHijackHolder"/>
public delegate Font? FontTagHijack(ProtoId<FontPrototype> protoId, int size);

/// <summary>
/// Allows replacing font resolution done by <see cref="FontPrototype"/>
/// </summary>
public sealed class FontTagHijackHolder
{
    [Dependency] private readonly IUserInterfaceManager _ui = null!;

    /// <summary>
    /// Called when a font prototype gets resolved.
    /// </summary>
    public FontTagHijack? Hijack;

    /// <summary>
    /// Indicate that the results of <see cref="Hijack"/> may have changed,
    /// and that engine things relying on it must be updated.
    /// </summary>
    public void HijackUpdated()
    {
        // This isn't fool-proof, but it's probably good enough.
        // Recursively navigate the UI tree and invalidate rich text controls.
        var queue = new Queue<Control>();

        foreach (var root in _ui.AllRoots)
        {
            queue.Enqueue(root);
        }

        while (queue.TryDequeue(out var control))
        {
            foreach (var child in control.Children)
            {
                queue.Enqueue(child);
            }

            if (control is OutputPanel output)
                output._invalidateEntries();
            else if (control is RichTextLabel label)
                label.InvalidateMeasure();
        }
    }
}
