using System;
using JetBrains.Annotations;
using Robust.Shared.IoC;

namespace Robust.Shared.Prototypes
{
    public abstract class PrototypeReference : IDisposable
    {
        [Dependency] protected IPrototypeManager _manager = null!;

        public readonly string ID;

        public PrototypeReference(string id)
        {
            ID = id;
            Register();
        }

        public void Dispose()
        {
            Unregister();
        }

        protected abstract void Register();
        protected abstract void Unregister();
        public abstract void RefreshPrototype();

        public event Action? ReferenceChanged;

        protected void InvokeReferenceChanged()
        {
            ReferenceChanged?.Invoke();
        }
    }

    public abstract class PrototypeReference<T> : PrototypeReference where T : IPrototype
    {
        protected PrototypeReference([NotNull] string id) : base(id) { }
        public abstract T? Prototype { get; }

        protected override void Register()
        {
            _manager.RegisterPrototypeReference(this);
        }

        protected override void Unregister()
        {
            _manager.UnregisterPrototypeReference(this);
        }

        public static PrototypeReference<T> Create(string id)
        {
            if (typeof(T).IsValueType)
            {
                return (PrototypeReference<T>) Activator.CreateInstance(
                    typeof(PrototypeStructReference<>).MakeGenericType(typeof(T)), id)!;
            }

            return (PrototypeReference<T>) Activator.CreateInstance(
                typeof(PrototypeClassReference<>).MakeGenericType(typeof(T)), id)!;
        }
    }

    public class PrototypeClassReference<T> : PrototypeReference<T> where T : class, IPrototype
    {
        public override T? Prototype => PrototypeRef.TryGetTarget(out var prototype) ? prototype : null;

        public PrototypeClassReference([NotNull] string id) : base(id) {}

        public override void RefreshPrototype()
        {
            PrototypeRef.SetTarget(_manager.TryIndex(ID, out T? prototype) ? prototype : null);
            InvokeReferenceChanged();
        }

        public WeakReference<T?> PrototypeRef { get; private set; } = new(null);
    }

    public class PrototypeStructReference<T> : PrototypeReference<T> where T : struct, IPrototype
    {
        public PrototypeStructReference([NotNull] string id) : base(id){ }

        // aaaaaaaaa how are generics&nullability so FUCKED
        public override T Prototype { get; }

        public override void RefreshPrototype()
        {
            throw new NotImplementedException();
        }
    }
}
