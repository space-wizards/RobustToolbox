using System;
using System.Collections.Generic;
using System.Diagnostics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

namespace Robust.Server.Bql
{
    public partial class BqlQueryManager
    {
        private static IReflectionManager _reflectionManager = default!;
        private static IComponentFactory _componentFactory = default!;

        private readonly List<BqlQuerySelector> _instances = new();
        private readonly Dictionary<string, BqlQuerySelector> _queriesByToken = new();
        private readonly Dictionary<Type, BqlQuerySelector> _queriesByType = new();


        public BqlQueryManager()
        {
            _reflectionManager = IoCManager.Resolve<IReflectionManager>();
            _componentFactory = IoCManager.Resolve<IComponentFactory>();
        }

        /// <summary>
        /// Automatically registers all quer
        /// </summary>
        public void DoAutoRegistrations()
        {
            foreach (var type in _reflectionManager.FindTypesWithAttribute<RegisterBqlQuerySelectorAttribute>())
            {
                RegisterClass(type);
            }

            DoParserSetup();
        }

        /// <summary>
        /// Internally registers the given <see cref="BqlQuerySelector"/>.
        /// </summary>
        /// <param name="bqlQuerySelector">The selector to register</param>
        private void RegisterClass(Type bqlQuerySelector)
        {
            DebugTools.Assert(bqlQuerySelector.BaseType == typeof(BqlQuerySelector));
            var inst = (BqlQuerySelector)Activator.CreateInstance(bqlQuerySelector)!;
            _instances.Add(inst);
            _queriesByToken.Add(inst.Token, inst);
            _queriesByType.Add(bqlQuerySelector, inst);
        }

        public void DoParserSetup()
        {
            throw new NotImplementedException();
        }
    }
}
