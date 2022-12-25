using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;

namespace Robust.Benchmarks.EntityManager;

[MemoryDiagnoser]
[Virtual]
public class ArchetypeComponentAccessBenchmark
{
    private const int N = 10000;
    private const int Entity = 1584;

    private Dictionary<Type, Dictionary<int, object>> _componentDictionary = default!;
    private Archetype<Type1, Type2, Type3, Type4, Type5, Type6, Type7, Type8, Type9, Type10> _archetype = default!;
    private static readonly Consumer Consumer = new();

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
    public Type1 GetSingleComponentDictionary()
    {
        return (Type1) _componentDictionary[typeof(Type1)][Entity];
    }

    [Benchmark]
    public Type1 GetSingleComponentArchetypeCast()
    {
        return _archetype.GetComponentCast<Type1>(Entity);
    }

    [Benchmark]
    public Type1 GetSingleComponentArchetypeCastHandle()
    {
        // Handle is the same as the id
        return _archetype.GetComponentCastHandle<Type1>(Entity);
    }

    [Benchmark]
    public Type1 GetSingleComponentArchetypeUnsafe()
    {
        return _archetype.GetComponentUnsafe<Type1>(Entity);
    }

    [Benchmark]
    public Type1 GetSingleComponentArchetypeUnsafeHandle()
    {
        // Handle is the same as the id
        return _archetype.GetComponentUnsafeHandle<Type1>(Entity);
    }

    [Benchmark]
    public (Type1, Type2, Type3, Type4, Type5, Type6, Type7, Type8, Type9, Type10) GetTenComponentsDictionary()
    {
        return (
            (Type1) _componentDictionary[typeof(Type1)][Entity],
            (Type2) _componentDictionary[typeof(Type2)][Entity],
            (Type3) _componentDictionary[typeof(Type3)][Entity],
            (Type4) _componentDictionary[typeof(Type4)][Entity],
            (Type5) _componentDictionary[typeof(Type5)][Entity],
            (Type6) _componentDictionary[typeof(Type6)][Entity],
            (Type7) _componentDictionary[typeof(Type7)][Entity],
            (Type8) _componentDictionary[typeof(Type8)][Entity],
            (Type9) _componentDictionary[typeof(Type9)][Entity],
            (Type10) _componentDictionary[typeof(Type10)][Entity]
        );
    }

    [Benchmark]
    public (Type1, Type2, Type3, Type4, Type5, Type6, Type7, Type8, Type9, Type10) GetTenComponentsArchetypeCast()
    {
        return (
            _archetype.GetComponentCast<Type1>(Entity),
            _archetype.GetComponentCast<Type2>(Entity),
            _archetype.GetComponentCast<Type3>(Entity),
            _archetype.GetComponentCast<Type4>(Entity),
            _archetype.GetComponentCast<Type5>(Entity),
            _archetype.GetComponentCast<Type6>(Entity),
            _archetype.GetComponentCast<Type7>(Entity),
            _archetype.GetComponentCast<Type8>(Entity),
            _archetype.GetComponentCast<Type9>(Entity),
            _archetype.GetComponentCast<Type10>(Entity)
        );
    }

    [Benchmark]
    public (Type1, Type2, Type3, Type4, Type5, Type6, Type7, Type8, Type9, Type10) GetTenComponentsArchetypeCastHandle()
    {
        // Handle is the same as the id
        return (
            _archetype.GetComponentCastHandle<Type1>(Entity),
            _archetype.GetComponentCastHandle<Type2>(Entity),
            _archetype.GetComponentCastHandle<Type3>(Entity),
            _archetype.GetComponentCastHandle<Type4>(Entity),
            _archetype.GetComponentCastHandle<Type5>(Entity),
            _archetype.GetComponentCastHandle<Type6>(Entity),
            _archetype.GetComponentCastHandle<Type7>(Entity),
            _archetype.GetComponentCastHandle<Type8>(Entity),
            _archetype.GetComponentCastHandle<Type9>(Entity),
            _archetype.GetComponentCastHandle<Type10>(Entity)
        );
    }

    [Benchmark]
    public (Type1, Type2, Type3, Type4, Type5, Type6, Type7, Type8, Type9, Type10) GetTenComponentsArchetypeUnsafe()
    {
        return (
            _archetype.GetComponentUnsafe<Type1>(Entity),
            _archetype.GetComponentUnsafe<Type2>(Entity),
            _archetype.GetComponentUnsafe<Type3>(Entity),
            _archetype.GetComponentUnsafe<Type4>(Entity),
            _archetype.GetComponentUnsafe<Type5>(Entity),
            _archetype.GetComponentUnsafe<Type6>(Entity),
            _archetype.GetComponentUnsafe<Type7>(Entity),
            _archetype.GetComponentUnsafe<Type8>(Entity),
            _archetype.GetComponentUnsafe<Type9>(Entity),
            _archetype.GetComponentUnsafe<Type10>(Entity)
        );
    }

    [Benchmark]
    public (Type1, Type2, Type3, Type4, Type5, Type6, Type7, Type8, Type9, Type10) GetTenComponentsArchetypeUnsafeHandle()
    {
        // Handle is the same as the id
        return (
            _archetype.GetComponentUnsafeHandle<Type1>(Entity),
            _archetype.GetComponentUnsafeHandle<Type2>(Entity),
            _archetype.GetComponentUnsafeHandle<Type3>(Entity),
            _archetype.GetComponentUnsafeHandle<Type4>(Entity),
            _archetype.GetComponentUnsafeHandle<Type5>(Entity),
            _archetype.GetComponentUnsafeHandle<Type6>(Entity),
            _archetype.GetComponentUnsafeHandle<Type7>(Entity),
            _archetype.GetComponentUnsafeHandle<Type8>(Entity),
            _archetype.GetComponentUnsafeHandle<Type9>(Entity),
            _archetype.GetComponentUnsafeHandle<Type10>(Entity)
        );
    }

    [Benchmark]
    public bool HasSingleComponentDictionary()
    {
        return _componentDictionary[typeof(Type1)].ContainsKey(Entity);
    }

    [Benchmark]
    public bool HasSingleComponentArchetype()
    {
        return _archetype.HasComponent<Type1>();
    }

    [Benchmark]
    public bool HasTenComponentsDictionary()
    {
        return _componentDictionary[typeof(Type1)].ContainsKey(Entity) &&
               _componentDictionary[typeof(Type2)].ContainsKey(Entity) &&
               _componentDictionary[typeof(Type3)].ContainsKey(Entity) &&
               _componentDictionary[typeof(Type4)].ContainsKey(Entity) &&
               _componentDictionary[typeof(Type5)].ContainsKey(Entity) &&
               _componentDictionary[typeof(Type6)].ContainsKey(Entity) &&
               _componentDictionary[typeof(Type7)].ContainsKey(Entity) &&
               _componentDictionary[typeof(Type8)].ContainsKey(Entity) &&
               _componentDictionary[typeof(Type9)].ContainsKey(Entity) &&
               _componentDictionary[typeof(Type10)].ContainsKey(Entity);
    }

    [Benchmark]
    public bool HasTenComponentsArchetype()
    {
        return _archetype.HasComponent<Type1>() &&
               _archetype.HasComponent<Type2>() &&
               _archetype.HasComponent<Type3>() &&
               _archetype.HasComponent<Type4>() &&
               _archetype.HasComponent<Type5>() &&
               _archetype.HasComponent<Type6>() &&
               _archetype.HasComponent<Type7>() &&
               _archetype.HasComponent<Type8>() &&
               _archetype.HasComponent<Type9>() &&
               _archetype.HasComponent<Type10>();
    }

    [Benchmark]
    public void IterateSingleComponentDictionary()
    {
        foreach (Type1 value in _componentDictionary[typeof(Type1)].Values)
        {
            Consumer.Consume(value);
        }
    }

    [Benchmark]
    public void IterateCastSingleComponentArchetype()
    {
        foreach (var value in _archetype.IterateSingleCast<Type1>())
        {
            Consumer.Consume(value);
        }
    }

    [Benchmark]
    public void IterateDelegateSingleComponentArchetype()
    {
        _archetype.IterateSingleDelegate(static (ref Type1 t1) => Consumer.Consume(t1));
    }

    [Benchmark]
    public void IterateTenComponentsDictionary()
    {
        for (var i = 0; i < N; i++)
        {
            Consumer.Consume((
                (Type1) _componentDictionary[typeof(Type1)][i],
                (Type2) _componentDictionary[typeof(Type2)][i],
                (Type3) _componentDictionary[typeof(Type3)][i],
                (Type4) _componentDictionary[typeof(Type4)][i],
                (Type5) _componentDictionary[typeof(Type5)][i],
                (Type6) _componentDictionary[typeof(Type6)][i],
                (Type7) _componentDictionary[typeof(Type7)][i],
                (Type8) _componentDictionary[typeof(Type8)][i],
                (Type9) _componentDictionary[typeof(Type9)][i],
                (Type10) _componentDictionary[typeof(Type10)][i]
            ));
        }
    }

    [Benchmark]
    public void IterateDelegateTenComponentsArchetype()
    {
        _archetype.IterateDelegate(
            static (ref Type1 t1, ref Type2 t2, ref Type3 t3, ref Type4 t4, ref Type5 t5, ref Type6 t6, ref Type7 t7,
                    ref Type8 t8, ref Type9 t9, ref Type10 t10) =>
                Consumer.Consume((t1, t2, t3, t4, t5, t6, t7, t8, t9, t10))
        );
    }

    [Benchmark]
    public void IterateTenComponentsArchetype()
    {
        var comps = _archetype.Iterate();
        while (comps.MoveNext())
        {
            Consumer.Consume(comps.Current);
        }
    }

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

    private sealed class Archetype<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
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

        public T GetComponentCast<T>(int entity) where T : struct
        {
            Unsafe.SkipInit(out T val);
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

        public T GetComponentCastHandle<T>(int handle) where T : struct
        {
            Unsafe.SkipInit(out T val);
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

        public ref T GetComponentUnsafe<T>(int entity) where T : struct
        {
            Unsafe.SkipInit(out T val);
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

        public ref T GetComponentUnsafeHandle<T>(int handle) where T : struct
        {
            Unsafe.SkipInit(out T val);
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

        public bool HasComponent<T>()
        {
            Unsafe.SkipInit(out T val);
            return val switch
            {
                T1 => true,
                T2 => true,
                T3 => true,
                T4 => true,
                T5 => true,
                T6 => true,
                T7 => true,
                T8 => true,
                T9 => true,
                T10 => true,
                _ => false,
            };
        }

        public IEnumerable<T> IterateSingleCast<T>()
        {
            Unsafe.SkipInit(out T val);
            return val switch
            {
                T1 => _t1Comps.Cast<T>(),
                T2 => _t2Comps.Cast<T>(),
                T3 => _t3Comps.Cast<T>(),
                T4 => _t4Comps.Cast<T>(),
                T5 => _t5Comps.Cast<T>(),
                T6 => _t6Comps.Cast<T>(),
                T7 => _t7Comps.Cast<T>(),
                T8 => _t8Comps.Cast<T>(),
                T9 => _t9Comps.Cast<T>(),
                T10 => _t10Comps.Cast<T>(),
                _ => throw new ArgumentException($"Unknown type: {typeof(T)}")
            };
        }

        private void IterateSingleSpan<T, TComp>([RequireStaticDelegate] IteratorSingle<T> action, TComp[] array) where T : struct
        {
            foreach (ref var comp in array.AsSpan())
            {
                action(ref Unsafe.As<TComp, T>(ref comp));
            }
        }

        public delegate void IteratorSingle<T>(ref T t1);

        public void IterateSingleDelegate<T>([RequireStaticDelegate] IteratorSingle<T> action) where T : struct
        {
            Unsafe.SkipInit(out T val);
            switch (val)
            {
                case T1:
                    IterateSingleSpan(action, _t1Comps);
                    break;
                case T2:
                    IterateSingleSpan(action, _t2Comps);
                    break;
                case T3:
                    IterateSingleSpan(action, _t3Comps);
                    break;
                case T4:
                    IterateSingleSpan(action, _t4Comps);
                    break;
                case T5:
                    IterateSingleSpan(action, _t5Comps);
                    break;
                case T6:
                    IterateSingleSpan(action, _t6Comps);
                    break;
                case T7:
                    IterateSingleSpan(action, _t7Comps);
                    break;
                case T8:
                    IterateSingleSpan(action, _t8Comps);
                    break;
                case T9:
                    IterateSingleSpan(action, _t9Comps);
                    break;
                case T10:
                    IterateSingleSpan(action, _t10Comps);
                    break;
                default:
                    throw new ArgumentException($"Unknown type: {typeof(T)}");
            }
        }

        public delegate void IteratorTen(ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7,
            ref T8 t8, ref T9 t9, ref T10 t10);

        public void IterateDelegate([RequireStaticDelegate] IteratorTen action)
        {
            var t1Span = _t1Comps.AsSpan();
            var t2Span = _t2Comps.AsSpan();
            var t3Span = _t3Comps.AsSpan();
            var t4Span = _t4Comps.AsSpan();
            var t5Span = _t5Comps.AsSpan();
            var t6Span = _t6Comps.AsSpan();
            var t7Span = _t7Comps.AsSpan();
            var t8Span = _t8Comps.AsSpan();
            var t9Span = _t9Comps.AsSpan();
            var t10Span = _t10Comps.AsSpan();

            for (var i = 0; i < _ids.Count; i++)
            {
                action(
                    ref t1Span[i],
                    ref t2Span[i],
                    ref t3Span[i],
                    ref t4Span[i],
                    ref t5Span[i],
                    ref t6Span[i],
                    ref t7Span[i],
                    ref t8Span[i],
                    ref t9Span[i],
                    ref t10Span[i]
                );
            }
        }

        public Enumerator Iterate()
        {
            return new Enumerator(
                _t1Comps,
                _t2Comps,
                _t3Comps,
                _t4Comps,
                _t5Comps,
                _t6Comps,
                _t7Comps,
                _t8Comps,
                _t9Comps,
                _t10Comps
            );
        }

        public ref struct Enumerator
        {
            private readonly Span<T1> _t1Comps;
            private readonly Span<T2> _t2Comps;
            private readonly Span<T3> _t3Comps;
            private readonly Span<T4> _t4Comps;
            private readonly Span<T5> _t5Comps;
            private readonly Span<T6> _t6Comps;
            private readonly Span<T7> _t7Comps;
            private readonly Span<T8> _t8Comps;
            private readonly Span<T9> _t9Comps;
            private readonly Span<T10> _t10Comps;
            private int _index;

            public Enumerator(Span<T1> t1Comps, Span<T2> t2Comps, Span<T3> t3Comps, Span<T4> t4Comps, Span<T5> t5Comps, Span<T6> t6Comps, Span<T7> t7Comps, Span<T8> t8Comps, Span<T9> t9Comps, Span<T10> t10Comps)
            {
                _t1Comps = t1Comps;
                _t2Comps = t2Comps;
                _t3Comps = t3Comps;
                _t4Comps = t4Comps;
                _t5Comps = t5Comps;
                _t6Comps = t6Comps;
                _t7Comps = t7Comps;
                _t8Comps = t8Comps;
                _t9Comps = t9Comps;
                _t10Comps = t10Comps;
            }

            public bool MoveNext()
            {
                var index = _index + 1;
                if (index < _t1Comps.Length)
                {
                    _index = index;
                    return true;
                }

                return false;
            }

            public (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) Current => (
                _t1Comps[_index],
                _t2Comps[_index],
                _t3Comps[_index],
                _t4Comps[_index],
                _t5Comps[_index],
                _t6Comps[_index],
                _t7Comps[_index],
                _t8Comps[_index],
                _t9Comps[_index],
                _t10Comps[_index]
            );
        }
    }
}
