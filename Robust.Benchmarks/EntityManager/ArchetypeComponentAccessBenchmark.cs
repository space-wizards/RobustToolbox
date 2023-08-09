using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Benchmarks.EntityManager;

[MemoryDiagnoser]
[Virtual]
public class ArchetypeComponentAccessBenchmark
{
    private const int N = 10000;
    private const int Entity = 1584;

    private Dictionary<Type, Dictionary<int, object>> _classDictionary = default!;
    private Dictionary<Type, Dictionary<int, object>> _structDictionary = default!;
    private Dictionary<int, object>[] _entTraitArray = default!;
    private Archetype<Struct1, Struct2, Struct3, Struct4, Struct5, Struct6, Struct7, Struct8, Struct9, Struct10> _archetype = default!;
    private static readonly Consumer Consumer = new();

    [GlobalSetup]
    public void GlobalSetup()
    {
        _classDictionary = new Dictionary<Type, Dictionary<int, object>>(N)
        {
            [typeof(Class1)] = new(),
            [typeof(Class2)] = new(),
            [typeof(Class3)] = new(),
            [typeof(Class4)] = new(),
            [typeof(Class5)] = new(),
            [typeof(Class6)] = new(),
            [typeof(Class7)] = new(),
            [typeof(Class8)] = new(),
            [typeof(Class9)] = new(),
            [typeof(Class10)] = new(),
        };
        _structDictionary = new Dictionary<Type, Dictionary<int, object>>(N)
        {
            [typeof(Struct1)] = new(),
            [typeof(Struct2)] = new(),
            [typeof(Struct3)] = new(),
            [typeof(Struct4)] = new(),
            [typeof(Struct5)] = new(),
            [typeof(Struct6)] = new(),
            [typeof(Struct7)] = new(),
            [typeof(Struct8)] = new(),
            [typeof(Struct9)] = new(),
            [typeof(Struct10)] = new(),
        };

        _entTraitArray = new Dictionary<int, object>[20];
        for (var i = 0; i < 20; i++)
        {
            _entTraitArray[i] = new Dictionary<int, object>();
        }

        _archetype = new Archetype<Struct1, Struct2, Struct3, Struct4, Struct5, Struct6, Struct7, Struct8, Struct9, Struct10>(N);

        for (var i = 0; i < N; i++)
        {
            var c1 = new Class1();
            var c2 = new Class2();
            var c3 = new Class3();
            var c4 = new Class4();
            var c5 = new Class5();
            var c6 = new Class6();
            var c7 = new Class7();
            var c8 = new Class8();
            var c9 = new Class9();
            var c10 = new Class10();

            _classDictionary[typeof(Class1)][i] = c1;
            _classDictionary[typeof(Class2)][i] = c2;
            _classDictionary[typeof(Class3)][i] = c3;
            _classDictionary[typeof(Class4)][i] = c4;
            _classDictionary[typeof(Class5)][i] = c5;
            _classDictionary[typeof(Class6)][i] = c6;
            _classDictionary[typeof(Class7)][i] = c7;
            _classDictionary[typeof(Class8)][i] = c8;
            _classDictionary[typeof(Class9)][i] = c9;
            _classDictionary[typeof(Class10)][i] = c10;

            _entTraitArray[CompIdx.ArrayIndex<Class1>()][i] = c1;
            _entTraitArray[CompIdx.ArrayIndex<Class2>()][i] = c2;
            _entTraitArray[CompIdx.ArrayIndex<Class3>()][i] = c3;
            _entTraitArray[CompIdx.ArrayIndex<Class4>()][i] = c4;
            _entTraitArray[CompIdx.ArrayIndex<Class5>()][i] = c5;
            _entTraitArray[CompIdx.ArrayIndex<Class6>()][i] = c6;
            _entTraitArray[CompIdx.ArrayIndex<Class7>()][i] = c7;
            _entTraitArray[CompIdx.ArrayIndex<Class8>()][i] = c8;
            _entTraitArray[CompIdx.ArrayIndex<Class9>()][i] = c9;
            _entTraitArray[CompIdx.ArrayIndex<Class10>()][i] = c10;

            var s1 = new Struct1();
            var s2 = new Struct2();
            var s3 = new Struct3();
            var s4 = new Struct4();
            var s5 = new Struct5();
            var s6 = new Struct6();
            var s7 = new Struct7();
            var s8 = new Struct8();
            var s9 = new Struct9();
            var s10 = new Struct10();

            _structDictionary[typeof(Struct1)][i] = s1;
            _structDictionary[typeof(Struct2)][i] = s2;
            _structDictionary[typeof(Struct3)][i] = s3;
            _structDictionary[typeof(Struct4)][i] = s4;
            _structDictionary[typeof(Struct5)][i] = s5;
            _structDictionary[typeof(Struct6)][i] = s6;
            _structDictionary[typeof(Struct7)][i] = s7;
            _structDictionary[typeof(Struct8)][i] = s8;
            _structDictionary[typeof(Struct9)][i] = s9;
            _structDictionary[typeof(Struct10)][i] = s10;

            _entTraitArray[CompIdx.ArrayIndex<Struct1>()][i] = s1;
            _entTraitArray[CompIdx.ArrayIndex<Struct2>()][i] = s2;
            _entTraitArray[CompIdx.ArrayIndex<Struct3>()][i] = s3;
            _entTraitArray[CompIdx.ArrayIndex<Struct4>()][i] = s4;
            _entTraitArray[CompIdx.ArrayIndex<Struct5>()][i] = s5;
            _entTraitArray[CompIdx.ArrayIndex<Struct6>()][i] = s6;
            _entTraitArray[CompIdx.ArrayIndex<Struct7>()][i] = s7;
            _entTraitArray[CompIdx.ArrayIndex<Struct8>()][i] = s8;
            _entTraitArray[CompIdx.ArrayIndex<Struct9>()][i] = s9;
            _entTraitArray[CompIdx.ArrayIndex<Struct10>()][i] = s10;

            _archetype.AddEntity(i);
            _archetype.AddComponent(i, new Struct1());
            _archetype.AddComponent(i, new Struct2());
            _archetype.AddComponent(i, new Struct3());
            _archetype.AddComponent(i, new Struct4());
            _archetype.AddComponent(i, new Struct5());
            _archetype.AddComponent(i, new Struct6());
            _archetype.AddComponent(i, new Struct7());
            _archetype.AddComponent(i, new Struct8());
            _archetype.AddComponent(i, new Struct9());
            _archetype.AddComponent(i, new Struct10());
        }
    }

    [Benchmark]
    public Struct1 GetSingleComponentDictionary()
    {
        return (Struct1) _structDictionary[typeof(Struct1)][Entity];
    }

    [Benchmark]
    public Struct1 GetSingleComponentArchetypeCast()
    {
        return _archetype.GetComponentCast<Struct1>(Entity);
    }

    [Benchmark]
    public Struct1 GetSingleComponentArchetypeCastHandle()
    {
        // Handle is the same as the id
        return _archetype.GetComponentCastHandle<Struct1>(Entity);
    }

    [Benchmark]
    public Struct1 GetSingleComponentArchetypeUnsafe()
    {
        return _archetype.GetComponentUnsafe<Struct1>(Entity);
    }

    [Benchmark]
    public Struct1 GetSingleComponentArchetypeUnsafeHandle()
    {
        // Handle is the same as the id
        return _archetype.GetComponentUnsafeHandle<Struct1>(Entity);
    }

    [Benchmark]
    public (Struct1, Struct2, Struct3, Struct4, Struct5, Struct6, Struct7, Struct8, Struct9, Struct10) GetTenComponentsDictionary()
    {
        return (
            (Struct1) _structDictionary[typeof(Struct1)][Entity],
            (Struct2) _structDictionary[typeof(Struct2)][Entity],
            (Struct3) _structDictionary[typeof(Struct3)][Entity],
            (Struct4) _structDictionary[typeof(Struct4)][Entity],
            (Struct5) _structDictionary[typeof(Struct5)][Entity],
            (Struct6) _structDictionary[typeof(Struct6)][Entity],
            (Struct7) _structDictionary[typeof(Struct7)][Entity],
            (Struct8) _structDictionary[typeof(Struct8)][Entity],
            (Struct9) _structDictionary[typeof(Struct9)][Entity],
            (Struct10) _structDictionary[typeof(Struct10)][Entity]
        );
    }

    [Benchmark]
    public (Struct1, Struct2, Struct3, Struct4, Struct5, Struct6, Struct7, Struct8, Struct9, Struct10) GetTenComponentsArchetypeCast()
    {
        return (
            _archetype.GetComponentCast<Struct1>(Entity),
            _archetype.GetComponentCast<Struct2>(Entity),
            _archetype.GetComponentCast<Struct3>(Entity),
            _archetype.GetComponentCast<Struct4>(Entity),
            _archetype.GetComponentCast<Struct5>(Entity),
            _archetype.GetComponentCast<Struct6>(Entity),
            _archetype.GetComponentCast<Struct7>(Entity),
            _archetype.GetComponentCast<Struct8>(Entity),
            _archetype.GetComponentCast<Struct9>(Entity),
            _archetype.GetComponentCast<Struct10>(Entity)
        );
    }

    [Benchmark]
    public (Struct1, Struct2, Struct3, Struct4, Struct5, Struct6, Struct7, Struct8, Struct9, Struct10) GetTenComponentsArchetypeCastHandle()
    {
        // Handle is the same as the id
        return (
            _archetype.GetComponentCastHandle<Struct1>(Entity),
            _archetype.GetComponentCastHandle<Struct2>(Entity),
            _archetype.GetComponentCastHandle<Struct3>(Entity),
            _archetype.GetComponentCastHandle<Struct4>(Entity),
            _archetype.GetComponentCastHandle<Struct5>(Entity),
            _archetype.GetComponentCastHandle<Struct6>(Entity),
            _archetype.GetComponentCastHandle<Struct7>(Entity),
            _archetype.GetComponentCastHandle<Struct8>(Entity),
            _archetype.GetComponentCastHandle<Struct9>(Entity),
            _archetype.GetComponentCastHandle<Struct10>(Entity)
        );
    }

    [Benchmark]
    public (Struct1, Struct2, Struct3, Struct4, Struct5, Struct6, Struct7, Struct8, Struct9, Struct10) GetTenComponentsArchetypeUnsafe()
    {
        return (
            _archetype.GetComponentUnsafe<Struct1>(Entity),
            _archetype.GetComponentUnsafe<Struct2>(Entity),
            _archetype.GetComponentUnsafe<Struct3>(Entity),
            _archetype.GetComponentUnsafe<Struct4>(Entity),
            _archetype.GetComponentUnsafe<Struct5>(Entity),
            _archetype.GetComponentUnsafe<Struct6>(Entity),
            _archetype.GetComponentUnsafe<Struct7>(Entity),
            _archetype.GetComponentUnsafe<Struct8>(Entity),
            _archetype.GetComponentUnsafe<Struct9>(Entity),
            _archetype.GetComponentUnsafe<Struct10>(Entity)
        );
    }

    [Benchmark]
    public (Struct1, Struct2, Struct3, Struct4, Struct5, Struct6, Struct7, Struct8, Struct9, Struct10)
        GetTenComponentsArchetypeUnsafeHandle()
    {
        // Handle is the same as the id
        return (
            _archetype.GetComponentUnsafeHandle<Struct1>(Entity),
            _archetype.GetComponentUnsafeHandle<Struct2>(Entity),
            _archetype.GetComponentUnsafeHandle<Struct3>(Entity),
            _archetype.GetComponentUnsafeHandle<Struct4>(Entity),
            _archetype.GetComponentUnsafeHandle<Struct5>(Entity),
            _archetype.GetComponentUnsafeHandle<Struct6>(Entity),
            _archetype.GetComponentUnsafeHandle<Struct7>(Entity),
            _archetype.GetComponentUnsafeHandle<Struct8>(Entity),
            _archetype.GetComponentUnsafeHandle<Struct9>(Entity),
            _archetype.GetComponentUnsafeHandle<Struct10>(Entity)
        );
    }

    [Benchmark]
    public bool HasSingleComponentDictionary()
    {
        return _structDictionary[typeof(Struct1)].ContainsKey(Entity);
    }

    [Benchmark]
    public bool HasSingleComponentArchetype()
    {
        return _archetype.HasComponent<Struct1>();
    }

    [Benchmark]
    public bool HasTenComponentsDictionary()
    {
        return _structDictionary[typeof(Struct1)].ContainsKey(Entity) &&
               _structDictionary[typeof(Struct2)].ContainsKey(Entity) &&
               _structDictionary[typeof(Struct3)].ContainsKey(Entity) &&
               _structDictionary[typeof(Struct4)].ContainsKey(Entity) &&
               _structDictionary[typeof(Struct5)].ContainsKey(Entity) &&
               _structDictionary[typeof(Struct6)].ContainsKey(Entity) &&
               _structDictionary[typeof(Struct7)].ContainsKey(Entity) &&
               _structDictionary[typeof(Struct8)].ContainsKey(Entity) &&
               _structDictionary[typeof(Struct9)].ContainsKey(Entity) &&
               _structDictionary[typeof(Struct10)].ContainsKey(Entity);
    }

    [Benchmark]
    public bool HasTenComponentsArchetype()
    {
        return _archetype.HasComponent<Struct1>() &&
               _archetype.HasComponent<Struct2>() &&
               _archetype.HasComponent<Struct3>() &&
               _archetype.HasComponent<Struct4>() &&
               _archetype.HasComponent<Struct5>() &&
               _archetype.HasComponent<Struct6>() &&
               _archetype.HasComponent<Struct7>() &&
               _archetype.HasComponent<Struct8>() &&
               _archetype.HasComponent<Struct9>() &&
               _archetype.HasComponent<Struct10>();
    }

    [Benchmark]
    public void IterateSingleComponentDictionary()
    {
        foreach (Struct1 value in _structDictionary[typeof(Struct1)].Values)
        {
            Consumer.Consume(value);
        }
    }

    [Benchmark]
    public void IterateCastSingleComponentArchetype()
    {
        foreach (var value in _archetype.IterateSingleCast<Struct1>())
        {
            Consumer.Consume(value);
        }
    }

    [Benchmark]
    public void IterateDelegateSingleComponentArchetype()
    {
        _archetype.IterateSingleDelegate(static (ref Struct1 t1) => Consumer.Consume(t1));
    }

    [Benchmark]
    public void IterateTenClassesDictionary()
    {
        for (var i = 0; i < N; i++)
        {
            Consumer.Consume((
                (Class1) _classDictionary[typeof(Class1)][i],
                (Class2) _classDictionary[typeof(Class2)][i],
                (Class3) _classDictionary[typeof(Class3)][i],
                (Class4) _classDictionary[typeof(Class4)][i],
                (Class5) _classDictionary[typeof(Class5)][i],
                (Class6) _classDictionary[typeof(Class6)][i],
                (Class7) _classDictionary[typeof(Class7)][i],
                (Class8) _classDictionary[typeof(Class8)][i],
                (Class9) _classDictionary[typeof(Class9)][i],
                (Class10) _classDictionary[typeof(Class10)][i]
            ));
        }
    }

    [Benchmark]
    public void IterateTenStructsDictionary()
    {
        for (var i = 0; i < N; i++)
        {
            Consumer.Consume((
                (Struct1) _structDictionary[typeof(Struct1)][i],
                (Struct2) _structDictionary[typeof(Struct2)][i],
                (Struct3) _structDictionary[typeof(Struct3)][i],
                (Struct4) _structDictionary[typeof(Struct4)][i],
                (Struct5) _structDictionary[typeof(Struct5)][i],
                (Struct6) _structDictionary[typeof(Struct6)][i],
                (Struct7) _structDictionary[typeof(Struct7)][i],
                (Struct8) _structDictionary[typeof(Struct8)][i],
                (Struct9) _structDictionary[typeof(Struct9)][i],
                (Struct10) _structDictionary[typeof(Struct10)][i]
            ));
        }
    }

    [Benchmark]
    public void IterateTenClassesTraitDictionary()
    {
        var enumerator =
            new DictionaryEnumerator<Class1, Class2, Class3, Class4, Class5, Class6, Class7, Class8, Class9, Class10>(
                _entTraitArray[CompIdx.ArrayIndex<Class1>()],
                _entTraitArray[CompIdx.ArrayIndex<Class2>()],
                _entTraitArray[CompIdx.ArrayIndex<Class3>()],
                _entTraitArray[CompIdx.ArrayIndex<Class4>()],
                _entTraitArray[CompIdx.ArrayIndex<Class5>()],
                _entTraitArray[CompIdx.ArrayIndex<Class6>()],
                _entTraitArray[CompIdx.ArrayIndex<Class7>()],
                _entTraitArray[CompIdx.ArrayIndex<Class8>()],
                _entTraitArray[CompIdx.ArrayIndex<Class9>()],
                _entTraitArray[CompIdx.ArrayIndex<Class10>()]
            );

        while (enumerator.MoveNext(
                   out var t1,
                   out var t2,
                   out var t3,
                   out var t4,
                   out var t5,
                   out var t6,
                   out var t7,
                   out var t8,
                   out var t9,
                   out var t10
               ))
        {
            Consumer.Consume((t1, t2, t3, t4, t5, t6, t7, t8, t9, t10));
        }
    }

    [Benchmark]
    public void IterateTenStructsTraitDictionary()
    {
        var enumerator =
            new DictionaryEnumerator<Struct1, Struct2, Struct3, Struct4, Struct5, Struct6, Struct7, Struct8, Struct9, Struct10>(
                _entTraitArray[CompIdx.ArrayIndex<Struct1>()],
                _entTraitArray[CompIdx.ArrayIndex<Struct2>()],
                _entTraitArray[CompIdx.ArrayIndex<Struct3>()],
                _entTraitArray[CompIdx.ArrayIndex<Struct4>()],
                _entTraitArray[CompIdx.ArrayIndex<Struct5>()],
                _entTraitArray[CompIdx.ArrayIndex<Struct6>()],
                _entTraitArray[CompIdx.ArrayIndex<Struct7>()],
                _entTraitArray[CompIdx.ArrayIndex<Struct8>()],
                _entTraitArray[CompIdx.ArrayIndex<Struct9>()],
                _entTraitArray[CompIdx.ArrayIndex<Struct10>()]
            );

        while (enumerator.MoveNext(
                   out var t1,
                   out var t2,
                   out var t3,
                   out var t4,
                   out var t5,
                   out var t6,
                   out var t7,
                   out var t8,
                   out var t9,
                   out var t10
               ))
        {
            Consumer.Consume((t1, t2, t3, t4, t5, t6, t7, t8, t9, t10));
        }
    }

    [Benchmark]
    public void IterateDelegateTenStructsArchetype()
    {
        _archetype.IterateDelegate(
            static (ref Struct1 t1, ref Struct2 t2, ref Struct3 t3, ref Struct4 t4, ref Struct5 t5, ref Struct6 t6, ref Struct7 t7,
                    ref Struct8 t8, ref Struct9 t9, ref Struct10 t10) =>
                Consumer.Consume((t1, t2, t3, t4, t5, t6, t7, t8, t9, t10))
        );
    }

    [Benchmark]
    public void IterateTenStructsArchetype()
    {
        var comps = _archetype.Iterate();
        while (comps.MoveNext())
        {
            Consumer.Consume(comps.Current);
        }
    }

    public class Class1{}
    public class Class2{}
    public class Class3{}
    public class Class4{}
    public class Class5{}
    public class Class6{}
    public class Class7{}
    public class Class8{}
    public class Class9{}
    public class Class10{}

    public struct Struct1{}
    public struct Struct2{}
    public struct Struct3{}
    public struct Struct4{}
    public struct Struct5{}
    public struct Struct6{}
    public struct Struct7{}
    public struct Struct8{}
    public struct Struct9{}
    public struct Struct10{}

    public sealed class Archetype<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
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

        private void IterateSingleSpan<T, TComp>([RequireStaticDelegate] IteratorSingle<T> action, TComp[] array)
            where T : struct
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

            public Enumerator(
                Span<T1> t1Comps,
                Span<T2> t2Comps,
                Span<T3> t3Comps,
                Span<T4> t4Comps,
                Span<T5> t5Comps,
                Span<T6> t6Comps,
                Span<T7> t7Comps,
                Span<T8> t8Comps,
                Span<T9> t9Comps,
                Span<T10> t10Comps)
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

    public readonly struct CompIdx : IEquatable<CompIdx>
    {
        private static readonly ReaderWriterLockSlim SlowStoreLock = new();
        private static readonly Dictionary<Type, CompIdx> SlowStore = new();

        internal readonly int Value;

        internal static CompIdx Index<T>() => Store<T>.Index;

        internal static CompIdx Index(Type t)
        {
            using (SlowStoreLock.ReadGuard())
            {
                if (SlowStore.TryGetValue(t, out var idx))
                    return idx;
            }

            // Doesn't exist in the store, get a write lock and add it.
            using (SlowStoreLock.WriteGuard())
            {
                var idx = (CompIdx) typeof(Store<>)
                    .MakeGenericType(t)
                    .GetField(nameof(Store<int>.Index), BindingFlags.Static | BindingFlags.Public)!
                    .GetValue(null)!;

                SlowStore[t] = idx;
                return idx;
            }
        }

        internal static int ArrayIndex<T>() => Index<T>().Value;
        internal static int ArrayIndex(Type type) => Index(type).Value;

        internal static void AssignArray<T>(ref T[] array, CompIdx idx, T value)
        {
            RefArray(ref array, idx) = value;
        }

        internal static ref T RefArray<T>(ref T[] array, CompIdx idx)
        {
            var curLength = array.Length;
            if (curLength <= idx.Value)
            {
                var newLength = MathHelper.NextPowerOfTwo(System.Math.Max(8, idx.Value));
                Array.Resize(ref array, newLength);
            }

            return ref array[idx.Value];
        }

        internal static int _CompIdxMaster = -1;

        private static class Store<T>
        {
            // ReSharper disable once StaticMemberInGenericType
            public static readonly CompIdx Index = new(Interlocked.Increment(ref _CompIdxMaster));
        }

        internal CompIdx(int value)
        {
            Value = value;
        }

        public bool Equals(CompIdx other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            return obj is CompIdx other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public static bool operator ==(CompIdx left, CompIdx right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CompIdx left, CompIdx right)
        {
            return !left.Equals(right);
        }
    }

    public struct DictionaryEnumerator<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
    {
        private Dictionary<int, object>.Enumerator _t1Comps;
        private readonly Dictionary<int, object> _t2Comps;
        private readonly Dictionary<int, object> _t3Comps;
        private readonly Dictionary<int, object> _t4Comps;
        private readonly Dictionary<int, object> _t5Comps;
        private readonly Dictionary<int, object> _t6Comps;
        private readonly Dictionary<int, object> _t7Comps;
        private readonly Dictionary<int, object> _t8Comps;
        private readonly Dictionary<int, object> _t9Comps;
        private readonly Dictionary<int, object> _t10Comps;

        public DictionaryEnumerator(
            Dictionary<int, object> t1Comps,
            Dictionary<int, object> t2Comps,
            Dictionary<int, object> t3Comps,
            Dictionary<int, object> t4Comps,
            Dictionary<int, object> t5Comps,
            Dictionary<int, object> t6Comps,
            Dictionary<int, object> t7Comps,
            Dictionary<int, object> t8Comps,
            Dictionary<int, object> t9Comps,
            Dictionary<int, object> t10Comps)
        {
            _t1Comps = t1Comps.GetEnumerator();
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

        public bool MoveNext(
            out T1 comp1,
            out T2 comp2,
            out T3 comp3,
            out T4 comp4,
            out T5 comp5,
            out T6 comp6,
            out T7 comp7,
            out T8 comp8,
            out T9 comp9,
            out T10 comp10)
        {
            while (true)
            {
                if (!_t1Comps.MoveNext())
                {
                    comp1 = default!;
                    comp2 = default!;
                    comp3 = default!;
                    comp4 = default!;
                    comp5 = default!;
                    comp6 = default!;
                    comp7 = default!;
                    comp8 = default!;
                    comp9 = default!;
                    comp10 = default!;
                    return false;
                }

                var current = _t1Comps.Current;

                if (!_t2Comps.TryGetValue(current.Key, out var comp2Obj))
                {
                    continue;
                }

                if (!_t3Comps.TryGetValue(current.Key, out var comp3Obj))
                {
                    continue;
                }

                if (!_t4Comps.TryGetValue(current.Key, out var comp4Obj))
                {
                    continue;
                }

                if (!_t5Comps.TryGetValue(current.Key, out var comp5Obj))
                {
                    continue;
                }

                if (!_t6Comps.TryGetValue(current.Key, out var comp6Obj))
                {
                    continue;
                }

                if (!_t7Comps.TryGetValue(current.Key, out var comp7Obj))
                {
                    continue;
                }

                if (!_t8Comps.TryGetValue(current.Key, out var comp8Obj))
                {
                    continue;
                }

                if (!_t9Comps.TryGetValue(current.Key, out var comp9Obj))
                {
                    continue;
                }

                if (!_t10Comps.TryGetValue(current.Key, out var comp10Obj))
                {
                    continue;
                }

                comp1 = (T1) current.Value;
                comp2 = (T2) comp2Obj;
                comp3 = (T3) comp3Obj;
                comp4 = (T4) comp4Obj;
                comp5 = (T5) comp5Obj;
                comp6 = (T6) comp6Obj;
                comp7 = (T7) comp7Obj;
                comp8 = (T8) comp8Obj;
                comp9 = (T9) comp9Obj;
                comp10 = (T10) comp10Obj;
                return true;
            }
        }
    }
}
