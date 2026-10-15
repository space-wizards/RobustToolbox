using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Robust.Client.Animations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Themes;
using Robust.Client.UserInterface.XAML.Proxy;
using Robust.Shared.Animations;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Client.UserInterface
{
    [TestFixture]
    [TestOf(typeof(Control))]
    public sealed class ControlTest : RobustUnitTest
    {
        private IUserInterfaceManagerInternal _userInterfaceManager = default!;
        private IDynamicTypeFactoryInternal _typeFactory = default!;

        private static readonly AttachedProperty _refTypeAttachedProperty
            = AttachedProperty.Create("_refType", typeof(ControlTest), typeof(string), "foo", v => (string?) v != "bar");

        private static readonly AttachedProperty _valueTypeAttachedProperty
            = AttachedProperty.Create("_valueType", typeof(ControlTest), typeof(float));

        private static readonly AttachedProperty _nullableAttachedProperty
            = AttachedProperty.Create("_nullable", typeof(ControlTest), typeof(float?));

        private static readonly AttachedProperty<int> _genericProperty =
            AttachedProperty<int>.Create("generic", typeof(ControlTest), 5, i => i % 2 == 1);

        public override UnitTestProject Project => UnitTestProject.Client;

        protected override void OverrideIoC()
        {
            base.OverrideIoC();

            IoCManager.Register<IXamlProxyManager, XamlProxyManagerStub>(overwrite: true);
        }

        [OneTimeSetUp]
        public void Setup()
        {
            var resources = IoCManager.Resolve<IResourceManagerInternal>();
            resources.Initialize(null, false);
            resources.MountContentDirectory(Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..",
                "..",
                "Resources")));

            IoCManager.Resolve<ISerializationManager>().Initialize();

            var prototypes = IoCManager.Resolve<IPrototypeManager>();
            prototypes.Initialize();
            prototypes.LoadDirectory(new ResPath("/EnginePrototypes/Shaders"));
            prototypes.LoadDirectory(new ResPath("/EnginePrototypes/UserInterface"));
            prototypes.ResolveResults();

            _userInterfaceManager = IoCManager.Resolve<IUserInterfaceManagerInternal>();
            _userInterfaceManager.InitializeTesting();
            _userInterfaceManager.SetDefaultTheme(UITheme.DefaultName);
            _typeFactory = IoCManager.Resolve<IDynamicTypeFactoryInternal>();
        }

        private static IEnumerable<TestCaseData> ControlTypes()
        {
            var baseType = typeof(Control);

            yield return new TestCaseData(baseType);

            foreach (var type in baseType.Assembly.GetTypes()
                         .Where(type => type != baseType
                                        && type.IsAssignableTo(baseType)
                                        && !type.IsAbstract
                                        && !type.ContainsGenericParameters
                                        && type.GetConstructor(Type.EmptyTypes) != null)
                         .OrderBy(type => type.FullName))
            {
                yield return new TestCaseData(type);
            }
        }

        [TestCaseSource(nameof(ControlTypes))]
        public void TestTreeLifecycle(Type controlType)
        {
            var root = _userInterfaceManager.RootControl;
            var initialTreeSize = GetSelfAndDescendants(root).Count();
            var control = _typeFactory.CreateInstanceUnchecked<Control>(controlType);

            try
            {
                for (var i = 0; i < 2; i++)
                {
                    Assert.That(control.Parent, Is.Null);
                    AssertTreeRoot(control, null);

                    root.AddChild(control);
                    Assert.That(control.Parent, Is.SameAs(root));

                    var attachedControls = GetSelfAndDescendants(control).ToArray();
                    foreach (var attachedControl in attachedControls)
                    {
                        Assert.That(attachedControl.Root, Is.SameAs(root), attachedControl.GetType().FullName);
                        Assert.That(attachedControl.IsInsideTree, Is.True, attachedControl.GetType().FullName);
                    }

                    control.Orphan();
                    Assert.That(control.Parent, Is.Null);

                    foreach (var attachedControl in attachedControls)
                    {
                        Assert.That(attachedControl.Root, Is.Null, attachedControl.GetType().FullName);
                        Assert.That(attachedControl.IsInsideTree, Is.False, attachedControl.GetType().FullName);
                    }

                    Assert.That(GetSelfAndDescendants(root).Count(), Is.EqualTo(initialTreeSize));
                }
            }
            finally
            {
                control.Orphan();
            }
        }

        [Test]
        public void TestTreeLifecycleCallbacks()
        {
            var root = _userInterfaceManager.RootControl;
            var control = new LifecycleControl();

            root.AddChild(control);
            Assert.That(control.EnteredRoots, Is.EqualTo(new[] { root }));
            Assert.That(control.ExitedRoots, Is.Empty);

            control.Orphan();
            Assert.That(control.EnteredRoots, Is.EqualTo(new[] { root }));
            Assert.That(control.ExitedRoots, Is.EqualTo(new UIRoot?[] { null }));

            root.AddChild(control);
            control.Orphan();

            Assert.That(control.EnteredRoots, Is.EqualTo(new[] { root, root }));
            Assert.That(control.ExitedRoots, Is.EqualTo(new UIRoot?[] { null, null }));
        }

        private static void AssertTreeRoot(Control control, UIRoot? root)
        {
            foreach (var child in GetSelfAndDescendants(control))
            {
                Assert.That(child.Root, Is.SameAs(root), child.GetType().FullName);
                Assert.That(child.IsInsideTree, Is.EqualTo(root != null), child.GetType().FullName);
            }
        }

        private static IEnumerable<Control> GetSelfAndDescendants(Control control)
        {
            yield return control;

            foreach (var child in control.Children)
            {
                foreach (var descendant in GetSelfAndDescendants(child))
                {
                    yield return descendant;
                }
            }
        }

        /// <summary>
        ///     Test that you can't parent a control to its (grand)child.
        /// </summary>
        [Test]
        public void TestNoRecursion()
        {
            var control1 = new Control();
            var control2 = new Control();
            var control3 = new Control();

            control1.AddChild(control2);
            // Test direct parent/child.
            Assert.That(() => control2.AddChild(control1), Throws.ArgumentException);

            control2.AddChild(control3);
            // Test grand child.
            Assert.That(() => control3.AddChild(control1), Throws.ArgumentException);
        }

        [Test]
        public void TestVisibleInTree()
        {
            var control1 = new Control();

            // Not visible because not parented to root control.
            Assert.That(control1.Visible, Is.True);
            Assert.That(control1.VisibleInTree, Is.False);

            control1.UserInterfaceManager.RootControl.AddChild(control1);
            Assert.That(control1.Visible, Is.True);
            Assert.That(control1.VisibleInTree, Is.True);

            control1.Visible = false;
            Assert.That(control1.Visible, Is.False);
            Assert.That(control1.VisibleInTree, Is.False);
            control1.Visible = true;

            var control2 = new Control();
            Assert.That(control2.VisibleInTree, Is.False);

            control1.AddChild(control2);
            Assert.That(control2.VisibleInTree, Is.True);

            control1.Visible = false;
            Assert.That(control2.VisibleInTree, Is.False);

            control2.Visible = false;
            Assert.That(control2.VisibleInTree, Is.False);

            control1.Visible = true;
            Assert.That(control2.VisibleInTree, Is.False);

            control1.Orphan();
        }

        [Test]
        public void TestAttachedPropertiesBasic()
        {
            var control = new Control();

            control.SetValue(_refTypeAttachedProperty, "honk");

            Assert.That(control.GetValue(_refTypeAttachedProperty), Is.EqualTo("honk"));
        }

        [Test]
        public void TestAttachedPropertiesValidate()
        {
            var control = new Control();

            Assert.Throws<ArgumentException>(() => control.SetValue(_refTypeAttachedProperty, "bar"));
        }

        [Test]
        public void TestAttachedPropertiesInvalidType()
        {
            var control = new Control();

            Assert.Throws<ArgumentException>(() => control.SetValue(_refTypeAttachedProperty, new object()));
            Assert.Throws<ArgumentException>(() => control.SetValue(_valueTypeAttachedProperty, new object()));
        }

        [Test]
        public void TestAttachedPropertiesInvalidNull()
        {
            var control = new Control();

            Assert.Throws<ArgumentNullException>(() => control.SetValue(_valueTypeAttachedProperty, null));
        }

        [Test]
        public void TestAttachedPropertiesValidNull()
        {
            var control = new Control();

            control.SetValue(_nullableAttachedProperty, null);
        }

        [Test]
        public void TestAttachedPropertiesGeneric()
        {
            var control = new Control();

            Assert.That(control.GetValue(_genericProperty), Is.EqualTo(5));

            control.SetValue(_genericProperty, 11);

            Assert.That(control.GetValue(_genericProperty), Is.EqualTo(11));

            Assert.That(() => control.SetValue(_genericProperty, 10), Throws.ArgumentException);
        }

        [Test]
        public void TestAnimations()
        {
            var control = new TestControl();
            var animation = new Animation
            {
                Length = TimeSpan.FromSeconds(3),
                AnimationTracks =
                {
                    new AnimationTrackControlProperty
                    {
                        Property = nameof(TestControl.Foo),
                        KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame(1f, 1f),
                            new AnimationTrackProperty.KeyFrame(3f, 2f)
                        }
                    }
                }
            };

            control.PlayAnimation(animation, "foo");
            control.DoFrameUpdateRecursive(new FrameEventArgs(0.5f));

            Assert.That(control.Foo, new ApproxEqualityConstraint(0f)); // Should still be 0.

            control.DoFrameUpdateRecursive(new FrameEventArgs(0.5001f));

            Assert.That(control.Foo, new ApproxEqualityConstraint(1f, 0.01)); // Should now be 1.

            control.DoFrameUpdateRecursive(new FrameEventArgs(0.5f));

            Assert.That(control.Foo, new ApproxEqualityConstraint(1.5f, 0.01)); // Should now be 1.5.

            control.DoFrameUpdateRecursive(new FrameEventArgs(1.0f));

            Assert.That(control.Foo, new ApproxEqualityConstraint(2.5f, 0.01)); // Should now be 2.5.

            control.DoFrameUpdateRecursive(new FrameEventArgs(0.5f));

            Assert.That(control.Foo, new ApproxEqualityConstraint(3f, 0.01)); // Should now be 3.

            control.DoFrameUpdateRecursive(new FrameEventArgs(0.5f));

            Assert.That(control.Foo, new ApproxEqualityConstraint(3f, 0.01)); // Should STILL be 3.
        }

        private sealed class TestControl : Control
        {
            [Animatable] public float Foo { get; set; }
        }

        private sealed class LifecycleControl : Control
        {
            public List<UIRoot?> EnteredRoots { get; } = new();
            public List<UIRoot?> ExitedRoots { get; } = new();

            protected override void EnteredTree()
            {
                base.EnteredTree();
                EnteredRoots.Add(Root);
            }

            protected override void ExitedTree()
            {
                base.ExitedTree();
                ExitedRoots.Add(Root);
            }
        }
    }
}
