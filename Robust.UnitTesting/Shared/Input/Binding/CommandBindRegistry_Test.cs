using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Robust.UnitTesting.Shared.Input.Binding
{
    [TestFixture, TestOf(typeof(CommandBindRegistry))]
    public sealed class CommandBindRegistry_Test : RobustUnitTest
    {

        private sealed class TypeA { }
        private sealed class TypeB { }
        private sealed class TypeC { }

        private sealed class TestInputCmdHandler : InputCmdHandler
        {
            // these vars only for tracking / debugging during testing
            private readonly string name;
            public readonly Type? ForType;

            public TestInputCmdHandler(Type? forType = null, string name = "")
            {
                ForType = forType;
                this.name = name;
            }

            public override string ToString()
            {
                return name;
            }

            public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
            {
                return false;
            }
        }

        [TestCase(1,1)]
        [TestCase(10,10)]
        public void ResolvesHandlers_WhenNoDependencies(int handlersPerType, int numFunctions)
        {
            var registry = new CommandBindRegistry(new DummySawmill());
            var allHandlers = new Dictionary<BoundKeyFunction,List<InputCmdHandler>>();
            for (int i = 0; i < numFunctions; i++)
            {
                var bkf = new BoundKeyFunction(i.ToString());
                var theseHandlers = new List<InputCmdHandler>();
                allHandlers[bkf] = theseHandlers;

                var aHandlers = new List<InputCmdHandler>();
                var bHandlers = new List<InputCmdHandler>();
                var cHandlers = new List<InputCmdHandler>();
                for (int j = 0; j < handlersPerType; j++)
                {
                    aHandlers.Add(new TestInputCmdHandler(typeof(TypeA)));
                    bHandlers.Add(new TestInputCmdHandler(typeof(TypeB)));
                    cHandlers.Add(new TestInputCmdHandler(typeof(TypeC)));
                }
                theseHandlers.AddRange(aHandlers);
                theseHandlers.AddRange(bHandlers);
                theseHandlers.AddRange(cHandlers);

                CommandBinds.Builder
                    .Bind(bkf, aHandlers)
                    .Register<TypeA>(registry);
                CommandBinds.Builder
                    .Bind(bkf, bHandlers)
                    .Register<TypeB>(registry);
                CommandBinds.Builder
                    .Bind(bkf, cHandlers)
                    .Register<TypeC>(registry);
            }


            //order doesn't matter, just verify that all handlers are returned
            foreach (var bkfToExpectedHandlers in allHandlers)
            {
                var bkf = bkfToExpectedHandlers.Key;
                var expectedHandlers = bkfToExpectedHandlers.Value;
                HashSet<InputCmdHandler> returnedHandlers = registry.GetHandlers(bkf).ToHashSet();

                Assert.That(expectedHandlers, Is.EqualTo(returnedHandlers).AsCollection);
            }

            // type b stuff should no longer fire
            CommandBinds.Unregister<TypeB>(registry);

            foreach (var bkfToExpectedHandlers in allHandlers)
            {
                var bkf = bkfToExpectedHandlers.Key;
                var expectedHandlers = bkfToExpectedHandlers.Value;
                expectedHandlers.RemoveAll(handler => ((TestInputCmdHandler) handler).ForType == typeof(TypeB));
                HashSet<InputCmdHandler> returnedHandlers = registry.GetHandlers(bkf).ToHashSet();
                Assert.That(expectedHandlers, Is.EqualTo(returnedHandlers).AsCollection);
            }
        }


        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void ResolvesHandlers_WithDependency(bool before, bool after)
        {
            var registry = new CommandBindRegistry(new DummySawmill());
            var bkf = new BoundKeyFunction("test");

            var aHandler1 = new TestInputCmdHandler( );
            var aHandler2 = new TestInputCmdHandler();
            var bHandler1 = new TestInputCmdHandler();
            var bHandler2 = new TestInputCmdHandler();
            var cHandler1 = new TestInputCmdHandler();
            var cHandler2 = new TestInputCmdHandler();

            // a handler 2 should run after both b and c handlers for all the below cases
            if (before && after)
            {
                CommandBinds.Builder
                    .Bind(bkf, aHandler1)
                    .BindAfter(bkf, aHandler2, typeof(TypeB), typeof(TypeC))
                    .Register<TypeA>(registry);
                CommandBinds.Builder
                    .BindBefore(bkf, bHandler1, typeof(TypeA))
                    .BindBefore(bkf, bHandler2, typeof(TypeA))
                    .Register<TypeB>(registry);
                CommandBinds.Builder
                    .BindBefore(bkf, cHandler1, typeof(TypeA))
                    .BindBefore(bkf, cHandler2, typeof(TypeA))
                    .Register<TypeC>(registry);
            }
            else if (before)
            {
                CommandBinds.Builder
                    .Bind(bkf, aHandler1)
                    .Bind(bkf, aHandler2)
                    .Register<TypeA>(registry);
                CommandBinds.Builder
                    .BindBefore(bkf, bHandler1, typeof(TypeA))
                    .BindBefore(bkf, bHandler2, typeof(TypeA))
                    .Register<TypeB>(registry);
                CommandBinds.Builder
                    .BindBefore(bkf, cHandler1, typeof(TypeA))
                    .BindBefore(bkf, cHandler2, typeof(TypeA))
                    .Register<TypeC>(registry);
            }
            else if (after)
            {
                CommandBinds.Builder
                    .Bind(bkf, aHandler1)
                    .BindAfter(bkf, aHandler2, typeof(TypeB), typeof(TypeC))
                    .Register<TypeA>(registry);
                CommandBinds.Builder
                    .Bind(bkf, bHandler1)
                    .Bind(bkf, bHandler2)
                    .Register<TypeB>(registry);
                CommandBinds.Builder
                    .Bind(bkf, cHandler1)
                    .Bind(bkf, cHandler2)
                    .Register<TypeC>(registry);
            }


            var returnedHandlers = registry.GetHandlers(bkf);

            // b1 , b2, c1, c2 should be fired before a2
            bool foundB1 = false, foundB2 = false, foundC1 = false, foundC2 = false;
            foreach (var returnedHandler in returnedHandlers)
            {
                if (returnedHandler == bHandler1)
                {
                    foundB1 = true;
                }
                else if (returnedHandler == bHandler2)
                {
                    foundB2 = true;
                }
                else if (returnedHandler == cHandler1)
                {
                    foundC1= true;
                }
                else if (returnedHandler == cHandler2)
                {
                    foundC2 = true;
                }
                else if (returnedHandler == aHandler2)
                {
                    Assert.That(foundB1 && foundB2 && foundC1 && foundC2, "bind registry didn't respect" +
                                                                          " handler dependency order");
                }
            }

            var expectedHandlers =
                new []{aHandler1, aHandler2, bHandler1, bHandler2, cHandler1, cHandler2};
            var returnedHandlerSet = new HashSet<InputCmdHandler>(returnedHandlers);
            foreach (var expectedHandler in expectedHandlers)
            {
                Assert.That(returnedHandlerSet.Contains(expectedHandler));
            }
        }

        [Test]
        public void ThrowsError_WhenCircularDependency()
        {
            var registry = new CommandBindRegistry(new DummySawmill());
            var bkf = new BoundKeyFunction("test");

            var aHandler1 = new TestInputCmdHandler();
            var aHandler2 = new TestInputCmdHandler();
            var bHandler1 = new TestInputCmdHandler();
            var bHandler2 = new TestInputCmdHandler();
            var cHandler1 = new TestInputCmdHandler();
            var cHandler2 = new TestInputCmdHandler();

            CommandBinds.Builder
                .Bind(bkf, aHandler1)
                .BindAfter(bkf, aHandler2, typeof(TypeB), typeof(TypeC))
                .Register<TypeA>(registry);
            CommandBinds.Builder
                .Bind(bkf, bHandler1)
                .Bind(bkf, bHandler2)
                .Register<TypeB>(registry);
            CommandBinds.Builder
                .Bind(bkf, cHandler1)
                .BindAfter(bkf, cHandler2, typeof(TypeA))
                .Register<TypeC>(registry);

            Assert.Throws<InvalidOperationException>(registry.RebuildGraph);
        }
    }
}
