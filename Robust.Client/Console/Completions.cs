using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Robust.Shared.Network.Messages;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using static Robust.Shared.Network.Messages.MsgScriptCompletionResponse;

namespace Robust.Client.Console
{
    public class Completions : SS14Window
    {
        private HistoryLineEdit _textBar;
        private ScrollContainer _suggestPanel = new()
        {
            HScrollEnabled = false,
        };

        private BoxContainer _suggestBox = new()
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalAlignment = HAlignment.Left,
        };

        public Completions(HistoryLineEdit textBar) : base()
        {
            Title = "Suggestions";
            MouseFilter = MouseFilterMode.Pass;

            _textBar = textBar;
            _suggestPanel.AddChild(_suggestBox);
            ContentsContainer.AddChild(_suggestPanel);
        }

        private bool _firstopen = true;
        public void OpenAt(Vector2 position, Vector2 size)
        {
            if (_firstopen)
            {
                SetSize = size;
                LayoutContainer.SetPosition(this, position);
                _firstopen = false;
            }
            Open();
        }

        private ImmutableArray<LiteResult> _results;

        public void Update()
        {
            _suggestBox.RemoveAllChildren();
            foreach (var res in _results)
            {
                var label = new Entry(res);

                label.OnKeyBindDown += ev =>
                {
                    if (ev.Function == EngineKeyFunctions.UIClick)
                        _textBar.InsertAtCursor(label.Result.Properties["InsertionText"]);
                };

                _suggestBox.AddChild(label);
            }
        }

        public void TextChanged() => Close();

        public void SetSuggestions(MsgScriptCompletionResponse response)
        {
            _results = response.Results;
            Update();
        }

        // Label and ghetto button.
        public class Entry : RichTextLabel
        {
            public readonly LiteResult Result;

            public Entry(LiteResult result)
            {
                MouseFilter = MouseFilterMode.Stop;
                Result = result;
                var compl = new List<Section>();
                var dim = Color.FromHsl((0f, 0f, 0.8f, 1f));

                // warning: ew ahead
                string basen = "default";
                if (Result.Tags.Contains("Interface"))
                    basen = "interface name";
                else if (Result.Tags.Contains("Class"))
                    basen = "class name";
                else if (Result.Tags.Contains("Struct"))
                    basen = "struct name";
                else if (Result.Tags.Contains("Keyword"))
                    basen = "keyword";
                else if (Result.Tags.Contains("Namespace"))
                    basen = "namespace name";
                else if (Result.Tags.Contains("Method"))
                    basen = "method name";
                else if (Result.Tags.Contains("Property"))
                    basen = "property name";
                else if (Result.Tags.Contains("Field"))
                    basen = "field name";

                Color basec = ScriptingColorScheme.ColorScheme[basen];
                Color dimmed = basec * dim;
                compl.AddRange(new []
                        {
                            new Section() { Color=dimmed.ToArgb(), Content=Result.DisplayTextPrefix },
                            new Section() { Color=basec.ToArgb(), Content=Result.DisplayText },
                            new Section() { Color=dimmed.ToArgb(), Content=Result.DisplayTextSuffix }
                        }
                );

                compl.Add(new Section() { Color=dimmed.ToArgb(), Content=" [" + String.Join(", ", Result.Tags) + "]" });
                if (Result.InlineDescription.Length != 0)
                {
                    compl.AddRange(new []
                            {
                                new Section() { Color=(basec * dim).ToArgb(), Content="\n: " },
                                new Section() { Color=Color.LightSlateGray.ToArgb(), Content=Result.InlineDescription }
                            }
                    );
                }
                SetMessage(new FormattedMessage(compl.ToArray()));
            }
        }
    }
}
