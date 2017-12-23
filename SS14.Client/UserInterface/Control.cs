using SS14.Client.GodotGlue;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using SS14.Shared.Log;

namespace SS14.Client.UserInterface
{
    public partial class Control : IDisposable
    {
        /// <summary>
        ///     The name of this control.
        ///     Names must be unique between the control's siblings.
        /// </summary>
        // TODO: Allow changing the name at any point, probably.
        public string Name { get; }

        public Control Parent { get; private set; }

        /// <summary>
        ///     The UserInterfaceManager we belong to, for convenience.
        /// </summary>
        /// <returns></returns>
        public IUserInterfaceManager UserInterfaceManager { get; }

        /// <summary>
        ///     Gets an enumerable over all the children of this control.
        /// </summary>
        public IEnumerable<Control> Children => _children.Values;

        /// <summary>
        ///     The control's representation in Godot's scene tree.
        /// </summary>
        public Godot.Control SceneControl { get; private set; }

        public const float ANCHOR_BEGIN = 0;
        public const float ANCHOR_END = 1;

        public float AnchorBottom
        {
            get => SceneControl.AnchorBottom;
            set => SceneControl.AnchorBottom = value;
        }

        public float AnchorLeft
        {
            get => SceneControl.AnchorLeft;
            set => SceneControl.AnchorLeft = value;
        }

        public float AnchorRight
        {
            get => SceneControl.AnchorRight;
            set => SceneControl.AnchorRight = value;
        }

        public float AnchorTop
        {
            get => SceneControl.AnchorTop;
            set => SceneControl.AnchorTop = value;
        }

        public float MarginRight
        {
            get => SceneControl.MarginRight;
            set => SceneControl.MarginRight = value;
        }

        public float MarginLeft
        {
            get => SceneControl.MarginLeft;
            set => SceneControl.MarginLeft = value;
        }

        public float MarginTop
        {
            get => SceneControl.MarginTop;
            set => SceneControl.MarginTop = value;
        }

        public float MarginBottom
        {
            get => SceneControl.MarginBottom;
            set => SceneControl.MarginBottom = value;
        }

        private readonly Dictionary<string, Control> _children = new Dictionary<string, Control>();

        /// <summary>
        ///     Default constructor.
        ///     The name of the control is decided based on type.
        /// </summary>
        public Control()
        {
            UserInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();
            Name = GetType().Name;
            SetupSceneControl();
        }

        /// <param name="name">The name the component will have.</param>
        public Control(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name must not be null or whitespace.", nameof(name));
            }
            UserInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();
            Name = name;
            SetupSceneControl();
        }

        private void SetupSceneControl()
        {
            SceneControl = SpawnSceneControl();
            SceneControl.SetName(Name);

            SetupSignalHooks();
        }

        /// <summary>
        ///     Overriden by child classes to change the Godot control type.
        /// </summary>
        /// <returns></returns>
        protected virtual Godot.Control SpawnSceneControl()
        {
            return new Godot.Control();
        }


        public virtual void Dispose()
        {
            DisposeSignalHooks();

            DisposeAllChildren();
            Parent?.RemoveChild(this);

            SceneControl.QueueFree();
            SceneControl = null;
        }

        public void DisposeAllChildren()
        {
            // Cache because the children modify the dictionary.
            var children = new List<Control>(Children);
            foreach (var child in children)
            {
                child.Dispose();
            }
        }

        /// <summary>
        ///     Make the provided control a parent of this control.
        /// </summary>
        /// <param name="child">The control to make a child of this control.</param>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if we already have a component with the same name,
        ///     or the provided component is still parented to a different control.
        /// </exception>
        public virtual void AddChild(Control child)
        {
            if (_children.ContainsKey(child.Name))
            {
                throw new InvalidOperationException($"We already have a control with name {child.Name}!");
            }

            if (child.Parent != null)
            {
                throw new InvalidOperationException("This component is still parented. Deparent it before adding it.");
            }

            child.Parented(this);
            _children[child.Name] = child;
            SceneControl.AddChild(child.SceneControl);
        }

        /// <summary>
        ///     Called when this control gets made a child of a different control.
        /// </summary>
        /// <param name="newParent">The new parent component.</param>
        protected virtual void Parented(Control newParent)
        {
        }

        /// <summary>
        ///     Removes the provided child from this control.
        /// </summary>
        /// <param name="child">The child to remove.</param>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if the provided child is not one of this control's children.
        /// </exception>
        public virtual void RemoveChild(Control child)
        {
            if (!_children.ContainsKey(child.Name) || _children[child.Name] != child)
            {
                throw new InvalidOperationException("The provided control is not a direct child of this control.");
            }

            _children.Remove(child.Name);
            child.Parent = null;
            SceneControl.RemoveChild(child.SceneControl);
        }

        /// <summary>
        ///     Called when this control is removed as child from the former parent.
        /// </summary>
        protected virtual void Deparented()
        {
        }

        protected virtual void FocusEntered()
        {
        }

        protected virtual void FocusExited()
        {
        }
    }
}
