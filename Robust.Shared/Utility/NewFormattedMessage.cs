using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.Utility
{
    [PublicAPI]
    [Serializable, NetSerializable]
    public sealed record NewFormattedMessage(Section[] Sections) : ISectionable
    {
        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var i in Sections)
                sb.Append(i.Content);

            return sb.ToString();
        }

        // I don't wanna fix the serializer yet.
        public string ToMarkup()
        {
            #warning NewFormattedMessage.ToMarkup is still lossy.
            var sb = new StringBuilder();
            foreach (var i in Sections)
            {
                if (i.Content.Length == 0)
                    continue;

                if (i.Color != default)
                    sb.AppendFormat("[color=#{0:X8}]",
                            // Bit twiddling to swap AARRGGBB to RRGGBBAA
                            ((i.Color << 8) & 0xFF_FF_FF_00) | // Drop alpha from the front
                            ((i.Color & 0xFF_00_00_00) >> 24)  // Shuffle it to the back
                            );

                sb.Append(i.Content);

                if (i.Color != default)
                    sb.Append("[/color]");
            }

            return sb.ToString();
        }

        public static readonly NewFormattedMessage Empty = new NewFormattedMessage(Array.Empty<Section>());

        public Section this[int i] { get => Sections[i]; }
        public int Length { get => Sections.Length; }

        [Obsolete("Construct NewFormattedMessage Sections manually.")]
        public class Builder
        {
            // _dirty signals that _sb has content that needs flushing to _work
            private bool _dirty = false;

            // We fake a stack by keeping an index in to the work list.
            // Since each Section contains all its styling info, we can "pop" the stack by
            // using the (unchanged) Section before it.
            private int _idx = 0;
            private StringBuilder _sb = new();

            // _work starts out with a dummy item because otherwise we break the assumption that
            // _idx will always refer to *something* in _work.
            private List<Section> _work = new() {
                new Section()
            };

            public static Builder FromNewFormattedMessage(NewFormattedMessage orig) => new ()
            {
                // Again, we always need at least one _work item, so if the FormattedMessage
                // is empty, we'll forge one.
                _idx = orig.Sections.Length < 0 ? orig.Sections.Length - 1 : 0,
                _work = new List<Section>(
                    orig.Sections.Length == 0 ?
                        new [] { new Section() }
                        : orig.Sections
                ),
            };

            // hmm what could this do
            public void Clear()
            {
                _dirty = false;
                _idx = 0;
                _work = new() {
                    new Section()
                };
                _sb = _sb.Clear();
            }

            // Since we don't change any styling, we don't need to add a full Section.
            // In these cases, we add it to the StringBuilder, and wait until styling IS changed,
            // or we Render().
            public void AddText(string text)
            {
                _dirty = true;
                _sb.Append(text);
            }

            // PushColor changes the styling, so we need to submit any text we had waiting, then
            // add a new empty Section with the new color.
            public void PushColor(Color color)
            {
                flushWork();

                var last = _work[_idx];
                last.Content = string.Empty;
                last.Color = color.ToArgb();
                _work.Add(last);
                _idx = _work.Count - 1;
            }

            // These next two are probably wildly bugged, since they'll include the other sections
            // wholesale, and the entire fake-stack facade breaks down, since there's no way for the
            // new stuff to inherit the previous style, and we don't know what parts of the style are
            // actually set, and what parts are just default values.

            // TODO: move _idx?
            public void AddMessage(NewFormattedMessage other)
            {
                flushWork();
                _work.AddRange(other.Sections);
                _idx = _work.Count-1;
            }

            // TODO: See above
            public void AddMessage(NewFormattedMessage.Builder other)
            {
                flushWork();
                AddMessage(other.Build());
                other.Clear();
            }

            // I wish I understood why this was needed...
            // Did people not know you could AddText("\n")?
            public void PushNewline()
            {
                _dirty = true;
                _sb.Append('\n');
            }

            // Flush any text we've got for the current style,
            // then roll back to the style before this one.
            public void Pop()
            {
                flushWork();
                // Go back one (or stay at the start)
                _idx = (_idx > 0) ? (_idx - 1) : 0;
            }

            public void flushWork()
            {
                // Nothing changed? Great.
                if (!_dirty)
                    return;

                // Get the last tag (for the style)...
                var last = _work[_idx];
                // ...and set the content to the current buffer
                last.Content = _sb.ToString();
                _work.Add(last);

                // Clean up
                _sb = _sb.Clear();
                _dirty = false;
            }

            public NewFormattedMessage Build()
            {
                flushWork();
                return new NewFormattedMessage(_work
                        .GetRange(1, _work.Count - 1)      // Drop the placeholder
                        .Where(e => e.Content.Length != 0) // and any blanks (which can happen from pushing colors and such)
                        .ToArray());
            }
        }
    }
}
