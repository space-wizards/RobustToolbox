namespace Robust.Shared.RichText;

/// <summary>
/// Extension methods for <see cref="FormattedStringBuilder"/>.
/// </summary>
public static class FormattedStringBuilderExtensions
{
    extension(FormattedStringBuilder builder)
    {
        /// <summary>
        /// Write a <c>cmdlink</c> tag.
        /// </summary>
        /// <param name="text">The user-visible tag for the link.</param>
        /// <param name="command">The command executed when the user clicks.</param>
        /// <param name="title">The tooltip (title) when the user hovers over the link.</param>
        /// <returns>The current instance, to enable easy method call chaining.</returns>
        public FormattedStringBuilder MakeCommandLinkTag(string text, string command, string? title = null)
        {
            builder.BeginTag("cmdlink", text);
            builder.TagAttribute("command", command);
            if (title != null)
                builder.TagAttribute("title", title);
            builder.FinishTagSelfClosed();

            return builder;
        }
    }
}
