using System.Collections.Generic;
using JetBrains.Annotations;

namespace SS14.Client.UserInterface
{
    /// <summary>
    ///     As part of my evil plan to convince Monster to work on SS14, I'm porting web technologies to SS14.
    ///     Here's CSS style sheets.
    /// </summary>
    public sealed class Stylesheet
    {
        private IReadOnlyList<StyleRule> Rules { get; }

        public Stylesheet(IReadOnlyList<StyleRule> rules)
        {
            Rules = rules;
        }
    }

    /// <summary>
    ///     A single rule in a style sheet, containing a bunch of properties and a selector.
    /// </summary>
    public sealed class StyleRule
    {
        public StyleRule(Selector selector, IReadOnlyList<StyleProperty> properties)
        {
            Selector = selector;
            Properties = properties;
        }

        public Selector Selector { get; }
        public IReadOnlyList<StyleProperty> Properties { get; }
    }

    /// <summary>
    ///     A single property in a rule, with a name and an object value.
    /// </summary>
    public readonly struct StyleProperty
    {
        public StyleProperty(string name, object value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public object Value { get; }
    }

    public abstract class Selector
    {
        public abstract bool Matches(Control control);
    }

    public sealed class SelectorElement : Selector
    {
        public SelectorElement(
            string elementType,
            IReadOnlyCollection<string> elementClasses,
            string elementId)
        {
            ElementType = elementType;
            ElementClasses = elementClasses;
            ElementId = elementId;
        }

        public string ElementType { get; }
        public IReadOnlyCollection<string> ElementClasses { get; }
        public string ElementId { get; }

        public override bool Matches(Control control)
        {
            if (ElementId != null && control.StyleIdentifier != ElementId)
            {
                return false;
            }

            if (ElementType != null && control.GetType().Name != ElementType)
            {
                return false;
            }

            if (ElementClasses != null)
            {
                foreach (var elementClass in ElementClasses)
                {
                    if (!control.HasStyleClass(elementClass))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    public sealed class SelectorDescendant : Selector
    {
        public SelectorDescendant([NotNull] Selector ascendant, [NotNull] Selector descendant)
        {
            Ascendant = ascendant;
            Descendant = descendant;
        }

        public Selector Ascendant { get; }
        public Selector Descendant { get; }

        public override bool Matches(Control control)
        {
            if (!Descendant.Matches(control))
            {
                return false;
            }

            var parent = control.Parent;
            var found = false;
            while (parent != null)
            {
                if (Ascendant.Matches(parent))
                {
                    found = true;
                    break;
                }

                parent = parent.Parent;
            }

            return found;
        }
    }

    public sealed class SelectorChild : Selector
    {
        public SelectorChild(Selector parent, Selector child)
        {
            Parent = parent;
            Child = child;
        }

        public Selector Parent { get; }
        public Selector Child { get; }

        public override bool Matches(Control control)
        {
            if (control.Parent == null)
            {
                return false;
            }

            return Parent.Matches(control.Parent) && Child.Matches(control);
        }
    }
}
