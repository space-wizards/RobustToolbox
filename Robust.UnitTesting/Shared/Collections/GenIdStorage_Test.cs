using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Robust.Shared.Collections;

namespace Robust.UnitTesting.Shared.Collections;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture, TestOf(typeof(GenIdStorage<>))]
public abstract class GenIdStorageTest
{
#region Constructor Tests

    /// <summary>
    /// Tests the properties of the default constructor.
    /// <list type="bullet">
    ///   <item>Default constructor does not immediately allocate storage.</item>
    ///   <item>Newly constructed storage does not contain any values.</item>
    /// </list>
    /// </summary>
    [Test]
    public void TestDefaultConstructorState()
    {
        var storage = new GenIdStorage<int>();

        Assert.Multiple(() =>
        {
            Assert.That(storage.Capacity, Is.EqualTo(0), "storage was created with extant capacity");
            Assert.That(storage, Has.Count.EqualTo(0), "storage did not start with zero value count");
        });

        Assert.Multiple(() => {

        });
    }

    /// <summary>
    /// Tests the properties of the explicit capacity constructor.
    /// <list type="bullet">
    ///   <item>Default constructor preallocates storage for the specified number of values.</item>
    ///   <item>Newly constructed storage does not contain any values.</item>
    /// </list>
    /// </summary>
    [Test]
    public void TestExplicitCapacityConstructorState()
    {
        const int TEST_CAPACITY = 111;

        Assert.That(TEST_CAPACITY, Is.GreaterThan(0), "attempted to test constructor preallocation with explicit capacity <= 0");

        var storage = new GenIdStorage<int>(TEST_CAPACITY);

        Assert.Multiple(() =>
        {
            Assert.That(storage.Capacity, Is.EqualTo(TEST_CAPACITY), "storage was created with capacity other than specified");
            Assert.That(storage.Count, Is.EqualTo(0), "storage did not start with zero value count");
        });
    }

    /// <summary>
    /// Tests the that the explicit capacity constructor throws if asked to allocate space for a negative number of values.
    /// </summary>
    [Test]
    public void TestInvalidConstructorThrows()
    {
        const int TEST_CAPACITY = -1;

        Assert.That(TEST_CAPACITY, Is.LessThan(0), "attempted to test invalid constructor properties with a valid constructor argument");

        Assert.Catch(() => new GenIdStorage<int>(TEST_CAPACITY), "storage was created with invalid capacity");
    }

#endregion Constructor Tests

#region Allocate Tests

    /// <summary>
    /// Tests a single storage allocation
    /// <list type="bullet">
    ///   <item>Allocation does not throw an exception.</item>
    ///   <item>Allocation reserves and returns a valid reference to a value storage.</item>
    ///   <item>Allocation increments the count of the collection.</item>
    ///   <item>Allocation grows the capacity of the collection if it is full.</item>
    /// </list>
    /// </summary>
    [Test]
    public void TestSingleAllocation()
    {
        var storage = new GenIdStorage<int>();

        var key = GenIdStorage<int>.Key.Invalid;
        Assert.DoesNotThrow(() =>
        {
            const int TEST_VALUE = 43;

            ref var value = ref storage.Allocate(out key);

            Assert.That(!Unsafe.IsNullRef(ref value), "allocation returned a null ref");

            value = TEST_VALUE;
        }, "allocation or assignment to allocated storage threw an exception");

        Assert.Multiple(() =>
        {
            Assert.That(key, Is.Not.EqualTo(GenIdStorage<int>.Key.Invalid), "allocation returned an invalid key");
            Assert.That(storage, Has.Count.EqualTo(1), "allocation did not increment count");
            Assert.That(storage.Capacity, Is.GreaterThanOrEqualTo(storage.Count), "allocation did not increase storage capacity");
        });
    }

    /// <summary>
    /// Tests multiple sequential storage allocations.
    /// <list type="bullet">
    ///   <item>Allocation does not throw an exception.</item>
    ///   <item>Allocation reserves and returns a valid reference to a value storage.</item>
    ///   <item>Allocation increments the count of the collection.</item>
    ///   <item>Allocation grows the capacity of the collection if it is full.</item>
    /// </list>
    /// </summary>
    [Test]
    public void TestMultipleAllocations()
    {
        // The initial capacity of the storage container.
        const int TEST_CAPACITY = 10;
        // The number of allocations to perform (must be > TEST_CAPACITY)
        const int TEST_COUNT = 30;

        Assert.That(TEST_COUNT, Is.GreaterThan(TEST_CAPACITY), "");

        var storage = new GenIdStorage<int>(TEST_CAPACITY);

        var keys = new HashSet<GenIdStorage<int>.Key>();
        for (var i = 1; i <= TEST_COUNT; ++i)
        {
            ref var value = ref storage.Allocate(out var key);

            Assert.That(!Unsafe.IsNullRef(ref value), "allocation provided null ref");

            Assert.Multiple(() =>
            {
                Assert.That(key, Is.Not.EqualTo(GenIdStorage<int>.Key.Invalid), "allocation returned an invalid key");
                Assert.That(keys.Add(key), Is.True, "allocation returned a duplicate key");
                Assert.That(storage, Has.Count.EqualTo(i), "allocation did not increment count");
                Assert.That(storage.Capacity, Is.GreaterThanOrEqualTo(storage.Count), "allocation did not increase storage capacity");
            });
        }
    }

#endregion Allocate Tests

#region Free Tests

    /// <summary>
    /// Tests that attempting to free from an empty storage throws an exception.
    /// </summary>
    [Test]
    public void TestEmptyFree()
    {
        var storage = new GenIdStorage<int>();

        Assert.Catch(() => storage.Free(GenIdStorage<int>.Key.Invalid), "attempting to free from an empty storage did not throw");
    }

    /// <summary>
    /// Tests that attempting to free an allocated value does not throw.
    /// </summary>
    [Test]
    public void TestSingleFree()
    {
        var storage = new GenIdStorage<int>();

        ref var value = ref storage.Allocate(out var key);

        Assert.DoesNotThrow(() => storage.Free(key), "valid free threw");

        Assert.Multiple(() =>
        {
            Assert.That(storage, Has.Count.EqualTo(0), "free did not decrement count");
            Assert.That(storage.Capacity, Is.GreaterThan(0), "free shrank storage");
        });
    }

    /// <summary>
    /// Tests that attempting to free an allocated value twice throws an exception.
    /// </summary>
    [Test]
    public void TestDoubleFree()
    {
        var storage = new GenIdStorage<int>();

        ref var value = ref storage.Allocate(out var key);

        Assert.DoesNotThrow(() => storage.Free(key), "valid free threw");
        Assert.Catch(() => storage.Free(key), "double free of same key did not throw");
    }

    /// <summary>
    /// Tests that attempting to free with an invalid key throws an exception.
    /// </summary>
    [Test]
    public void TestInvalidFree()
    {
        var storage = new GenIdStorage<int>();

        ref var value = ref storage.Allocate(out var key);

        Assert.Catch(() => storage.Free(GenIdStorage<int>.Key.Invalid), "valid free threw");
    }

#endregion Free Tests

#region ContainsKey Tests

    /// <summary>
    /// Tests for the <see cref="GenIdStorage{T}.ContainsKey(in GenIdStorage{T}.Key)"/> method.
    /// <list type="bullet">
    ///   <item>Storage does not contain invalid keys.</item>
    ///   <item>Storage contains keys for allocated values.</item>
    ///   <item>Storage does not contain keys for freed values.</item>
    ///   <item>ContainsKey differentiates between multiple keys to the same slot index.</item>
    /// </list>
    /// </summary>
    [Test]
    public void TestContainsKey()
    {
        var storage = new GenIdStorage<int>();

        Assert.That(storage.ContainsKey(GenIdStorage<int>.Key.Invalid), Is.False, "storage contains invalid key");

        storage.Allocate(out var key1);

        Assert.That(storage.ContainsKey(key1), Is.True, "key to allocated slot was not valid");
        Assert.That(storage.ContainsKey(key1), Is.True, "ContainsKey is not idempotent");

        storage.Free(key1);

        Assert.That(storage.ContainsKey(key1), Is.False, "key to freed value was still valid");

        storage.Allocate(out var key2);

        Assert.That(key1.Index, Is.EqualTo(key2.Index), "allocate after free did not use free slot");

        Assert.Multiple(() =>
        {
            Assert.That(storage.ContainsKey(key1), Is.False, "key to old version of reallocated slot was still valid");
            Assert.That(storage.ContainsKey(key2), Is.True, "key to reallocated slot was invalid");
        });
    }

#endregion ContainsKey Tests

#region Indexer Tests

    /// <summary>
    /// Tests for the <see cref="GenIdStorage{T}.this[in GenIdStorage{T}.Key]"/> API
    /// <list type="bullet">
    ///   <item>Attempting to index using invalid keys throws an exception.</item>
    ///   <item>Indexing using valid keys returns the stored value.</item>
    ///   <item>Indexing may be used to assign values to slots.</item>
    ///   <item>Indexing using freed keys throws an exception.</item>
    /// </list>
    /// </summary>
    [Test]
    public void TestIndexer()
    {
        // Unique values generated by keyboard smash
        const int TEST_VALUE_A = -13452;
        const int TEST_VALUE_B = 5345123;

        var storage = new GenIdStorage<int>();

        Assert.Catch(() => { var _ = storage[GenIdStorage<int>.Key.Invalid]; }, "indexer for invalid key did not throw");

        storage.Allocate(out var key) = TEST_VALUE_A;

        Assert.That(storage[key], Is.EqualTo(TEST_VALUE_A), "indexer did not retrieve assigned value");
        Assert.That(storage[key], Is.EqualTo(TEST_VALUE_A), "indexer is not idempotent");

        Assert.DoesNotThrow(() => storage[key] = TEST_VALUE_B, "indexer threw when attempting to assign a value");
        Assert.That(storage[key], Is.EqualTo(TEST_VALUE_B), "indexer assignment did not assign proper value");

        storage.Free(key);

        Assert.Catch(() => { var _ = storage[key]; }, "indexer for freed slot did not throw");
    }

#endregion Indexer Tests

#region Clear Tests

    /// <summary>
    /// Tests for <see cref="GenIdStorage{T}.Clear()"/>
    /// <list type="bullet">
    ///   <item>Clearing the storage frees all allocated slots.</item>
    ///   <item>Clearing the storage does not change the capacity.</item>
    ///   <item>Clearing the storage invalidates all extant keys.</item>
    /// </list>
    /// </summary>
    [Test]
    public void TestClear()
    {
        const int TEST_COUNT = 23;

        var storage = new GenIdStorage<int>();

        var keys = new List<GenIdStorage<int>.Key>();
        for (var i = 0; i < TEST_COUNT; ++i)
        {
            storage.Allocate(out var key);
            keys.Add(key);
        }

        Assert.That(storage, Has.Count.GreaterThan(0));

        var capacity = storage.Capacity;
        storage.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(storage, Has.Count.EqualTo(0), "clear did not empty storage");
            Assert.That(storage.Capacity, Is.EqualTo(capacity), "clear changed storage capacity");
        });

        Assert.That(keys.Any((key) => storage.ContainsKey(key)), "clear did not invalidate all allocated slots");
    }

#endregion Clear Tests

#region Resize Tests

    /// <summary>
    /// Tests for <see cref="GenIdStorage{T}.Resize(int)"/> where the requested capacity is equal to the current capacity.
    /// <list type="bullet">
    ///   <item>NOP resize does not throw exceptions.</item>
    ///   <item>NOP resize does not change storage capacity.</item>
    ///   <item>NOP resize does not invalidate contents.</item>
    /// </list>
    /// </summary>
    [Test]
    public void TestResizeNop()
    {
        const int TEST_CAPACITY = 22;
        const int TEST_VALUE = -3545;

        var storage = new GenIdStorage<int>(TEST_CAPACITY);

        storage.Allocate(out var key) = TEST_VALUE;

        Assert.DoesNotThrow(() => storage.Resize(TEST_CAPACITY), "NOP resize threw");

        Assert.Multiple(() =>
        {
            Assert.That(storage.Capacity, Is.EqualTo(TEST_CAPACITY), "NOP resize changed storage capacity");
            Assert.That(storage[key], Is.EqualTo(TEST_VALUE), "NOP reisize invalidated storage contents");
        });
    }

    /// <summary>
    /// Tests for <see cref="GenIdStorage{T}.Resize(int)"/> where the requested capacity is greater than the current capacity.
    /// <list type="bullet">
    ///   <item>Resize grow does not throw exceptions.</item>
    ///   <item>Resize grow allocates the requested capacity.</item>
    ///   <item>Resize grow dows not invalidate the contents.</item>
    /// </list>
    /// </summary>
    [Test]
    public void TestGrow()
    {
        const int TEST_CAPACITY = 22;
        const int TEST_VALUE = -3545;

        var storage = new GenIdStorage<int>();

        storage.Allocate(out var key) = TEST_VALUE;

        var capacity = storage.Capacity + TEST_CAPACITY;
        Assert.DoesNotThrow(() => storage.Resize(capacity), "resize grow threw");

        Assert.Multiple(() =>
        {
            Assert.That(storage.Capacity, Is.EqualTo(capacity), "resize grow did not allocate requested capacity");
            Assert.That(storage[key], Is.EqualTo(TEST_VALUE), "resize grow invalidated storage contents");
        });
    }

    /// <summary>
    /// Tests for <see cref="GenIdStorage{T}.Resize(int)"/> where the requested capacity is less than the current capacity.
    /// <list type="bullet">
    ///   <item>Resize shrink throws an exception if any storage slots are currently allocated.</item>
    ///   <item>Interrupted resize shrinks do not change the capacity.</item>
    ///   <item>Interrupted resize shrinks do not invalidate stored values.</item>
    ///   <item>Resize shrink succeeds if no storage slots are currently allocated.</item>
    ///   <item>Resize shrink reduces the allocated storage slots down to the specified capacity.</item>
    /// </list>
    /// </summary>
    [Test]
    public void TestShrink()
    {
        const int TEST_CAPACITY_START = 22;
        const int TEST_CAPACITY_END = 3;
        const int TEST_VALUE = -1323145;

        Assert.That(TEST_CAPACITY_END, Is.LessThan(TEST_CAPACITY_START), "");

        var storage = new GenIdStorage<int>(TEST_CAPACITY_START);

        storage.Allocate(out var key) = TEST_VALUE;

        Assert.Catch(() => storage.Resize(TEST_CAPACITY_END), "resize shrink succeeded with allocated slots");

        Assert.Multiple(() =>
        {
            Assert.That(storage.Capacity, Is.EqualTo(TEST_CAPACITY_START), "invalid resize shrink changed capacity");
            Assert.That(storage[key], Is.EqualTo(TEST_VALUE), "invalid resize shrink invalidated stored value");
        });

        storage.Free(key);

        Assert.DoesNotThrow(() => storage.Resize(TEST_CAPACITY_END), "resize shrink succeeded with allocated slots");
        Assert.That(storage.Capacity, Is.EqualTo(TEST_CAPACITY_END), "invalid resize shrink changed capacity");
    }

#endregion Resize Tests

#region Insert Tests

    [Test]
    public void TestInsert()
    {
        const int TEST_VALUE = 95784;

        var storage = new GenIdStorage<int>();

        var key = storage.Insert(TEST_VALUE);

        Assert.Multiple(() =>
        {
            Assert.That(storage, Has.Count.EqualTo(1), "inserted value did not increment count");
            Assert.That(storage[key], Is.EqualTo(TEST_VALUE), "inserted value was not present in storage");
        });
    }

    [Test]
    public void TestTryRemove()
    {
        const int TEST_VALUE_A = 95784;
        const int TEST_VALUE_B = -3450423;

        var storage = new GenIdStorage<int>();

        Assert.That(storage.TryRemove(GenIdStorage<int>.Key.Invalid), Is.False, "attempt to remove invalid key succeeded");
        Assert.That(storage, Has.Count.EqualTo(0), "attempt to remove invalid key decremented count");

        var key1 = storage.Insert(TEST_VALUE_A);
        var key2 = storage.Insert(TEST_VALUE_B);

        Assert.That(storage.TryRemove(key1, out var value1), "attempt to remove valid key failed");

        Assert.Multiple(() =>
        {
            Assert.That(value1, Is.EqualTo(TEST_VALUE_A), "removed value was not inserted value");
            Assert.That(storage, Has.Count.EqualTo(1), "successful remove did not decrement count");
        });

        Assert.That(storage.TryRemove(key1, out value1), Is.False, "attempt to double remove key succeeded");

        Assert.Multiple(() =>
        {
            Assert.That(value1, Is.EqualTo(default(int)!), "returned value was not default");
            Assert.That(storage, Has.Count.EqualTo(1), "attempt to double remove key decremented count");
        });
    }

    [Test]
    public void TestTryGet()
    {
        const int TEST_VALUE_A = 95784;
        const int TEST_VALUE_B = -3450423;

        var storage = new GenIdStorage<int>();

        ref var nullRef = ref storage.TryGetRef(GenIdStorage<int>.Key.Invalid, out var success);

        Assert.That(success, Is.False, "attempt to get ref with invalid key succeeded");
        Assert.That(Unsafe.IsNullRef(ref nullRef), Is.True, "attempt to get ref with invalid key returned non-null ref");

        var key = storage.Insert(TEST_VALUE_A);

        ref var valRef = ref storage.TryGetRef(key, out success);

        Assert.That(success, Is.True, "attempt to get ref with valid key failed");
        Assert.That(Unsafe.IsNullRef(ref valRef), Is.False, "attempt to get ref with valid key returned null ref");
        Assert.That(valRef, Is.EqualTo(TEST_VALUE_A), "attempt to get ref with valid key returned wrong ref");

        valRef = TEST_VALUE_B;

        Assert.That(storage.TryGet(key, out var value), "attempt to get value with valid key failed");
        Assert.That(value, Is.EqualTo(TEST_VALUE_B), "attempt to get value with valid key returned wrong value");

        storage.Free(key);

        valRef = ref storage.TryGetRef(key, out success);

        Assert.That(success, Is.False, "attempt to get ref with freed key failed");
        Assert.That(Unsafe.IsNullRef(ref valRef), Is.True, "attempt to get ref with freed key returned non-null ref");

        Assert.That(storage.TryGet(key, out value), "attempt to get value with freed key failed");
        Assert.That(value, Is.EqualTo(default(int)!), "attempt to get value with freed key returned non-default value");
    }

#endregion Insert Tests

#region Enumerator Tests

    /// <summary>
    /// Tests for enumerating over all stored key-value pairs.
    /// </summary>
    [Test]
    public void TestKeyValuePairEnumerator()
    {
        const int TEST_COUNT = 54;

        var storage = new GenIdStorage<int>();

        var keys = new Dictionary<GenIdStorage<int>.Key, int>(TEST_COUNT);
        for (var i = 1; i <= TEST_COUNT; ++i)
        {
            keys.Add(storage.Insert(i), i);
        }

        Assert.Multiple(() =>
        {
            foreach (var (key, storedValue) in storage)
            {
                Assert.That(keys.Remove(key, out var insertedValue), Is.True, "enumerator returned unallocated key");
                Assert.That(storedValue, Is.EqualTo(insertedValue), "enumerator returned unassociated key-value pair");
            }
        });

        Assert.That(keys, Has.Count.EqualTo(0), "enumerator did not enumerate all stored key-value pairs");
    }

    /// <summary>
    /// Tests for enumerating over all valid keys pairs.
    /// </summary>
    [Test]
    public void TestKeyEnumerator()
    {
        const int TEST_COUNT = 54;

        var storage = new GenIdStorage<int>();

        var keys = new HashSet<GenIdStorage<int>.Key>(TEST_COUNT);
        for (var i = 1; i <= TEST_COUNT; ++i)
        {
            keys.Add(storage.Insert(i));
        }

        Assert.That(storage.Keys, Has.Count.EqualTo(TEST_COUNT), "");

        Assert.Multiple(() =>
        {
            foreach (var key in storage.Keys)
            {
                Assert.That(keys.Remove(key), Is.True, "key enumerator returned unallocated key");
            }
        });

        Assert.That(keys, Has.Count.EqualTo(0), "key enumerator did not enumerate all stored keys pairs");
    }

    /// <summary>
    /// Tests for enumerating over all stored values pairs.
    /// </summary>
    [Test]
    public void TestValueEnumerator()
    {
        const int TEST_COUNT = 54;

        var storage = new GenIdStorage<int>();

        var values = new HashSet<int>(TEST_COUNT);
        for (var i = 1; i <= TEST_COUNT; ++i)
        {
            storage.Insert(i);
            values.Add(i);
        }

        Assert.That(storage.Values, Has.Count.EqualTo(TEST_COUNT), "");

        Assert.Multiple(() =>
        {
            foreach (var value in storage.Values)
            {
                Assert.That(values.Remove(value), Is.True, "values enumerator returned value not inserted into storage");
            }
        });

        Assert.That(values, Has.Count.EqualTo(0), "values enumerator did not enumerate all stored values");
    }

#endregion Enumerator Tests
}
