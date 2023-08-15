using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.RichText;

public interface IMarkupTag
{
    /// <summary>
    /// The string used as the tags name when writing rich text
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Called when an opening node for this tag is encountered.<br/>
    /// Used for pushing new values used for rendering text contained within this tag.<br/>
    /// Important: Push some kind of default value into the context or throw when missing a required parameter
    /// or attribute. The order in which state gets popped of the context breaks otherwise.
    /// </summary>
    /// <param name="node">The markup node containing the parameter and attributes</param>
    /// <param name="context">The context to push the state on</param>
    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
    }

    /// <summary>
    /// Called after <see cref="PushDrawContext"/>.<br/>
    /// Supplies text to be rendered before this tags children.
    /// </summary>
    /// <param name="node">The markup node containing the parameter and attributes</param>
    /// <returns>The text that gets rendered</returns>
    public string TextBefore(MarkupNode node)
    {
        return "";
    }

    /// <summary>
    /// Called after <see cref="TextBefore"/>, when encountering a closing node for this tag.<br/>
    /// Supplies text to be rendered after this controls children.
    /// </summary>
    /// <param name="node">The markup node containing the parameter and attributes</param>
    /// <returns>The text that gets rendered</returns>
    public string TextAfter(MarkupNode node)
    {
        return "";
    }
    /// <summary>
    /// Called after <see cref="TextAfter"/>.<br/>
    /// Used for popping values that got added by this tag from the drawing context.
    /// </summary>
    /// <param name="node">The markup node containing the parameter and attributes</param>
    /// <param name="context">The context to pop the state from</param>
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
    }

    /// <summary>
    /// Called inside the constructor of <see cref="RichTextEntry"/> to
    /// supply a control that gets rendered inline before this tags children<br/>
    /// Text continues to the right of the control until the next line and then continues bellow it
    /// </summary>
    /// <param name="node">The markup node containing the parameter and attributes</param>
    /// <param name="control">A UI control for placing in line with this tags children</param>
    /// <returns>true if this tag supplies a control</returns>
    public bool TryGetControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        return false;
    }

}
