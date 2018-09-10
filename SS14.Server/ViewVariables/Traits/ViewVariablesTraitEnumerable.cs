using System.Collections;
using System.Collections.Generic;
using SS14.Shared.ViewVariables;

namespace SS14.Server.ViewVariables.Traits
{
    internal class ViewVariablesTraitEnumerable : ViewVariablesTrait
    {
        private readonly List<object> _cache = new List<object>();
        private IEnumerator _enumerator;
        private readonly IEnumerable _enumerable;
        private bool Ended => _enumerator == null;

        public ViewVariablesTraitEnumerable(ViewVariablesSession session) : base(session)
        {
            _enumerable = (IEnumerable) session.Object;
            _refresh();
        }

        public override ViewVariablesBlob DataRequest(ViewVariablesRequest viewVariablesRequest)
        {
            if (viewVariablesRequest is ViewVariablesRequestEnumerable requestEnumerable)
            {
                if (requestEnumerable.Refresh)
                {
                    _refresh();
                }
                _cacheTo(requestEnumerable.ToIndex);
                var list = new List<object>();

                for (var i = requestEnumerable.FromIndex; i < _cache.Count && i <= requestEnumerable.ToIndex; i++)
                {
                    list.Add(_cache[i]);
                }

                return new ViewVariablesBlobEnumerable {Objects = list};
            }

            return base.DataRequest(viewVariablesRequest);
        }

        public override bool TryGetRelativeObject(object property, out object value)
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

            value = default(object);
            return false;
        }

        private void _cacheTo(int index)
        {
            if (Ended || index < _cache.Count)
            {
                return;
            }

            while (_cache.Count <= index)
            {
                if (!_enumerator.MoveNext())
                {
                    _enumerator = null;
                    break;
                }
                _cache.Add(_enumerator.Current);
            }
        }

        private void _refresh()
        {
            _enumerator = _enumerable.GetEnumerator();
        }
    }
}
