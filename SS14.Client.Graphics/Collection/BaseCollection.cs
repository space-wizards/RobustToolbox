using System;
using System.Collections;
using System.Collections.Generic;

namespace SS14.Client.Graphics.Collection
{
    /// <summary>
    /// An abstract class representing a standard collection of objects.
    /// </summary>
    /// <remarks>
    /// This was designed as a point of convenience to take some of the annoyance out of inheriting a collection.
    /// <para>
    /// This class provides a simplified method of inheriting a basic SortedList collection.  It holds no benefit over the System.Collections.Generic.SortedList object, which it uses internally.
    /// </para>
    /// 	<para>
    /// This class overloads properties to retrieve or remove an item by its index as well as by its key.  These are setup as protected methods so that the user can implement the collection however they see fit.
    /// </para>
    /// 	<para>
    /// This class, like the other collection classes, implements the IEnumerable interface already to return an iterator interface for enumeration.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">Type of data to store.</typeparam>
	public abstract class BaseCollection<T> : IEnumerable<T>
	{
		#region Variables.
		private SortedList<string, T> _items = null;	// Container for the collection data.
		private bool _caseSensitive = true;				// Flag to indicate if the collection keys are case sensitive.
		#endregion

		#region Properties.
		/// <summary>
		/// Property to return the internal list of items.
		/// </summary>
		protected SortedList<string, T> Items
		{
			get
			{
				return _items;
			}
		}

		/// <summary>
		/// Property to return whether the collection keys are case sensitive or not.
		/// </summary>
		public bool IsCaseSensitive
		{
			get
			{
				return _caseSensitive;
			}
		}

		/// <summary>
		/// Property to return the number of items in the collection.
		/// </summary>
		public int Count
		{
			get
			{
				
				return _items.Count;
			}
		}
		#endregion

		#region Methods.
		/// <summary>
		/// Function to retrieve an item from the list by its key.
		/// </summary>
		/// <param name="key">Key for the object to retrieve.</param>
		/// <returns>Item in the collection.</returns>
		protected virtual T GetItem(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			else if (!Contains(key))
				throw new KeyNotFoundException("The object '" + key + "' was not found in the collection.");

			if (_caseSensitive)
				return _items[key];
			else
				return _items[key.ToLowerInvariant()];
		}

		/// <summary>
		/// Function to retrieve an item from the list by index.
		/// </summary>
		/// <param name="index">Index of the item to retrieve.</param>
		protected virtual T GetItem(int index)
		{
            if ((index < 0) || (index >= _items.Count))
                throw new IndexOutOfRangeException("The index " + index.ToString() + " is not valid for this collection.");

			return _items[_items.Keys[index]];
		}

		/// <summary>
		/// Function to remove an object from the list by index.
		/// </summary>
		/// <param name="index">Index to remove at.</param>
		protected virtual void RemoveItem(int index)
		{
			if ((index < 0) || (index >= _items.Count))
                throw new IndexOutOfRangeException("The index " + index.ToString() + " is not valid for this collection.");

			_items.RemoveAt(index);
		}

		/// <summary>
		/// Function to remove an object from the list by key.
		/// </summary>
		/// <param name="key">Key of the object to remove.</param>
		protected virtual void RemoveItem(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			else if (!Contains(key))
				throw new KeyNotFoundException(key);

			if (_caseSensitive)
				_items.Remove(key);
			else
				_items.Remove(key.ToLowerInvariant());
		}

		/// <summary>
		/// Function to clear the collection.
		/// </summary>
		protected virtual void ClearItems()
		{
			_items.Clear();
		}

		/// <summary>
		/// Function to update an item in the list by its key.
		/// </summary>
		/// <param name="key">Key of the item to set.</param>
		/// <param name="item">Item to set.</param>
		protected virtual void SetItem(string key, T item)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			else if (!Contains(key))
				throw new KeyNotFoundException(key);

			if (_caseSensitive)
				_items[key] = item;
			else
				_items[key.ToLowerInvariant()] = item;
		}

		/// <summary>
		/// Function to update an item in the list by its index.
		/// </summary>
		/// <param name="index">Index of the item to set.</param>
		/// <param name="item">Item to set.</param>
		protected virtual void SetItem(int index, T item)
		{
			if ((index < 0) || (index >= _items.Count))
                throw new IndexOutOfRangeException("The index " + index.ToString() + " is not valid for this collection.");

			_items[_items.Keys[index]] = item;
		}

		/// <summary>
		/// Function to add an item to the list.
		/// </summary>
		/// <param name="key">Key of the item to add.</param>
		/// <param name="item">Item to add.</param>
		protected virtual void AddItem(string key, T item)
		{
			if (key == null)
				throw new ArgumentNullException("key");
            else if (Contains(key))
                throw new ArgumentException("The key '" + key + "' already exists within this collection.");

			if (_caseSensitive)
				_items.Add(key, item);
			else
				_items.Add(key.ToLowerInvariant(), item);
		}

		/// <summary>
		/// Function to return whether a key exists in the collection or not.
		/// </summary>
		/// <param name="key">Key of the object in the collection.</param>
		/// <returns>TRUE if the object exists, FALSE if not.</returns>
		public virtual bool Contains(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			if (_caseSensitive)
				return _items.ContainsKey(key);
			else
				return _items.ContainsKey(key.ToLowerInvariant());
		}        

		/// <summary>
		/// Function to return the items in the collection as a static array.
		/// </summary>
		/// <param name="start">Starting index of the collection.</param>
		/// <param name="count">Number of items to copy.</param>
		/// <returns>A static array containing a copy of this collection.</returns>
		public T[] StaticArray(int start, int count)
		{
			if ((start >= _items.Count) || (start + count > _items.Count) || (start < 0))
				throw new ArgumentOutOfRangeException("start + count", "The starting index and item count cannot be greater than the length of the collection.");

			if (count < 1)
				throw new ArgumentOutOfRangeException("count", "Need to have at least 1 element to copy.");

			T[] newArray = new T[count];		// Static array.

			// Copy into new array.
			for (int i = start; i < start + count; i++)
				newArray[i] = GetItem(i);
			
			return newArray;
		}

		/// <summary>
		/// Function to return the items in the collection as a static array.
		/// </summary>
		/// <returns>A static array containing a copy of this collection.</returns>
		public T[] StaticArray()
		{
			return StaticArray(0, _items.Count);
		}
		#endregion

		#region Constructor/Destructor.
		/// <summary>
		/// Initializes a new instance of the <see cref="BaseCollection&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="defaultsize">Default size of the collection.</param>
		/// <param name="caseSensitiveKeys">TRUE if the keys are case sensitive, FALSE if not.</param>
		protected BaseCollection(int defaultsize, bool caseSensitiveKeys)
		{
			_items = new SortedList<string,T>(defaultsize);
			_caseSensitive = caseSensitiveKeys;
		}
		#endregion

		#region IEnumerable Members
		/// <summary>
		/// Function to return a new enumerator object.
		/// </summary>
		/// <returns>Enumerator object.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return _items.GetEnumerator();
		}

		/// <summary>
		/// Function to return a new enumerator object.
		/// </summary>
		/// <returns>Enumerator object.</returns>
		public virtual IEnumerator<T> GetEnumerator()
		{
			foreach (KeyValuePair<string, T> item in _items)
				yield return item.Value;
		}
		#endregion
	}

    /// <summary>
    /// Abstract object representing a concrete version of the base collection.
    /// </summary>
    /// <remarks>
    /// This collection is inherited from when a standard collection interface is desired.  Several methods have been implmented, such as Remove.  
    /// <para>
    /// This collection inherits from the BaseCollection object and just implements some of the functionality for you.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">Type of object to store.</typeparam>
	public class Collection<T> 
		: BaseCollection<T>
	{
		#region Properties.
		/// <summary>
		/// Property to get or set the item at the specified index.
		/// </summary>
		public virtual T this[int index]
		{
			get
			{
				return GetItem(index);
			}
		}

		/// <summary>
		/// Property to get or set the item with the specified key.
		/// </summary>
		public virtual T this[string key]
		{
			get
			{
				return GetItem(key);
			}
		}
		#endregion

		#region Methods.
		/// <summary>
		/// Function to remove an object from the list by index.
		/// </summary>
		/// <param name="index">Index to remove at.</param>
		public virtual void Remove(int index)
		{
			RemoveItem(index);
		}

		/// <summary>
		/// Function to remove an object from the list by key.
		/// </summary>
		/// <param name="key">Key of the object to remove.</param>
		public virtual void Remove(string key)
		{
			RemoveItem(key);
		}

		/// <summary>
		/// Function to clear the collection.
		/// </summary>
		public virtual void Clear()
		{
			ClearItems();
		}
		#endregion

		#region Constructor.
		/// <summary>
		/// Initializes a new instance of the <see cref="Collection&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="defaultsize">Default size of the collection.</param>
		/// <param name="caseSensitiveKeys">TRUE if the keys are case sensitive, FALSE if not.</param>
		public Collection(int defaultsize, bool caseSensitiveKeys) 
			: base(defaultsize, caseSensitiveKeys)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Collection&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="caseSensitiveKeys">TRUE if the keys are case sensitive, FALSE if not.</param>
		public Collection(bool caseSensitiveKeys)
			: base(60, caseSensitiveKeys)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Collection&lt;T&gt;"/> class.
		/// </summary>
		public Collection() 
			: this(60, true)
		{
		}
		#endregion
	}

	/// <summary>
	/// Object representing a collection that can be sorted by an arbitrary sorter.
	/// </summary>
	/// <typeparam name="T">Type of data to store in the collection.</typeparam>
	public class SortedCollection<T>
		: Collection<T>
	{
		#region Variables.
		private List<T> _sortedItems;		// List of keys.
		private IComparer<T> _comparer;		// Object used to sort the keys.
		#endregion

		#region Properties.
		/// <summary>
		/// Property to return an item by index.
		/// </summary>
		public override T this[int index]
		{
			get
			{				
				if ((index < 0) || (index >= _sortedItems.Count))
                    throw new IndexOutOfRangeException("The index " + index.ToString() + " is not valid for this collection.");
				return _sortedItems[index];
			}
		}
		#endregion

		#region Methods.
		/// <summary>
		/// Function to add an item to the collection.
		/// </summary>
		/// <param name="key">Key for the item.</param>
		/// <param name="item">Item to add.</param>
		protected override void AddItem(string key, T item)
		{
			base.AddItem(key, item);
			_sortedItems.Add(item);
			Sort();
		}

		/// <summary>
		/// Function to clear the collection.
		/// </summary>
		protected override void ClearItems()
		{
			base.ClearItems();
			_sortedItems.Clear();			
		}

		/// <summary>
		/// Function to remove an object from the list by index.
		/// </summary>
		/// <param name="index">Index to remove at.</param>
		protected override void RemoveItem(int index)
		{
			if ((index < 0) || (index >= _sortedItems.Count))
                throw new IndexOutOfRangeException("The index " + index.ToString() + " is not valid for this collection.");
			_sortedItems.Remove(this[index]);
			base.RemoveItem(index);
			Sort();
		}

		/// <summary>
		/// Function to remove an object from the list by key.
		/// </summary>
		/// <param name="key">Key of the object to remove.</param>
		protected override void RemoveItem(string key)
		{
			if (!Contains(key))
				throw new KeyNotFoundException(key);

			_sortedItems.Remove(this[key]);
			base.RemoveItem(key);
			Sort();
		}

		/// <summary>
		/// Function to sort the array.
		/// </summary>
		public virtual void Sort(IComparer<T> comparison)
		{
			if (_sortedItems.Count > 0)
				_sortedItems.Sort(comparison);
		}

		/// <summary>
		/// Function to sort the array.
		/// </summary>
		public virtual void Sort()
		{
			Sort(_comparer);
		}

		/// <summary>
		/// Function to return a new enumerator object.
		/// </summary>
		/// <returns>Enumerator object.</returns>
		public override IEnumerator<T> GetEnumerator()
		{
			for (int i = 0; i < _sortedItems.Count; i++)
				yield return _sortedItems[i];
		}

		/// <summary>
		/// Function to return a new unsorted enumerator object.
		/// </summary>
		/// <returns>Enumerator object.</returns>
		public IEnumerable<T> GetKeySortedEnumerator()
		{
			foreach (KeyValuePair<string, T> item in Items)
				yield return item.Value;
		}
		#endregion

		#region Constructor.
		/// <summary>
		/// Initializes a new instance of the <see cref="SortedCollection&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="defaultsize">Default size of the collection.</param>
		/// <param name="comparer">Object used to perform the comparsion for sorting.</param>
		/// <param name="caseSensitiveKeys">TRUE if the keys are case sensitive, FALSE if not.</param>
		public SortedCollection(int defaultsize, IComparer<T> comparer, bool caseSensitiveKeys) 
			: base(defaultsize, caseSensitiveKeys)
		{
			_sortedItems = new List<T>(defaultsize);
			_comparer = comparer;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SortedCollection&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="comparer">Object used to perform the comparsion for sorting.</param>
		/// <param name="caseSensitiveKeys">TRUE if the keys are case sensitive, FALSE if not.</param>
		public SortedCollection(IComparer<T> comparer, bool caseSensitiveKeys)
			: this(60, comparer, caseSensitiveKeys)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SortedCollection&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="comparer">Object used to perform the comparsion for sorting.</param>
		public SortedCollection(IComparer<T> comparer) 
			: this(60,comparer, true)
		{
		}
		#endregion
	}
}
