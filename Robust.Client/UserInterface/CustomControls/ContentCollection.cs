using System.Collections;
using System.Collections.Generic;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls;

public abstract class ContentCollection<T> : Control where T : Control
{
    public ContentCollection()
    {
        MouseFilter = MouseFilterMode.Stop;

        Contents = this;

        XamlChildren = new SS14ContentCollection(this);
    }

    public Control Contents { get; set; }

    public sealed class SS14ContentCollection : ICollection<Control>, IReadOnlyCollection<Control>
    {

        private readonly ContentCollection<T> Owner;

        public SS14ContentCollection(ContentCollection<T> owner)
        {
            Owner = owner;
        }

        public Enumerator GetEnumerator()
        {
            return new(Owner);
        }

        IEnumerator<Control> IEnumerable<Control>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(Control item)
        {
            Owner.Contents.AddChild(item);
        }

        public void Clear()
        {
            Owner.Contents.RemoveAllChildren();
        }

        public bool Contains(Control item)
        {
            return item?.Parent == Owner.Contents;
        }

        public void CopyTo(Control[] array, int arrayIndex)
        {
            Owner.Contents.Children.CopyTo(array, arrayIndex);
        }

        public bool Remove(Control item)
        {
            if (item?.Parent != Owner.Contents)
            {
                return false;
            }

            DebugTools.AssertNotNull(Owner?.Contents);
            Owner!.Contents.RemoveChild(item);

            return true;
        }

        int ICollection<Control>.Count => Owner.Contents.ChildCount;
        int IReadOnlyCollection<Control>.Count => Owner.Contents.ChildCount;

        public bool IsReadOnly => false;


        public struct Enumerator : IEnumerator<Control>
        {
            private OrderedChildCollection.Enumerator _enumerator;

            internal Enumerator(ContentCollection<T> contentCollection)
            {
                _enumerator = contentCollection.Contents.Children.GetEnumerator();
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                _enumerator.Reset();
            }

            public Control Current => _enumerator.Current;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                _enumerator.Dispose();
            }
        }
    }
}
