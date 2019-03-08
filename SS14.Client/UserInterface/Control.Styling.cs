using System.Collections.Generic;
using System.Linq;
using SS14.Shared.ViewVariables;

namespace SS14.Client.UserInterface
{
    // ReSharper disable once RequiredBaseTypesIsNotInherited
    public partial class Control
    {
        public const string StylePropertyModulateSelf = "modulate-self";

        private readonly Dictionary<string, object> _styleProperties = new Dictionary<string, object>();
        private readonly HashSet<string> _styleClasses = new HashSet<string>();
        public IReadOnlyCollection<string> StyleClasses => _styleClasses;

        private string _styleIdentifier;

        [ViewVariables]
        public string StyleIdentifier
        {
            get => _styleIdentifier;
            set
            {
                _styleIdentifier = value;
                Restyle();
            }
        }

        private string _stylePseudoClass;

        [ViewVariables]
        public string StylePseudoClass
        {
            get => _stylePseudoClass;
            protected set
            {
                if (_stylePseudoClass == value)
                {
                    return;
                }
                _stylePseudoClass = value;
                Restyle();
            }
        }

        public bool HasStyleClass(string className)
        {
            return _styleClasses.Contains(className);
        }

        public void AddStyleClass(string className)
        {
            _styleClasses.Add(className);
            Restyle();
        }

        public void RemoveStyleClass(string className)
        {
            _styleClasses.Remove(className);
            Restyle();
        }

        private void Restyle(bool doChildren = true)
        {
            _styleProperties.Clear();

            // TODO: probably gonna need support for multiple stylesheets.
            var stylesheet = UserInterfaceManager.Stylesheet;
            if (stylesheet == null)
            {
                return;
            }

            // Get all rules that apply to us, sort them and apply they params again.
            var ruleList = new List<(StyleRule rule, int index)>();
            var count = 0;
            foreach (var rule in stylesheet.Rules)
            {
                if (!rule.Selector.Matches(this))
                {
                    continue;
                }

                ruleList.Add((rule, count));
                count += 1;
            }

            // Sort by specificity.
            // The index is there to sort by if specificity is the same, in which case the last takes precedence.
            ruleList.Sort((a, b) =>
            {
                var cmp = a.rule.Specificity.CompareTo(b.rule.Specificity);
                // Reverse this sort so that high specificity is at the TOP.
                return -(cmp != 0 ? cmp : a.index.CompareTo(b.index));
            });

            // Go over each rule.
            foreach (var (rule, _) in ruleList)
            {
                foreach (var property in rule.Properties)
                {
                    if (_styleProperties.ContainsKey(property.Name))
                    {
                        // Since we've sorted by priority in reverse,
                        // the first ones to get applied have highest priority.
                        // So if we have a duplicate it's always lower priority and we can discard it.
                        continue;
                    }

                    _styleProperties[property.Name] = property.Value;
                }
            }

            StylePropertiesChanged();

            if (doChildren)
            {
                foreach (var child in _orderedChildren.ToList())
                {
                    child.Restyle(false);
                }
            }
        }

        protected virtual void StylePropertiesChanged()
        {
            MinimumSizeChanged();
        }

        public bool TryGetStyleProperty<T>(string param, out T value)
        {
            if (_styleProperties.TryGetValue(param, out var val))
            {
                value = (T) val;
                return true;
            }

            value = default;
            return false;
        }
    }
}
