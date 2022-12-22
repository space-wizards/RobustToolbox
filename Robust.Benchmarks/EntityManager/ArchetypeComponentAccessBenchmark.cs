using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;

namespace Robust.Benchmarks.EntityManager;

[MemoryDiagnoser]
[Virtual]
public class ArchetypeComponentAccessBenchmark
{
    private const int N = 10000;

    private Dictionary<Type, Dictionary<int, object>> _componentDictionary = default!;
    private Archetype<Type1, Type2, Type3, Type4, Type5, Type6, Type7, Type8, Type9, Type10> _archetype = default!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _componentDictionary = new Dictionary<Type, Dictionary<int, object>>(N)
        {
            [typeof(Type1)] = new(),
            [typeof(Type2)] = new(),
            [typeof(Type3)] = new(),
            [typeof(Type4)] = new(),
            [typeof(Type5)] = new(),
            [typeof(Type6)] = new(),
            [typeof(Type7)] = new(),
            [typeof(Type8)] = new(),
            [typeof(Type9)] = new(),
            [typeof(Type10)] = new(),
        };
        _archetype = new Archetype<Type1, Type2, Type3, Type4, Type5, Type6, Type7, Type8, Type9, Type10>(N);

        for (var i = 0; i < N; i++)
        {
            _componentDictionary[typeof(Type1)][i] = new Type1();
            _componentDictionary[typeof(Type2)][i] = new Type2();
            _componentDictionary[typeof(Type3)][i] = new Type3();
            _componentDictionary[typeof(Type4)][i] = new Type4();
            _componentDictionary[typeof(Type5)][i] = new Type5();
            _componentDictionary[typeof(Type6)][i] = new Type6();
            _componentDictionary[typeof(Type7)][i] = new Type7();
            _componentDictionary[typeof(Type8)][i] = new Type8();
            _componentDictionary[typeof(Type9)][i] = new Type9();
            _componentDictionary[typeof(Type10)][i] = new Type10();

            _archetype.AddEntity(i);
            _archetype.AddComponent(i, new Type1());
            _archetype.AddComponent(i, new Type2());
            _archetype.AddComponent(i, new Type3());
            _archetype.AddComponent(i, new Type4());
            _archetype.AddComponent(i, new Type5());
            _archetype.AddComponent(i, new Type6());
            _archetype.AddComponent(i, new Type7());
            _archetype.AddComponent(i, new Type8());
            _archetype.AddComponent(i, new Type9());
            _archetype.AddComponent(i, new Type10());
        }
    }

    [Benchmark]
    public Type1 GetSingleComponentsDictionary()
    {
        return (Type1) _componentDictionary[typeof(Type1)][1584];
    }

    [Benchmark]
    public Type1 GetSingleComponentsArchetypeCast()
    {
        return _archetype.GetComponentCast<Type1>(1584);
    }

    [Benchmark]
    public Type1 GetSingleComponentsArchetypeCastHandle()
    {
        // Handle is the same as the id
        return _archetype.GetComponentCastHandle<Type1>(1584);
    }

    [Benchmark]
    public Type1 GetSingleComponentsArchetypeUnsafe()
    {
        return _archetype.GetComponentUnsafe<Type1>(1584);
    }

    [Benchmark]
    public Type1 GetSingleComponentsArchetypeUnsafeHandle()
    {
        // Handle is the same as the id
        return _archetype.GetComponentUnsafeHandle<Type1>(1584);
    }

    [Benchmark]
    public (Type1, Type2, Type3, Type4, Type5, Type6, Type7, Type8, Type9, Type10) GetTenComponentsDictionary()
    {
        return (
            (Type1) _componentDictionary[typeof(Type1)][1584],
            (Type2) _componentDictionary[typeof(Type2)][1584],
            (Type3) _componentDictionary[typeof(Type3)][1584],
            (Type4) _componentDictionary[typeof(Type4)][1584],
            (Type5) _componentDictionary[typeof(Type5)][1584],
            (Type6) _componentDictionary[typeof(Type6)][1584],
            (Type7) _componentDictionary[typeof(Type7)][1584],
            (Type8) _componentDictionary[typeof(Type8)][1584],
            (Type9) _componentDictionary[typeof(Type9)][1584],
            (Type10) _componentDictionary[typeof(Type10)][1584]
        );
    }

    [Benchmark]
    public (Type1, Type2, Type3, Type4, Type5, Type6, Type7, Type8, Type9, Type10) GetTenComponentsArchetypeCast()
    {
        return (
            _archetype.GetComponentCast<Type1>(1584),
            _archetype.GetComponentCast<Type2>(1584),
            _archetype.GetComponentCast<Type3>(1584),
            _archetype.GetComponentCast<Type4>(1584),
            _archetype.GetComponentCast<Type5>(1584),
            _archetype.GetComponentCast<Type6>(1584),
            _archetype.GetComponentCast<Type7>(1584),
            _archetype.GetComponentCast<Type8>(1584),
            _archetype.GetComponentCast<Type9>(1584),
            _archetype.GetComponentCast<Type10>(1584)
        );
    }

    [Benchmark]
    public (Type1, Type2, Type3, Type4, Type5, Type6, Type7, Type8, Type9, Type10) GetTenComponentsArchetypeCastHandle()
    {
        // Handle is the same as the id
        return (
            _archetype.GetComponentCastHandle<Type1>(1584),
            _archetype.GetComponentCastHandle<Type2>(1584),
            _archetype.GetComponentCastHandle<Type3>(1584),
            _archetype.GetComponentCastHandle<Type4>(1584),
            _archetype.GetComponentCastHandle<Type5>(1584),
            _archetype.GetComponentCastHandle<Type6>(1584),
            _archetype.GetComponentCastHandle<Type7>(1584),
            _archetype.GetComponentCastHandle<Type8>(1584),
            _archetype.GetComponentCastHandle<Type9>(1584),
            _archetype.GetComponentCastHandle<Type10>(1584)
        );
    }

    [Benchmark]
    public (Type1, Type2, Type3, Type4, Type5, Type6, Type7, Type8, Type9, Type10) GetTenComponentsArchetypeUnsafe()
    {
        return (
            _archetype.GetComponentUnsafe<Type1>(1584),
            _archetype.GetComponentUnsafe<Type2>(1584),
            _archetype.GetComponentUnsafe<Type3>(1584),
            _archetype.GetComponentUnsafe<Type4>(1584),
            _archetype.GetComponentUnsafe<Type5>(1584),
            _archetype.GetComponentUnsafe<Type6>(1584),
            _archetype.GetComponentUnsafe<Type7>(1584),
            _archetype.GetComponentUnsafe<Type8>(1584),
            _archetype.GetComponentUnsafe<Type9>(1584),
            _archetype.GetComponentUnsafe<Type10>(1584)
        );
    }

    [Benchmark]
    public (Type1, Type2, Type3, Type4, Type5, Type6, Type7, Type8, Type9, Type10) GetTenComponentsArchetypeUnsafeHandle()
    {
        // Handle is the same as the id
        return (
            _archetype.GetComponentUnsafeHandle<Type1>(1584),
            _archetype.GetComponentUnsafeHandle<Type2>(1584),
            _archetype.GetComponentUnsafeHandle<Type3>(1584),
            _archetype.GetComponentUnsafeHandle<Type4>(1584),
            _archetype.GetComponentUnsafeHandle<Type5>(1584),
            _archetype.GetComponentUnsafeHandle<Type6>(1584),
            _archetype.GetComponentUnsafeHandle<Type7>(1584),
            _archetype.GetComponentUnsafeHandle<Type8>(1584),
            _archetype.GetComponentUnsafeHandle<Type9>(1584),
            _archetype.GetComponentUnsafeHandle<Type10>(1584)
        );
    }

    // Just a bunch of types to pad the size of the arrays and such.

    // @formatter:off
    // ReSharper disable UnusedType.Local
    public struct Type1{}
    public struct Type2{}
    public struct Type3{}
    public struct Type4{}
    public struct Type5{}
    public struct Type6{}
    public struct Type7{}
    public struct Type8{}
    public struct Type9{}
    public struct Type10{}
    // ReSharper restore UnusedType.Local
    // @formatter:on

    private class Archetype<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
    {
        private int _nextId;
        private readonly Dictionary<int, int> _ids;
        private readonly T1[] _t1Comps;
        private readonly T2[] _t2Comps;
        private readonly T3[] _t3Comps;
        private readonly T4[] _t4Comps;
        private readonly T5[] _t5Comps;
        private readonly T6[] _t6Comps;
        private readonly T7[] _t7Comps;
        private readonly T8[] _t8Comps;
        private readonly T9[] _t9Comps;
        private readonly T10[] _t10Comps;

        public Archetype(int n)
        {
            _ids = new Dictionary<int, int>(n);
            _t1Comps = new T1[n];
            _t2Comps = new T2[n];
            _t3Comps = new T3[n];
            _t4Comps = new T4[n];
            _t5Comps = new T5[n];
            _t6Comps = new T6[n];
            _t7Comps = new T7[n];
            _t8Comps = new T8[n];
            _t9Comps = new T9[n];
            _t10Comps = new T10[n];
        }

        public void AddEntity(int entity)
        {
            var id = _nextId++;
            _ids[entity] = id;
        }

        public void AddComponent<T>(int entity, T val)
        {
            var id = _ids[entity];

            switch (val)
            {
                case T1 val1:
                    _t1Comps[id] = val1;
                    break;
                case T2 val2:
                    _t2Comps[id] = val2;
                    break;
                case T3 val3:
                    _t3Comps[id] = val3;
                    break;
                case T4 val4:
                    _t4Comps[id] = val4;
                    break;
                case T5 val5:
                    _t5Comps[id] = val5;
                    break;
                case T6 val6:
                    _t6Comps[id] = val6;
                    break;
                case T7 val7:
                    _t7Comps[id] = val7;
                    break;
                case T8 val8:
                    _t8Comps[id] = val8;
                    break;
                case T9 val9:
                    _t9Comps[id] = val9;
                    break;
                case T10 val10:
                    _t10Comps[id] = val10;
                    break;
            }
        }

        public T GetComponentCast<T>(int entity, T val = default) where T : struct
        {
            var id = _ids[entity];

            return val switch
            {
                T1 => (T) (object) _t1Comps[id]!,
                T2 => (T) (object) _t2Comps[id]!,
                T3 => (T) (object) _t3Comps[id]!,
                T4 => (T) (object) _t4Comps[id]!,
                T5 => (T) (object) _t5Comps[id]!,
                T6 => (T) (object) _t6Comps[id]!,
                T7 => (T) (object) _t7Comps[id]!,
                T8 => (T) (object) _t8Comps[id]!,
                T9 => (T) (object) _t9Comps[id]!,
                T10 => (T) (object) _t10Comps[id]!,
                _ => throw new ArgumentException($"Unknown type: {typeof(T)}")
            };
        }

        public T GetComponentCastHandle<T>(int handle, T val = default) where T : struct
        {
            return val switch
            {
                T1 => (T) (object) _t1Comps[handle]!,
                T2 => (T) (object) _t2Comps[handle]!,
                T3 => (T) (object) _t3Comps[handle]!,
                T4 => (T) (object) _t4Comps[handle]!,
                T5 => (T) (object) _t5Comps[handle]!,
                T6 => (T) (object) _t6Comps[handle]!,
                T7 => (T) (object) _t7Comps[handle]!,
                T8 => (T) (object) _t8Comps[handle]!,
                T9 => (T) (object) _t9Comps[handle]!,
                T10 => (T) (object) _t10Comps[handle]!,
                _ => throw new ArgumentException($"Unknown type: {typeof(T)}")
            };
        }

        public ref T GetComponentUnsafe<T>(int entity, T val = default) where T : struct
        {
            var id = _ids[entity];

            switch (val)
            {
                case T1:
                    return ref Unsafe.As<T1, T>(ref _t1Comps[id]);
                case T2:
                    return ref Unsafe.As<T2, T>(ref _t2Comps[id]);
                case T3:
                    return ref Unsafe.As<T3, T>(ref _t3Comps[id]);
                case T4:
                    return ref Unsafe.As<T4, T>(ref _t4Comps[id]);
                case T5:
                    return ref Unsafe.As<T5, T>(ref _t5Comps[id]);
                case T6:
                    return ref Unsafe.As<T6, T>(ref _t6Comps[id]);
                case T7:
                    return ref Unsafe.As<T7, T>(ref _t7Comps[id]);
                case T8:
                    return ref Unsafe.As<T8, T>(ref _t8Comps[id]);
                case T9:
                    return ref Unsafe.As<T9, T>(ref _t9Comps[id]);
                case T10:
                    return ref Unsafe.As<T10, T>(ref _t10Comps[id]);
                default:
                    throw new ArgumentException($"Unknown type: {typeof(T)}");
            }
        }

        public ref T GetComponentUnsafeHandle<T>(int handle, T val = default) where T : struct
        {
            switch (val)
            {
                case T1:
                    return ref Unsafe.As<T1, T>(ref _t1Comps[handle]);
                case T2:
                    return ref Unsafe.As<T2, T>(ref _t2Comps[handle]);
                case T3:
                    return ref Unsafe.As<T3, T>(ref _t3Comps[handle]);
                case T4:
                    return ref Unsafe.As<T4, T>(ref _t4Comps[handle]);
                case T5:
                    return ref Unsafe.As<T5, T>(ref _t5Comps[handle]);
                case T6:
                    return ref Unsafe.As<T6, T>(ref _t6Comps[handle]);
                case T7:
                    return ref Unsafe.As<T7, T>(ref _t7Comps[handle]);
                case T8:
                    return ref Unsafe.As<T8, T>(ref _t8Comps[handle]);
                case T9:
                    return ref Unsafe.As<T9, T>(ref _t9Comps[handle]);
                case T10:
                    return ref Unsafe.As<T10, T>(ref _t10Comps[handle]);
                default:
                    throw new ArgumentException($"Unknown type: {typeof(T)}");
            }
        }
    }
}
