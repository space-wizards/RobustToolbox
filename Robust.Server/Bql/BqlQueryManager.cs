using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

namespace Robust.Server.Bql
{
    public partial class BqlQueryManager : IBqlQueryManager
    {
        private readonly IReflectionManager _reflectionManager;
        private readonly IComponentFactory _componentFactory;

        private readonly List<BqlQuerySelector> _instances = new();
        private readonly Dictionary<string, BqlQuerySelector> _queriesByToken = new();

        public BqlQueryManager()
        {
            _reflectionManager = IoCManager.Resolve<IReflectionManager>();
            _componentFactory = IoCManager.Resolve<IComponentFactory>();
        }

        /// <summary>
        /// Automatically registers all query selectors with the parser/executor.
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
        }
    }
}
