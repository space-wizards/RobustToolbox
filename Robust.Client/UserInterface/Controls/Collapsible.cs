using System;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    [Virtual]
    public class Collapsible : BoxContainer
    {
        public BaseButton? Heading { get; private set; }
        public Control? Body { get; private set; }

        private bool _initialized = false;

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
        {
            Orientation = LayoutOrientation.Vertical;
        }

        public Collapsible(CollapsibleHeading header, CollapsibleBody body) : this()
        {
            AddChild(header);
            AddChild(body);

            Initialize();
        }

        public Collapsible(string title, CollapsibleBody body) : this(new CollapsibleHeading(title), body)
        {

        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            if (!_initialized) Initialize();

            base.Draw(handle);
        }

        private void Initialize()
        {
            using var enumerator = Children.GetEnumerator();
            enumerator.MoveNext();

            // downcast
            if (enumerator.Current is not BaseButton heading
                || !heading.ToggleMode)
                throw new ArgumentException("No toggle button defined in Collapsible, or title is missing");

            Heading = heading;

            if (!enumerator.MoveNext())
                throw new ArgumentException("Not enough children in Collapsible");

            Body = enumerator.Current;
            BodyVisible = _bodyVisible;
            Heading.Pressed = _bodyVisible;

            if (enumerator.MoveNext())
                throw new ArgumentException("Too many children in Collapsible");

            Heading.OnToggled += args => BodyVisible = args.Pressed;

            _initialized = true;
        }
    }

    [Virtual]
    public class CollapsibleHeading : ContainerButton
    {
        private TextureRect _chevron = new TextureRect
        {
            StyleClasses = { OptionButton.StyleClassOptionTriangle },
            Margin = new Thickness(2, 0),
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
        };

        public bool ChevronVisible
        {
            get => _chevron.Visible;
            set => _chevron.Visible = value;
        }

        public Thickness ChevronMargin
        {
            get => _chevron.Margin;
            set => _chevron.Margin = value;
        }

        /// <summary>
        /// Exposes the label for this heading.
        /// </summary>
        public Label Label { get; }

        public string? Title
        {
            get => Label.Text;
            set => Label.Text = value;
        }

        public CollapsibleHeading()
        {
            ToggleMode = true;
            var box = new BoxContainer();
            AddChild(box);
            box.AddChild(_chevron);
            Label = new Label();
            box.AddChild(Label);
        }

        public CollapsibleHeading(string title) : this()
        {
            Title = title;
        }
    }

    [Virtual]
    public class CollapsibleBody : Container
    {
        public CollapsibleBody()
        {
            this.Visible = false;
        }
    }
}
