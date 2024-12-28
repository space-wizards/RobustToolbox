using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Robust.Shared.Scripting
{
    [PublicAPI]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "CA1822")]
    public abstract class ScriptGlobalsShared : IInvocationContext
    {
        private const BindingFlags DefaultHelpFlags =
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;

        [field: Dependency] public IEntityManager ent { get; } = default!;
        [field: Dependency] public IEntitySystemManager esm { get; } = default!;
        [field: Dependency] public IPrototypeManager prot { get; } = default!;
        [field: Dependency] public IMapManager map { get; } = default!;
        [field: Dependency] public IDependencyCollection dependencies { get; } = default!;

        [field: Dependency] public ToolshedManager shed { get; } = default!;

        public ToolshedManager Toolshed => shed;
        public ToolshedEnvironment Environment => shed.DefaultEnvironment;

        protected ScriptGlobalsShared(IDependencyCollection dependencies)
        {
            dependencies.InjectDependencies(this);
        }

        public IEnumerable<T> protos<T>() where T : class, IPrototype
        {
            return prot.EnumeratePrototypes<T>();
        }

        public IEnumerable<EntityPrototype> eprotos => prot.EnumeratePrototypes<EntityPrototype>();

        public EntityCoordinates gpos(double x, double y, int gridId)
        {
            return gpos(x, y, new EntityUid(gridId));
        }

        public EntityCoordinates gpos(double x, double y, EntityUid gridId)
        {
            return new EntityCoordinates(gridId, new Vector2((float) x, (float) y));
        }

        public EntityUid eid(int i)
        {
            return new(i);
        }

        public MapGridComponent getgrid(int i)
        {
            return ent.GetComponent<MapGridComponent>(new EntityUid(i));
        }

        public MapGridComponent getgrid(EntityUid mapId)
        {
            return ent.GetComponent<MapGridComponent>(mapId);
        }

        public T res<T>()
        {
            return dependencies.Resolve<T>();
        }

        public T ressys<T>() where T : EntitySystem
        {
            return esm.GetEntitySystem<T>();
        }

        public object? prop(object target, string name)
        {
            var prop = (PropertyInfo?) ReflectionGetInstanceMember(target.GetType(), MemberTypes.Property, name);
            return prop!.GetValue(target);
        }

        public void setprop(object target, string name, object? value)
        {
            var prop = (PropertyInfo?) ReflectionGetInstanceMember(target.GetType(), MemberTypes.Property, name);
            prop!.SetValue(target, value);
        }

        public object? fld(object target, string name)
        {
            var fld = (FieldInfo?) ReflectionGetInstanceMember(target.GetType(), MemberTypes.Field, name);
            return fld!.GetValue(target);
        }

        public void setfld(object target, string name, object? value)
        {
            var fld = (FieldInfo?) ReflectionGetInstanceMember(target.GetType(), MemberTypes.Field, name);
            fld!.SetValue(target, value);
        }

        public object? call(object target, string name, params object[] args)
        {
            var t = target.GetType();
            // TODO: overloads
            var m = (MethodInfo?) ReflectionGetInstanceMember(t, MemberTypes.Method, name);
            return m!.Invoke(target, args);
        }

        public void help()
        {
            help(GetType(), DefaultHelpFlags, false, false);
        }

        public void help(object obj, BindingFlags flags = DefaultHelpFlags, bool specialNameMethods = true, bool modifiers = true)
        {
            help(obj.GetType(), flags, specialNameMethods, modifiers);
        }

        public void help(Type type, BindingFlags flags = DefaultHelpFlags, bool specialNameMethods = true, bool modifiers = true)
        {
            var builder = new StringBuilder();

            foreach (var member in type.GetMembers(flags))
            {
                switch (member.MemberType)
                {
                    case MemberTypes.Method:
                        var method = (MethodInfo) member;

                        if (!specialNameMethods && method.IsSpecialName)
                            continue; // Let's not print constructors, property methods, etc.

                        builder.Append(method.PrintMethodSignature(modifiers));
                        builder.AppendLine(";");
                        break;

                    case MemberTypes.Property:
                        builder.AppendLine(((PropertyInfo)member).PrintPropertySignature(modifiers, true));
                        break;

                    case MemberTypes.Field:
                        builder.Append(((FieldInfo) member).PrintFieldSignature(modifiers));
                        builder.AppendLine(";");
                        break;

                    default:
                        continue;
                }

                builder.AppendLine();
            }

            // This is slow, so do it all at once.
            WriteSyntax(builder.ToString());
        }

        protected abstract void WriteSyntax(object toString);
        public abstract void write(object toString);
        public abstract void show(object obj);

        public object? tsh(string toolshedCommand)
        {
            shed.InvokeCommand(this, toolshedCommand, null, out var res);
            return res;
        }

        public T tsh<T>(string toolshedCommand)
        {
            shed.InvokeCommand(this, toolshedCommand, null, out var res);
            return (T)res!;
        }

        public TOut tsh<TIn, TOut>(TIn value, string toolshedCommand)
        {
            shed.InvokeCommand(this, toolshedCommand, value, out var res);
            return (TOut)res!;
        }

        #region EntityManager proxy methods
        public T Comp<T>(EntityUid uid) where T : IComponent
            => ent.GetComponent<T>(uid);

        public bool TryComp<T>(EntityUid uid, out T? comp) where T : IComponent
            => ent.TryGetComponent(uid, out comp);

        public bool HasComp<T>(EntityUid uid)  where T : IComponent
            => ent.HasComponent<T>(uid);

        public EntityUid Spawn(string? prototype, EntityCoordinates position)
            => ent.SpawnEntity(prototype, position);

        public void Del(EntityUid uid)
            => ent.DeleteEntity(uid);

        public void Dirty(EntityUid uid)
            => ent.DirtyEntity(uid);

#pragma warning disable CS0618 // Type or member is obsolete
        // Remove this helper when component.Owner finally gets removed.
        public void Dirty(Component comp)
            => ent.Dirty(comp.Owner, comp);
#pragma warning restore CS0618 // Type or member is obsolete

        public string Name(EntityUid uid)
            => ent.GetComponent<MetaDataComponent>(uid).EntityName;

        public string Desc(EntityUid uid)
            => ent.GetComponent<MetaDataComponent>(uid).EntityDescription;

        public EntityPrototype? Prototype(EntityUid uid)
            => ent.GetComponent<MetaDataComponent>(uid).EntityPrototype;

        [return: NotNullIfNotNull("uid")]
        public EntityStringRepresentation? ToPrettyString(EntityUid? uid)
            => ent.ToPrettyString(uid);

        public IEnumerable<IComponent> AllComps(EntityUid uid)
            => ent.GetComponents(uid);

        public TransformComponent Transform(EntityUid uid)
            => ent.GetComponent<TransformComponent>(uid);

        public MetaDataComponent MetaData(EntityUid uid)
            => ent.GetComponent<MetaDataComponent>(uid);

        public EntityCoordinates Pos(EntityUid uid) => Transform(uid).Coordinates;

        public IEnumerable<TComp1> Query<TComp1>(bool includePaused = false)
            where TComp1 : IComponent
        {
            return ent.EntityQuery<TComp1>(includePaused);
        }

        public IEnumerable<(TComp1, TComp2)> Query<TComp1, TComp2>(bool includePaused = false)
            where TComp1 : IComponent
            where TComp2 : IComponent
        {
            return ent.EntityQuery<TComp1, TComp2>(includePaused);
        }

        public IEnumerable<(TComp1, TComp2, TComp3)> Query<TComp1, TComp2, TComp3>(bool includePaused = false)
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent
        {
            return ent.EntityQuery<TComp1, TComp2, TComp3>(includePaused);
        }
        #endregion

        public bool CheckInvokable(CommandSpec command, out IConError? error)
        {
            error = null;
            return true; // Do as I say!
        }

        public NetUserId? User => null;
        public ICommonSession? Session => null;

        public void WriteLine(string line)
        {
            write(line);
        }

        public void ReportError(IConError err)
        {
            write(err);
        }

        public IEnumerable<IConError> GetErrors()
        {
            return Array.Empty<IConError>();
        }

        public bool HasErrors => false;

        public void ClearErrors()
        {
        }

        /// <inheritdoc />
        public object? ReadVar(string name)
        {
            return Variables.GetValueOrDefault(name);
        }

        /// <inheritdoc />
        public void WriteVar(string name, object? value)
        {
            Variables[name] = value;
        }

        /// <inheritdoc />
        public IEnumerable<string> GetVars()
        {
            return Variables.Keys;
        }

        public Dictionary<string, object?> Variables { get; } = new();

        private static MemberInfo? ReflectionGetInstanceMember(Type type, MemberTypes memberType, string name)
        {
            for (var curType = type; curType != null; curType = curType.BaseType)
            {
                var member = curType.GetMember(
                    name,
                    memberType,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (member.Length > 0)
                    return member[0];
            }

            return null;
        }
    }
}
