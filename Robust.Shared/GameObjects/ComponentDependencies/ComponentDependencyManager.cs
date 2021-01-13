using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects.ComponentDependencies
{
    public class ComponentDependencyManager : IComponentDependencyManager
    {
        [IoC.Dependency] private readonly IComponentFactory _componentFactory = null!;

        /// <summary>
        /// Cache of queries and their corresponding field offsets
        /// </summary>
        private readonly
            Dictionary<Type, ComponentDependencyEntry[]>
            _componentDependencyQueries =
                new();

        /// <inheritdoc />
        public void OnComponentAdd(IEntity entity, IComponent newComp)
        {
            SetDependencyForEntityComponents(entity, newComp.GetType(), newComp);
            InjectIntoComponent(entity, newComp);
        }

        /// <summary>
        /// Filling the dependencies of newComp by iterating over entity's components
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="newComp"></param>
        public void InjectIntoComponent(IEntity entity, IComponent newComp)
        {
            var queries = GetPointerQueries(newComp);

            if (queries.Length == 0)
            {
                return;
            }

            //get all present components in entity
            foreach (var entityComp in entity.GetAllComponents())
            {
                var entityCompReg = _componentFactory.GetRegistration(entityComp);
                foreach (var reference in entityCompReg.References)
                {
                    foreach (var entry in queries)
                    {
                        if (entry.Query == reference)
                        {
                            SetField(newComp, entry.FieldMemoryOffset, entityComp);
                            entry.OnAddMethod?.Invoke(newComp);
                        }
                    }
                }
            }
        }

        /// <inheritdoc />
        public void OnComponentRemove(IEntity entity, IComponent removedComp)
        {
            ClearRemovedComponentDependencies(removedComp);
            SetDependencyForEntityComponents(entity, removedComp.GetType(), null);
        }

        /// <summary>
        /// Updates all dependencies to type compType on the entity (in fields on components), to the value of comp
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="compType"></param>
        /// <param name="comp"></param>
        private void SetDependencyForEntityComponents(IEntity entity, Type compType, IComponent? comp)
        {
            var compReg = _componentFactory.GetRegistration(compType);

            //check if any are requesting our component as a dependency
            foreach (var entityComponent in entity.GetAllComponents())
            {
                //get entry for out entityComponent
                var queries = GetPointerQueries(entityComponent);

                //check if our new component is in entityComponents queries
                for (var i = 0; i < queries.Length; i++)
                {
                    //it is
                    if (compReg.References.Contains(queries[i].Query))
                    {
                        SetField(entityComponent, queries[i].FieldMemoryOffset, comp);
                        if (comp == null)
                        {
                            queries[i].OnRemoveMethod?.Invoke(entityComponent);
                        }
                        else
                        {
                            queries[i].OnAddMethod?.Invoke(entityComponent);
                        }
                    }
                }
            }
        }

        private void ClearRemovedComponentDependencies(IComponent comp)
        {
            var queries = GetPointerQueries(comp);
            foreach (var entry in queries)
            {
                entry.OnRemoveMethod?.Invoke(comp);
                SetField(comp, entry.FieldMemoryOffset, null);
            }
        }

        private void SetField(object o, int offset, object? value)
        {
            var asDummy = Unsafe.As<FieldOffsetDummy>(o);
            ref var @ref = ref Unsafe.Add(ref asDummy.A, offset);
            ref var oRef = ref Unsafe.As<byte, object?>(ref @ref);
            oRef = value;
        }

        private ComponentDependencyEntry[] GetPointerQueries(object obj)
        {
            return !_componentDependencyQueries.TryGetValue(obj.GetType(), out var value) ? CreateAndCachePointerOffsets(obj) : value;
        }

        private ComponentDependencyEntry[] CreateAndCachePointerOffsets(object obj)
        {
            var objType = obj.GetType();

            var attributeFields = objType.GetAllFields()
                .Where(f => Attribute.IsDefined(f, typeof(ComponentDependencyAttribute))).Select(field => (field, attribute: field.GetCustomAttribute<ComponentDependencyAttribute>())).ToArray();
            var attributeFieldsLength = attributeFields.Length;


            var queries = new ComponentDependencyEntry[attributeFieldsLength];

            for (var i = 0; i < attributeFields.Length; i++)
            {
                var field = attributeFields[i].field;
                if (field.FieldType.IsValueType)
                {
                    throw new ComponentDependencyValueTypeException(objType, field);
                }

                if (!NullableHelper.IsMarkedAsNullable(field))
                {
                    throw new ComponentDependencyNotNullableException(objType, field);
                }

                var offset = GetFieldOffset(objType, field);

                var attribute = attributeFields[i].attribute!;

                var methods = objType.GetRuntimeMethods().ToArray();

                MethodInfo getterMethod = typeof(ComponentDependencyManager).GetMethod("GetEventMethodDelegate",
                    BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(objType);;

                Action<object>? onAddMethod = null;
                if (attribute.OnAddMethodName != null)
                {
                    var tempMethod = GetEventMethod(methods, attribute.OnAddMethodName, getterMethod);

                    onAddMethod = tempMethod ?? throw new ComponentDependencyInvalidOnAddMethodNameException(field);
                }

                Action<object>? onRemoveMethod = null;
                if (attribute.OnRemoveMethodName != null)
                {
                    var tempMethod = GetEventMethod(methods, attribute.OnRemoveMethodName, getterMethod);

                    onRemoveMethod = tempMethod ?? throw new ComponentDependencyInvalidOnRemoveMethodNameException(field);
                }

                queries[i] = new ComponentDependencyEntry(field.FieldType, offset, onAddMethod, onRemoveMethod);
            }

            _componentDependencyQueries.Add(objType, queries);
            return queries;
        }

        private Action<object>? GetEventMethod(MethodInfo[] methods, string methodName, MethodInfo getterMethod)
        {
            var method = methods.FirstOrDefault(m => m.Name == methodName);
            if (method == null) return null;

            return (Action<object>?) getterMethod.Invoke(null, new object[]{method});
        }

        private static Action<object> GetEventMethodDelegate<T>(MethodInfo m)
        {
            var @delegate = (Action<T>) m.CreateDelegate(typeof(Action<T>));
            return o => @delegate((T) o);
        }

        private int GetFieldOffset(Type type, FieldInfo field)
        {
            var fieldOffsetField = typeof(FieldOffsetDummy).GetField("A")!;
            var dynamicMethod = new DynamicMethod(
                $"_fieldOffsetCalc<>{type}<>{field}",
                typeof(int),
                new[] {typeof(object)},
                type,
                true);
            dynamicMethod.DefineParameter(1, ParameterAttributes.In, "obj");
            var generator = dynamicMethod.GetILGenerator();

            //getting the field pointer
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldflda, field);

            //getting our "anchor"-pointer
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldflda, fieldOffsetField);

            //calculating offset
            generator.Emit(OpCodes.Sub);

            //return offset
            generator.Emit(OpCodes.Ret);

            var @delegate = (Func<object, int>)dynamicMethod.CreateDelegate(typeof(Func<object, int>));
            return @delegate(type);
        }

        private sealed class FieldOffsetDummy
        {
#pragma warning disable 649
            public byte A;
#pragma warning restore 649
        }

        private sealed class ComponentDependencyEntry
        {
            public readonly Type Query;
            public readonly int FieldMemoryOffset;
            public readonly Action<object>? OnAddMethod;
            public readonly Action<object>? OnRemoveMethod;

            public ComponentDependencyEntry(Type query, int fieldMemoryOffset, Action<object>? onAddMethod, Action<object>? onRemoveMethod)
            {
                Query = query;
                FieldMemoryOffset = fieldMemoryOffset;
                OnAddMethod = onAddMethod;
                OnRemoveMethod = onRemoveMethod;
            }
        }
    }

    public class ComponentDependencyValueTypeException : Exception
    {
        public readonly Type ComponentType;
        public readonly FieldInfo FieldInfo;

        public ComponentDependencyValueTypeException(Type componentType, FieldInfo fieldInfo) : base($"Field {fieldInfo} of Type {componentType} is marked as ComponentDependency but is a value Type")
        {
            ComponentType = componentType;
            FieldInfo = fieldInfo;
        }
    }

    public class ComponentDependencyNotNullableException : Exception
    {
        public readonly Type ComponentType;
        public readonly FieldInfo Field;

        public ComponentDependencyNotNullableException(Type componentType, FieldInfo field) : base($"Field {field} of Type {componentType} is marked as ComponentDependency, but does not have ?(Nullable)-Flag!")
        {
            ComponentType = componentType;
            Field = field;
        }
    }

    public abstract class ComponentDependencyInvalidMethodNameException : Exception
    {
        public readonly string MethodTarget;
        public readonly FieldInfo Field;

        protected ComponentDependencyInvalidMethodNameException(string methodTarget, FieldInfo field) : base($"{methodTarget}MethodName for {field} was invalid")
        {
            MethodTarget = methodTarget;
            Field = field;
        }
    }

    public class ComponentDependencyInvalidOnAddMethodNameException : ComponentDependencyInvalidMethodNameException
    {
        public ComponentDependencyInvalidOnAddMethodNameException([NotNull] FieldInfo field) : base("OnAdd", field)
        {}
    }

    public class ComponentDependencyInvalidOnRemoveMethodNameException : ComponentDependencyInvalidMethodNameException
    {
        public ComponentDependencyInvalidOnRemoveMethodNameException([NotNull] FieldInfo field) : base("OnRemove", field)
        {}
    }
}
