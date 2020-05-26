using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Players;

namespace Robust.UnitTesting.Shared.Input.Binding
{
    [TestFixture]
    public class CommandBindRegistry_Test
    {

        private class TypeA { }
        private class TypeB { }
        private class TypeC { }

        private class NullInputCmdHandler : InputCmdHandler
        {
            private readonly string name;

            public NullInputCmdHandler(string name = "")
            {
                this.name = name;
            }

            public override string ToString()
            {
                return name;
            }

            public override bool HandleCmdMessage(ICommonSession session, InputCmdMessage message)
            {
                return false;
            }
        }

        [TestCase(1,1)]
        [TestCase(10,10)]
        public void ResolvesHandlers_WhenNoDependencies(int handlersPerType, int numFunctions)
        {
            var registry = new CommandBindRegistry();
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
                    aHandlers.Add(new NullInputCmdHandler());
                    bHandlers.Add(new NullInputCmdHandler());
                    cHandlers.Add(new NullInputCmdHandler());
                }
                theseHandlers.AddRange(aHandlers);
                theseHandlers.AddRange(bHandlers);
                theseHandlers.AddRange(cHandlers);

                TypeBindings.Builder<TypeA>()
                    .Bind(bkf, aHandlers)
                    .Register(registry);
                TypeBindings.Builder<TypeB>()
                    .Bind(bkf, bHandlers)
                    .Register(registry);
                TypeBindings.Builder<TypeC>()
                    .Bind(bkf, cHandlers)
                    .Register(registry);
            }


            //order doesn't matter, just verify that all handlers are returned
            foreach (var bkfToExpectedHandlers in allHandlers)
            {
                var bkf = bkfToExpectedHandlers.Key;
                var expectedHandlers = bkfToExpectedHandlers.Value;
                HashSet<InputCmdHandler> returnedHandlers = registry.GetHandlers(bkf).ToHashSet();

                foreach (var expectedHandler in expectedHandlers)
                {
                    Assert.True(returnedHandlers.Contains(expectedHandler));
                }
            }
        }


        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void ResolvesHandlers_WithDependency(bool before, bool after)
        {
            var registry = new CommandBindRegistry();
            var bkf = new BoundKeyFunction("test");

            var aHandler1 = new NullInputCmdHandler("a1");
            var aHandler2 = new NullInputCmdHandler("a2");
            var bHandler1 = new NullInputCmdHandler("b1");
            var bHandler2 = new NullInputCmdHandler("b2");
            var cHandler1 = new NullInputCmdHandler("c1");
            var cHandler2 = new NullInputCmdHandler("c2");

            // a handler 2 should run after both b and c handlers for all the below cases
            if (before && after)
            {
                TypeBindings.Builder<TypeA>()
                    .Bind(bkf, aHandler1)
                    .BindAfter(bkf, aHandler2, typeof(TypeB), typeof(TypeC))
                    .Register(registry);
                TypeBindings.Builder<TypeB>()
                    .BindBefore(bkf, bHandler1, typeof(TypeA))
                    .BindBefore(bkf, bHandler2, typeof(TypeA))
                    .Register(registry);
                TypeBindings.Builder<TypeC>()
                    .BindBefore(bkf, cHandler1, typeof(TypeA))
                    .BindBefore(bkf, cHandler2, typeof(TypeA))
                    .Register(registry);
            }
            else if (before)
            {
                TypeBindings.Builder<TypeA>()
                    .Bind(bkf, aHandler1)
                    .Bind(bkf, aHandler2)
                    .Register(registry);
                TypeBindings.Builder<TypeB>()
                    .BindBefore(bkf, bHandler1, typeof(TypeA))
                    .BindBefore(bkf, bHandler2, typeof(TypeA))
                    .Register(registry);
                TypeBindings.Builder<TypeC>()
                    .BindBefore(bkf, cHandler1, typeof(TypeA))
                    .BindBefore(bkf, cHandler2, typeof(TypeA))
                    .Register(registry);
            }
            else if (after)
            {
                TypeBindings.Builder<TypeA>()
                    .Bind(bkf, aHandler1)
                    .BindAfter(bkf, aHandler2, typeof(TypeB), typeof(TypeC))
                    .Register(registry);
                TypeBindings.Builder<TypeB>()
                    .Bind(bkf, bHandler1)
                    .Bind(bkf, bHandler2)
                    .Register(registry);
                TypeBindings.Builder<TypeC>()
                    .Bind(bkf, cHandler1)
                    .Bind(bkf, cHandler2)
                    .Register(registry);
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
                    Assert.True(foundB1 && foundB2 && foundC1 && foundC2, "bind registry didn't respect" +
                                                                          " handler dependency order");
                }
            }

            var expectedHandlers =
                new []{aHandler1, aHandler2, bHandler1, bHandler2, cHandler1, cHandler2};
            var returnedHandlerSet = new HashSet<InputCmdHandler>(returnedHandlers);
            foreach (var expectedHandler in expectedHandlers)
            {
                Assert.True(returnedHandlerSet.Contains(expectedHandler));
            }
        }

        public void ThrowsError_WhenCircularDependency()
        {
            //TODO: implement
        }
    }
}
