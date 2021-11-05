using System;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Robust.Client.UserInterface.Controls
{
    public class Collapsible : BoxContainer
    {
        public BaseButton? Heading { get; private set; }
        public Control? Body { get; private set; }

        private bool _initialized = false;

        public string? Title { get; set; }

        private bool _bodyVisible;
        public bool BodyVisible
        {
            get => _bodyVisible;
            set
            {
                _bodyVisible = value;

                if (Heading != null && Body != null)
                {
                    Heading.Pressed = value;
                    Body.Visible = value;
                }
            }
        }

        public Collapsible()
        {}

        public Collapsible(CollapsibleHeading header, CollapsibleBody body)
        {
            AddChild(header);
            AddChild(body);

            Initialize();
        }

        public Collapsible(string title, CollapsibleBody body)
        {
            Title = title;
            AddChild(body);

            Initialize();
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            if (!_initialized) Initialize();

            base.Draw(handle);
        }

        private void Initialize()
        {
            var enumerator = Children.GetEnumerator();
            enumerator.MoveNext();

            if (Title == null)
            {
                // downcast
                if (enumerator.Current is not BaseButton heading
                    || !heading.ToggleMode)
                    throw new ArgumentException("No toggle button defined in Collapsible, or title is missing");

                Heading = heading;
            }
            else
            {
                Heading = new CollapsibleHeading(Title);

            }

            if (!enumerator.MoveNext())
                throw new ArgumentException("Not enough children in Collapsible");

            Body = enumerator.Current;
            BodyVisible = _bodyVisible;
            Heading.Pressed = _bodyVisible;

            if (enumerator.MoveNext())
                throw new ArgumentException("Too many children in Collapsible");

            Heading.OnToggled += args => BodyVisible = args.Pressed;

            // implies heading was created just now
            if (Heading.Parent == null)
            {
                this.AddChild(Heading);
                Heading.SetPositionFirst();
            }

            _initialized = true;


        }
    }

    public class CollapsibleHeading : ContainerButton
    {
        private const string StyleClassOptionTriangle = "optionTriangle";

        private bool _initialized;

        private TextureRect _chevron = new TextureRect
        {
            StyleClasses = { StyleClassOptionTriangle },
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
        };
        private bool _chevronVisibility;
        public bool ChevronVisible
        {
            get => _chevronVisibility;
            set
            {
                _chevronVisibility = value;
                _chevron.Visible = _chevronVisibility;
            }
        }

        private Thickness _chevronMargin;
        public Thickness ChevronMargin
        {
            get => _chevronMargin;
            set
            {
                _chevronMargin = value;
                _chevron.Margin = _chevronMargin;
            }
        }

        private BoxContainer _box = new();

        public LayoutOrientation Orientation
        {
            get => _box.Orientation;
            set => _box.Orientation = value;
        }

        public CollapsibleHeading()
        {
            ToggleMode = true;
            this.AddChild(_box);
            _box.AddChild(_chevron);
        }

        public CollapsibleHeading(string title) : this()
        {
            this.AddChild(new Label { Text = title });
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            if (!_initialized) Initialize();

            base.Draw(handle);
        }

        private void Initialize()
        {
            var current = new List<Control>(Children);

            foreach (var control in current)
            {
                if (control == _box) continue;
                ReparentToBox(control);
            }

            _initialized = true;
        }

        private void ReparentToBox<T>(T control) where T : Control
        {
            control.Orphan();
            _box.AddChild(control);
        }
    }

    public class CollapsibleBody : Container
    {
        public CollapsibleBody()
        {
            this.Visible = false;
        }
    }
}
