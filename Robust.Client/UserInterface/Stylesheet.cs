using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface
{
    /// <summary>
    ///     As part of my evil plan to convince Monster to work on SS14, I'm porting web technologies to Robust.
    ///     Here's CSS style sheets.
    /// </summary>
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
            return new StyleSpecificity(
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

        public int CompareTo(object obj)
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
        public SelectorElement(
            Type elementType,
            IReadOnlyCollection<string> elementClasses,
            string elementId,
            IReadOnlyCollection<string> pseudoClass)
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
            ElementClasses = elementClasses;
            ElementId = elementId;
            _pseudoclass = pseudoClass != null ? new HashSet<string>(pseudoClass) : new HashSet<string>();
        }

        public Type ElementType { get; }
        public IReadOnlyCollection<string> ElementClasses { get; }
        public string ElementId { get; }

        private HashSet<string> _pseudoclass { get; set; }
        public IReadOnlyCollection<string> PseudoClass { get => _pseudoclass; }

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

            if (PseudoClass != null)
            {
                foreach (var elementClass in PseudoClass)
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
            var countClasses = (ElementClasses?.Count ?? 0) + PseudoClass.Count;
            var countTypes = 0;
            if (ElementType != null)
            {
                var type = ElementType;
                while (type != typeof(Control))
                {
                    DebugTools.AssertNotNull(type);
                    type = type.BaseType;
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
