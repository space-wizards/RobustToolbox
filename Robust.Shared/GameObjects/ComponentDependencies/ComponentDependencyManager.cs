using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
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
        private readonly Dictionary<Type, (Type, int)[]> _componentDependencyQueries = new Dictionary<Type, (Type, int)[]>();

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
                    foreach (var (type, offset) in queries)
                    {
                        if (type == reference)
                        {
                            SetField(newComp, offset, entityComp);
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
                    if (compReg.References.Contains(queries[i].Item1))
                    {
                        SetField(entityComponent, queries[i].Item2, comp);
                    }
                }
            }
        }

        private void ClearRemovedComponentDependencies(IComponent comp)
        {
            var queries = GetPointerQueries(comp);
            foreach (var (_, offset) in queries)
            {
                SetField(comp, offset, null);
            }
        }

        private void SetField(object o, int offset, object? value)
        {
            var asDummy = Unsafe.As<FieldOffsetDummy>(o);
            ref var @ref = ref Unsafe.Add(ref asDummy.A, offset);
            ref var oRef = ref Unsafe.As<byte, object?>(ref @ref);
            oRef = value;
        }

        private (Type, int)[] GetPointerQueries(object obj)
        {
            return !_componentDependencyQueries.TryGetValue(obj.GetType(), out var value) ? CreateAndCachePointerOffsets(obj) : value;
        }

        private (Type, int)[] CreateAndCachePointerOffsets(object obj)
        {
            var fieldOffsetField = typeof(FieldOffsetDummy).GetField("A")!;
            var objType = obj.GetType();

            var attributeFields = objType.GetAllFields()
                .Where(f => Attribute.IsDefined(f, typeof(ComponentDependencyAttribute))).ToArray();
            var attributeFieldsLength = attributeFields.Length;

            var dynamicMethod = new DynamicMethod(
                $"_fieldOffsetCalc<>{objType}",
                typeof(int[]),
                new[] {typeof(object)},
                objType,
                true);

            dynamicMethod.DefineParameter(1, ParameterAttributes.In, "obj");

            var generator = dynamicMethod.GetILGenerator();

            var typeArray = new Type[attributeFieldsLength];

            //create the return array
            generator.Emit(OpCodes.Ldc_I4, attributeFieldsLength);
            generator.Emit(OpCodes.Newarr, typeof(int));

            int i = 0;
            foreach (var field in attributeFields)
            {
                if (!NullableHelper.IsMarkedAsNullable(field))
                {
                    throw new Exception($"Field {field} of Type {objType} is marked as ComponentDependecy, but does not have ?(Nullable)-Flag!");
                }

                typeArray[i] = field.FieldType;

                //duplicates the array ontop of the eval stack so stelem doesn't clear it
                generator.Emit(OpCodes.Dup);

                //setting the index for our arrayinsertion
                generator.Emit(OpCodes.Ldc_I4, i++);

                //getting the field pointer
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldflda, field);

                //getting our "anchor"-pointer
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldflda, fieldOffsetField);

                //calculating offset
                generator.Emit(OpCodes.Sub);

                //inserting the offset into the array
                generator.Emit(OpCodes.Stelem, typeof(int));
            }

            generator.Emit(OpCodes.Ret);

            var @delegate = (Func<object, int[]>)dynamicMethod.CreateDelegate(typeof(Func<object, int[]>));
            var unlabeledOffsets = @delegate(obj);

            var offsets = new (Type, int)[attributeFieldsLength];
            for (int j = 0; j < attributeFieldsLength; j++)
            {
                offsets[j] = (typeArray[j], unlabeledOffsets[j]);
            }

            _componentDependencyQueries.Add(objType, offsets);
            return offsets;
        }

        private sealed class FieldOffsetDummy
        {
#pragma warning disable 649
            public byte A;
#pragma warning restore 649
        }
    }
}
