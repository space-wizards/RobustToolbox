using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface
{
    /// <summary>
    ///     As part of my evil plan to convince Monster to work on SS14, I'm porting web technologies to Robust.
    ///     Here's CSS style sheets.
    /// </summary>
    /// <seealso cref="StylesheetHelpers"/>
    public sealed class Stylesheet
    {
        public IReadOnlyList<StyleRule> Rules { get; }

        // More organized forms of the above rules list, that is more efficient to query.
        internal List<(int order, StyleRule rule)> UnsortedRules { get; }
        internal Dictionary<Type, List<(int order, StyleRule rule)>> TypeSortedRules { get; }

        public Stylesheet(IReadOnlyList<StyleRule> rules)
        {
            Rules = rules;

            UnsortedRules = new List<(int order, StyleRule rule)>();
            TypeSortedRules = new Dictionary<Type, List<(int order, StyleRule rule)>>();

            var order = 0;
            var typeControl = typeof(Control);
            foreach (var rule in rules)
            {
                if (rule.Selector is SelectorElement element
                    && element.ElementType != null
                    && typeControl.IsAssignableFrom(element.ElementType)
                    && element.ElementType != typeControl)
                {
                    if (!TypeSortedRules.TryGetValue(element.ElementType, out var typeList))
                    {
                        typeList = new List<(int order, StyleRule rule)>();
                        TypeSortedRules.Add(element.ElementType, typeList);
                    }

                    typeList.Add((order, rule));
                }
                else
                {
                    UnsortedRules.Add((order, rule));
                }

                order += 1;
            }
        }
    }

    /// <summary>
    /// Helper methods to make construction of style sheets easier.
    /// </summary>
    public static class StylesheetHelpers
    {
        /// <summary>
        ///     Creates a blank mutable element selector.
        /// </summary>
        /// <seealso cref="SelectorElement"/>
        public static MutableSelectorElement Element()
        {
            return new();
        }

        /// <summary>
        ///     Creates a mutable element selector with <typeparamref name="T"/> as control type.
        /// </summary>
        /// <typeparam name="T">The type of control that the selector binds to.</typeparam>
        /// <seealso cref="SelectorElement"/>
        public static MutableSelectorElement Element<T>() where T : Control
        {
            return new() {Type = typeof(T)};
        }

        /// <summary>
        ///     Creates a new blank mutable child selector.
        /// </summary>
        /// <seealso cref="MutableSelectorChild"/>
        public static MutableSelectorChild Child()
        {
            return new();
        }
    }

    /// <summary>
    ///     Mutable selectors are convenience wrappers to make constructing style selectors cleaner.
    /// </summary>
    /// <remarks>
    ///     Mutable selectors can store style properties that will get used when converted to a style rule.
    ///     These can be added with <see cref="Prop"/>
    /// </remarks>
    public abstract class MutableSelector
    {
        private readonly List<StyleProperty> _props = new();

        public MutableSelector Prop(string key, object value)
        {
            _props.Add(new StyleProperty(key, value));
            return this;
        }

        /// <summary>
        ///     Converts a mutable selector into a style rule, using the properties added via <see cref="Prop"/>.
        /// </summary>
        public static implicit operator StyleRule(MutableSelector elem)
        {
            return new(elem.ToSelector(), elem._props);
        }

        public static implicit operator Selector(MutableSelector elem)
        {
            return elem.ToSelector();
        }

        protected abstract Selector ToSelector();
    }

    /// <summary>
    ///     Mutable selector for <see cref="SelectorElement"/>.
    /// </summary>
    /// <inheritdoc/>
    public sealed class MutableSelectorElement : MutableSelector
    {
        public Type? Type { get; set; }
        public List<string>? StyleClasses { get; set; }
        public string? StyleIdentifier { get; set; }
        public List<string>? PseudoClasses { get; set; }

        /// <summary>
        ///     Adds a set of style classes to this selector.
        /// </summary>
        public MutableSelectorElement Class(params string[] classes)
        {
            StyleClasses ??= new List<string>();
            StyleClasses.AddRange(classes);
            return this;
        }

        /// <summary>
        ///     Adds a single style class to this selector.
        /// </summary>
        public MutableSelectorElement Class(string @class)
        {
            StyleClasses ??= new List<string>();
            StyleClasses.Add(@class);
            return this;
        }

        /// <summary>
        ///     Adds a set of pseudo classes to this selector.
        /// </summary>
        public MutableSelectorElement Pseudo(params string[] classes)
        {
            PseudoClasses ??= new List<string>();
            PseudoClasses.AddRange(classes);
            return this;
        }

        /// <summary>
        ///     Adds a single pseudo class to this selector.
        /// </summary>
        public MutableSelectorElement Pseudo(string @class)
        {
            PseudoClasses ??= new List<string>();
            PseudoClasses.Add(@class);
            return this;
        }

        /// <summary>
        ///     Sets the style identifier on this selector.
        /// </summary>
        public MutableSelectorElement Identifier(string identifier)
        {
            StyleIdentifier = identifier;
            return this;
        }

        protected override Selector ToSelector()
        {
            return new SelectorElement(Type, StyleClasses, StyleIdentifier, PseudoClasses);
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <inheritdoc/>
    /// <remarks>
    ///     Both <see cref="Parent"/> and <see cref="Child"/> must be set before converting this to a proper selector,
    ///     or else an <see cref="InvalidOperationException"/> will be thrown.
    /// </remarks>
    public sealed class MutableSelectorChild : MutableSelector
    {
        public Selector? ParentSelector { get; set; }
        public Selector? ChildSelector { get; set; }

        public MutableSelectorChild()
        {
        }

        public MutableSelectorChild Parent(Selector parent)
        {
            ParentSelector = parent;
            return this;
        }

        public MutableSelectorChild Child(Selector child)
        {
            ChildSelector = child;
            return this;
        }

        protected override Selector ToSelector()
        {
            if (ParentSelector == null || ChildSelector == null)
            {
                throw new InvalidOperationException("Must initialize both parent and child.");
            }

            return new SelectorChild(ParentSelector, ChildSelector);
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
            Specificity = selector.CalculateSpecificity();
        }

        public StyleSpecificity Specificity { get; }
        public Selector Selector { get; }
        public IReadOnlyList<StyleProperty> Properties { get; }
    }

    // https://specifishity.com/
    public struct StyleSpecificity : IComparable<StyleSpecificity>, IComparable
    {
        public readonly int IdSelectors;
        public readonly int ClassSelectors;
        public readonly int TypeSelectors;

        public StyleSpecificity(int idSelectors, int classSelectors, int typeSelectors)
        {
            IdSelectors = idSelectors;
            ClassSelectors = classSelectors;
            TypeSelectors = typeSelectors;
        }

        public static StyleSpecificity operator +(StyleSpecificity a, StyleSpecificity b)
        {
            return new(
                a.IdSelectors + b.IdSelectors,
                a.ClassSelectors + b.ClassSelectors,
                a.TypeSelectors + b.TypeSelectors);
        }

        public int CompareTo(StyleSpecificity other)
        {
            var idSelectorsComparison = IdSelectors.CompareTo(other.IdSelectors);
            if (idSelectorsComparison != 0)
            {
                return idSelectorsComparison;
            }

            var classSelectorsComparison = ClassSelectors.CompareTo(other.ClassSelectors);
            if (classSelectorsComparison != 0)
            {
                return classSelectorsComparison;
            }

            return TypeSelectors.CompareTo(other.TypeSelectors);
        }

        public int CompareTo(object? obj)
        {
            if (ReferenceEquals(null, obj)) return 1;
            return obj is StyleSpecificity other
                ? CompareTo(other)
                : throw new ArgumentException($"Object must be of type {nameof(StyleSpecificity)}");
        }

        public override string ToString()
        {
            return $"({IdSelectors}-{ClassSelectors}-{TypeSelectors})";
        }
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
        public abstract StyleSpecificity CalculateSpecificity();
    }

    public sealed class SelectorElement : Selector
    {
        private readonly string[]? _elementClasses;
        private readonly string[]? _pseudoClasses;

        public SelectorElement(
            Type? elementType,
            IEnumerable<string>? elementClasses,
            string? elementId,
            IEnumerable<string>? pseudoClass)
        {
            if (elementType != null)
            {
                if (!typeof(Control).IsAssignableFrom(elementType))
                {
                    throw new ArgumentException("elementType must inherit Control.", nameof(elementType));
                }

                if (elementType == typeof(Control))
                {
                    throw new ArgumentException("elementType may not be Control itself.", nameof(elementType));
                }
            }

            ElementType = elementType;
            _elementClasses = elementClasses?.ToArray();
            ElementId = elementId;
            _pseudoClasses = pseudoClass?.ToArray();
        }

        public static SelectorElement Type(Type elementType)
        {
            return new(elementType, null, null, null);
        }

        public static SelectorElement Class(params string[] classes)
        {
            return new(null, classes, null, null);
        }

        public static SelectorElement Id(string id)
        {
            return new(null, null, id, null);
        }

        public Type? ElementType { get; }
        public IReadOnlyList<string>? ElementClasses => _elementClasses;
        public string? ElementId { get; }
        public IReadOnlyList<string>? PseudoClasses => _pseudoClasses;

        public override bool Matches(Control control)
        {
            if (ElementId != null && control.StyleIdentifier != ElementId)
            {
                return false;
            }

            if (ElementType != null && !ElementType.IsInstanceOfType(control))
            {
                return false;
            }

            if (_elementClasses != null)
            {
                foreach (var elementClass in _elementClasses)
                {
                    if (!control.HasStyleClass(elementClass))
                    {
                        return false;
                    }
                }
            }

            if (_pseudoClasses != null)
            {
                foreach (var elementClass in _pseudoClasses)
                {
                    if (!control.HasStylePseudoClass(elementClass))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public override StyleSpecificity CalculateSpecificity()
        {
            var countId = ElementId == null ? 0 : 1;
            var countClasses = (ElementClasses?.Count ?? 0) + (PseudoClasses?.Count ?? 0);
            var countTypes = 0;
            if (ElementType != null)
            {
                var type = ElementType;
                while (type != typeof(Control))
                {
                    DebugTools.AssertNotNull(type);
                    type = type!.BaseType;
                    countTypes += 1;
                }
            }

            return new StyleSpecificity(countId, countClasses, countTypes);
        }
    }

    // Temporarily hidden due to performance concerns.
    // Like seriously this thing is awful performance wise.
    // Also, you can't just enable it. The code is here but AddChild etc only do restyles one level deep.
    internal sealed class SelectorDescendant : Selector
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

        public override StyleSpecificity CalculateSpecificity()
        {
            return Ascendant.CalculateSpecificity() + Descendant.CalculateSpecificity();
        }
    }

    // Temporarily hidden due to performance concerns.
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

        public override StyleSpecificity CalculateSpecificity()
        {
            return Parent.CalculateSpecificity() + Child.CalculateSpecificity();
        }
    }
}
