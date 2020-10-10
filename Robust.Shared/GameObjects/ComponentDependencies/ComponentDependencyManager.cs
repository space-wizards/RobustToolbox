using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects.ComponentDependencies
{
    public class ComponentDependencyManager : IComponentDependencyManager
    {
        private static readonly Type[] InjectorParameters = {typeof(IComponent), typeof(IComponent?[])};
        private delegate void InjectorDelegate(IComponent target, IComponent?[] components);

        private static readonly Type[] RetrieverParameters = {typeof(IComponent)};
        private delegate object?[] RetrieverDelegate(IComponent target);

        /// <summary>
        /// Cache of Dynamic methods to inject component-dependencies into Type
        /// </summary>
        private readonly Dictionary<Type, (InjectorDelegate, Type[])> _componentInjectors = new Dictionary<Type, (InjectorDelegate, Type[])>(); //todo null entries for types that done have anything

        /// <summary>
        /// Cache of Dynamic methods to retreive all added components-dependencies from a component
        /// </summary>
        private readonly Dictionary<Type, RetrieverDelegate> _componentRetrievers = new Dictionary<Type, RetrieverDelegate>(); //todo null entries for types that done have anything

        /// <inheritdoc />
        public void OnComponentAdd(IEntity entity, IComponent newComp)
        {
            SetDependencyForEntityComponents(entity, newComp.GetType(), newComp);
            InjectIntoComponent(entity, newComp);
        }

        public void InjectIntoComponent(IEntity entity, IComponent newComp)
        {
            var compType = newComp.GetType();

            //get all present components in entity
            Dictionary<Type, IComponent> entityComponents = entity.GetAllComponents().ToDictionary(comp => comp.GetType());

            //get entry
            var (injectorDelegate, query) = GetInjector(compType);

            if (query.Length == 0)
            {
                return;
            }

            var componentsToInject = new IComponent?[query.Length];
            for (int i = 0; i < componentsToInject.Length; i++)
            {
                if (entityComponents.TryGetValue(query[i], out var value))
                {
                    componentsToInject[i] = value;
                }
            }

            injectorDelegate(newComp, componentsToInject);
        }

        /// <inheritdoc />
        public void OnComponentRemove(IEntity entity, IComponent removedComp)
        {
            ClearRemovedComponentDependencies(removedComp);
            SetDependencyForEntityComponents(entity, removedComp.GetType(), null);
        }

        private void SetDependencyForEntityComponents(IEntity entity, Type compType, IComponent? comp)
        {
            //get all present component in entity
            var entityComponents = entity.GetAllComponents();

            //check if any are requesting our component as a dependency
            foreach (var entityComponent in entityComponents)
            {
                var entityCompType = entityComponent.GetType();

                //get entry for out entityComponent
                var (injectorDelegate, query) = GetInjector(entityCompType);

                //check if our new component is in entityComponents queries
                int fieldIndex = -1;
                for (int i = 0; i < query.Length; i++)
                {
                    if (query[i] == compType)
                    {
                        fieldIndex = i;
                        break;
                    }
                }
                if (fieldIndex == -1) continue;

                //it is, so we first retreive all values, change the corresponding one and inject

                var retrieverDelegate = GetRetriever(entityCompType);

                //getting all current values
                IComponent?[] currentValues = (IComponent?[]) retrieverDelegate(entityComponent);

                currentValues[fieldIndex] = comp;

                injectorDelegate!(entityComponent, currentValues);
            }
        }

        public void ClearRemovedComponentDependencies(IComponent comp)
        {
            var (injectionDelegate, queries) = GetInjector(comp.GetType());
            injectionDelegate(comp, new IComponent?[queries.Length]); //just clear all values
        }

        private (InjectorDelegate, Type[]) GetInjector(Type type)
        {
            if (!_componentInjectors.TryGetValue(type, out var entry))
            {
                entry = CreateAndCacheInjector(type);
            }
            return entry;
        }

        private (InjectorDelegate, Type[]) CreateAndCacheInjector(Type type)
        {
            var dynamicMethod = new DynamicMethod($"_component_injector<>{type}", null, InjectorParameters, type, true);

            dynamicMethod.DefineParameter(1, ParameterAttributes.In, "component");
            dynamicMethod.DefineParameter(2, ParameterAttributes.In, "components");

            var generator = dynamicMethod.GetILGenerator();
            var componentTypes = new List<Type>();

            var i = 0;
            foreach (var field in type.GetAllFields())
            {
                if (!Attribute.IsDefined(field, typeof(ComponentDependencyAttribute)))
                {
                    continue;
                }

                if (!NullableHelper.IsMarkedAsNullable(field))
                {
                    throw new Exception($"Field {field} of Type {type} is marked as ComponentDependecy, but does not have ?(Nullable)-Flag!");
                }

                if (!field.FieldType.IsSubclassOf(typeof(Component)))
                {
                    throw new Exception($"Field {field} of Type {type} is marked as ComponentDependency, even though its type isn't a subclass of Component!");
                }

                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldarg_1);

                generator.Emit(OpCodes.Ldc_I4, i++);
                generator.Emit(OpCodes.Ldelem_Ref);

                generator.Emit(OpCodes.Stfld, field);
                componentTypes.Add(field.FieldType);
            }

            generator.Emit(OpCodes.Ret);

            var @delegate = (InjectorDelegate)dynamicMethod.CreateDelegate(typeof(InjectorDelegate));
            var entry = (@delegate, componentTypes.ToArray());
            _componentInjectors.Add(type, entry);
            return entry;
        }

        private RetrieverDelegate GetRetriever(Type type)
        {
            if (!_componentRetrievers.TryGetValue(type, out var retrieverDelegate))
            {
                retrieverDelegate = CreateAndCacheRetrieverDelegate(type);
            }
            return retrieverDelegate;
        }

        private RetrieverDelegate CreateAndCacheRetrieverDelegate(Type type)
        {
            var dynamicMethod = new DynamicMethod($"_component_retreiver<>{type}", typeof(object[]), RetrieverParameters, type, true);

            dynamicMethod.DefineParameter(1, ParameterAttributes.In, "component");

            var attributeFields = type.GetAllFields().Where(f => Attribute.IsDefined(f, typeof(ComponentDependencyAttribute))).ToArray();
            var generator = dynamicMethod.GetILGenerator();

            //create the return array
            generator.Emit(OpCodes.Ldc_I4, attributeFields.Length);
            generator.Emit(OpCodes.Newarr, typeof(Component));

            int i = 0;
            foreach (var field in attributeFields)
            {
                if (!NullableHelper.IsMarkedAsNullable(field))
                {
                    throw new Exception($"Field {field} of Type {type} is marked as ComponentDependecy, but does not have ?(Nullable)-Flag!");
                }

                //duplicates the array ontop of the eval stack so stelem doesn't clear it
                generator.Emit(OpCodes.Dup);

                //setting the index for our arrayinsertion
                generator.Emit(OpCodes.Ldc_I4, i++);

                //getting the field value
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, field);

                //inserting the field value into the array
                generator.Emit(OpCodes.Stelem, typeof(Component));
            }

            generator.Emit(OpCodes.Ret);

            var @delegate = (RetrieverDelegate)dynamicMethod.CreateDelegate(typeof(RetrieverDelegate));

            _componentRetrievers.Add(type, @delegate);
            return @delegate;
        }
    }
}
