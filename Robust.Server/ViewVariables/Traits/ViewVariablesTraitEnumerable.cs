using System.Collections;
using System.Collections.Generic;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Server.ViewVariables.Traits
{
    internal sealed class ViewVariablesTraitEnumerable : ViewVariablesTrait
    {
        private readonly List<object?> _cache = new();
        private IEnumerator? _enumerator;
        private readonly IEnumerable _enumerable;
        private bool Ended => _enumerator == null;

        public ViewVariablesTraitEnumerable(IViewVariablesSession session) : base(session)
        {
            _enumerable = (IEnumerable) session.Object;
            _refresh();
        }

        public override ViewVariablesBlob? DataRequest(ViewVariablesRequest viewVariablesRequest)
        {
            if (viewVariablesRequest is ViewVariablesRequestEnumerable requestEnumerable)
            {
                if (requestEnumerable.Refresh)
                {
                    _refresh();
                }
                _cacheTo(requestEnumerable.ToIndex);
                var list = new List<object?>();

                for (var i = requestEnumerable.FromIndex; i < _cache.Count && i <= requestEnumerable.ToIndex; i++)
                {
                    list.Add(MakeValueNetSafe(_cache[i]));
                }

                return new ViewVariablesBlobEnumerable {Objects = list};
            }

            return base.DataRequest(viewVariablesRequest);
        }

        public override bool TryGetRelativeObject(object property, out object? value)
        {
            if (!(property is ViewVariablesEnumerableIndexSelector selector))
            {
                return base.TryGetRelativeObject(property, out value);
            }

            if (_cache.Count > selector.Index)
            {
                value = _cache[selector.Index];
                return true;
            }

            _cacheTo(selector.Index);

            if (_cache.Count > selector.Index)
            {
                value = _cache[selector.Index];
                return true;
            }

            value = default;
            return false;
        }

        private void _cacheTo(int index)
        {
            if (Ended || index < _cache.Count)
            {
                return;
            }

            DebugTools.AssertNotNull(_enumerator);
            while (_cache.Count <= index)
            {
                if (!_enumerator!.MoveNext())
                {
                    _enumerator = null;
                    break;
                }
                _cache.Add(_enumerator!.Current);
            }
        }

        private void _refresh()
        {
            _cache.Clear();
            _enumerator = _enumerable.GetEnumerator();
        }
    }
}
