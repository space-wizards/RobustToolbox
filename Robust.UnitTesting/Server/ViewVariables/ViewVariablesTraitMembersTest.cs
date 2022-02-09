using System;
using Moq;
using NUnit.Framework;
using Robust.Server.ViewVariables;
using Robust.Server.ViewVariables.Traits;
using Robust.Shared.Analyzers;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.UnitTesting.Server.ViewVariables
{
    [Parallelizable]
    [TestFixture]
    internal sealed class ViewVariablesTraitMembersTest
    {
        [Test]
        public void Test()
        {
            var ser = new Mock<IRobustSerializer>();
            ser.Setup(p => p.CanSerialize(It.IsAny<Type>())).Returns(true);

            var session = new Mock<IViewVariablesSession>();
            session.SetupGet(p => p.Object).Returns(new C());
            session.SetupGet(p => p.ObjectType).Returns(typeof(C));
            session.SetupGet(p => p.RobustSerializer).Returns(ser.Object);

            var blob = new ViewVariablesTraitMembers(session.Object).DataRequest(new ViewVariablesRequestMembers());
            Assert.That(blob, Is.TypeOf<ViewVariablesBlobMembers>());

            var blobM = (ViewVariablesBlobMembers) blob!;
            Assert.That(blobM.MemberGroups, Has.Count.EqualTo(2));

            var group0 = blobM.MemberGroups[0];
            var group1 = blobM.MemberGroups[1];

            Assert.That(group0.groupName, Does.EndWith("+C"));
            Assert.That(group1.groupName, Does.EndWith("+A"));

            Assert.That(group0.groupMembers, Has.Count.EqualTo(2));
            Assert.That(group1.groupMembers, Has.Count.EqualTo(1));

            Assert.That(group0.groupMembers[0].Name, Is.EqualTo("Y"));
            Assert.That(group0.groupMembers[1].Name, Is.EqualTo("Z"));

            Assert.That(group1.groupMembers[0].Name, Is.EqualTo("X"));
        }

#pragma warning disable 649
        [Virtual]
        private class A
        {
            [ViewVariables] public int X;
        }

        [Virtual]
        private class B : A
        {
            public int Hidden { get; set; }
        }

        private sealed class C : B
        {
            [ViewVariables] public int Y { get; set; }
            [ViewVariables] public string? Z { get; set; }
        }
#pragma warning restore 649
    }
}
