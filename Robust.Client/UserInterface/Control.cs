using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Animations;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface
{
    /// <summary>
    ///     A node in the GUI system.
    ///     See https://hackmd.io/@ss14/ui-system-tutorial for some basic concepts.
    /// </summary>
    [PublicAPI]
    public partial class Control : IDisposable
    {
        private readonly List<Control> _orderedChildren = new();

        private bool _visible = true;

        // _marginSetSize is the size calculated by the margins,
        // but it's different from _size if min size is higher.

        private bool _canKeyboardFocus;

        public event Action<Control>? OnVisibilityChanged;

        /// <summary>
        ///     The name of this control.
        ///     Names must be unique between the siblings of the control.
        /// </summary>
        [ViewVariables]
        public string? Name { get; set; }

        /// <summary>
        ///     Our parent inside the control tree.
        /// </summary>
        /// <remarks>
        ///     This cannot be changed directly. Use <see cref="AddChild" /> and such on the parent to change it.
        /// </remarks>
        [ViewVariables]
        public Control? Parent { get; private set; }

        public NameScope? NameScope;

        //public void AttachNameScope(Dictionary<string, Control> nameScope)
        //{
        //    _nameScope = nameScope;
        //}

        public NameScope? FindNameScope()
        {
            foreach (var control in this.GetSelfAndLogicalAncestors())
            {
                if (control.NameScope != null) return control.NameScope;
            }

            return null;
        }

        public T FindControl<T>(string name) where T : Control
        {
            var nameScope = FindNameScope();
            if (nameScope == null)
            {
                throw new ArgumentException("No Namespace found for Control");
            }

            var value = nameScope.Find(name);
            if (value == null)
            {
                throw new ArgumentException($"No Control with the name {name} found");
            }

            if (value is not T ret)
            {
                throw new ArgumentException($"Control with name {name} had invalid type {value.GetType()}");
            }

            return ret;
        }

        internal IUserInterfaceManagerInternal UserInterfaceManagerInternal { get; }

        /// <summary>
        ///     The UserInterfaceManager we belong to, for convenience.
        /// </summary>
        public IUserInterfaceManager UserInterfaceManager => UserInterfaceManagerInternal;

        /// <summary>
        ///     Gets an ordered enumerable over all the children of this control.
        /// </summary>
        [ViewVariables]
        public OrderedChildCollection Children { get; }

        [Content]
        public virtual ICollection<Control> XamlChildren { get; protected set; }

        [ViewVariables] public int ChildCount => _orderedChildren.Count;

        /// <summary>
        ///     Gets whether this control is at all visible.
        ///     This means the control is part of the tree of the root control, and all of its parents are visible.
        /// </summary>
        /// <seealso cref="Visible"/>
        [ViewVariables]
        public bool VisibleInTree
        {
            get
            {
                for (var parent = this; parent != null; parent = parent.Parent)
                {
                    if (!parent.Visible)
                    {
                        return false;
                    }

                    if (parent == UserInterfaceManager.RootControl)
                    {
                        return true;
                    }
                }

                return false;
            }
        }


        /// <summary>
        ///     Whether or not this control and its children are visible.
        /// </summary>
        /// <seealso cref="VisibleInTree"/>
        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable]
        public bool Visible
        {
            get => _visible;
            set
            {
                if (_visible == value)
                {
                    return;
                }

                _visible = value;

                _propagateVisibilityChanged(value);
                // TODO: unhardcode this.
                // Many containers ignore children if they're invisible, so that's why we're replicating that ehre.
                Parent?.MinimumSizeChanged();
            }
        }

        private void _propagateVisibilityChanged(bool newVisible)
        {
            OnVisibilityChanged?.Invoke(this);
            if (!VisibleInTree)
            {
                UserInterfaceManagerInternal.ControlHidden(this);
            }

            foreach (var child in _orderedChildren)
            {
                if (newVisible || child._visible)
                {
                    child._propagateVisibilityChanged(newVisible);
                }
            }
        }

        /// <summary>
        ///     Whether or not this control is an (possibly indirect) child of
        ///     <see cref="IUserInterfaceManager.RootControl"/>
        /// </summary>
        [ViewVariables]
        public bool IsInsideTree { get; internal set; }

        private void _propagateExitTree()
        {
            IsInsideTree = false;
            _exitedTree();

            foreach (var child in _orderedChildren)
            {
                child._propagateExitTree();
            }
        }

        /// <summary>
        ///     Called when the control is removed from the root control tree.
        /// </summary>
        /// <seealso cref="EnteredTree"/>
        protected virtual void ExitedTree()
        {
        }

        private void _exitedTree()
        {
            ExitedTree();
            UserInterfaceManagerInternal.ControlRemovedFromTree(this);
        }

        private void _propagateEnterTree()
        {
            IsInsideTree = true;
            _enteredTree();

            foreach (var child in _orderedChildren)
            {
                child._propagateEnterTree();
            }
        }

        /// <summary>
        ///     Called when the control enters the root control tree.
        /// </summary>
        /// <seealso cref="ExitedTree"/>
        protected virtual void EnteredTree()
        {
        }

        private void _enteredTree()
        {
            EnteredTree();
        }


        /// <summary>
        /// Simple text tooltip that is shown when the mouse is hovered over this control for a bit.
        /// See <see cref="TooltipSupplier"/> or <see cref="OnShowTooltip"/> for a more customizable alternative.
        /// No effect when TooltipSupplier is specified.
        /// </summary>
        /// <remarks>
        /// If empty or null, no tooltip is shown in the first place (but OnShowTooltip and OnHideTooltip
        /// events are still fired).
        /// </remarks>
        public string? ToolTip { get; set; }

        /// <summary>
        /// Overrides the global tooltip delay, showing the tooltip for this
        /// control within the specified number of seconds.
        /// </summary>
        public float? TooltipDelay { get; set; }

        /// <summary>
        /// When a tooltip should be shown for this control, this will be invoked to
        /// produce a control which will serve as the tooltip (doing nothing if null is returned).
        /// This is the generally recommended way to implement custom tooltips for controls, as it takes
        /// care of the various edge cases for showing / hiding the tooltip.
        /// For an even more customizable approach, <see cref="OnShowTooltip"/>
        ///
        /// The returned control will be added to PopupRoot, and positioned
        /// within the user interface under the current mouse position to avoid going off the edge of the
        /// screen. When the tooltip should be hidden, the control will be hidden by removing it from the tree.
        ///
        /// It is expected that the returned control remains within PopupRoot. Other classes should
        /// not move it around in the tree or move it out of PopupRoot, but may access and modify
        /// the control and its children via <see cref="SuppliedTooltip"/>.
        /// </summary>
        /// <remarks>
        /// Returning a new instance of a tooltip control every time is usually fine. If for some
        /// reason constructing the tooltip control is expensive, it MAY be fine to cache + reuse a single instance but this
        /// approach has not yet been tested.
        /// </remarks>
        public TooltipSupplier? TooltipSupplier { get; set; }

        /// <summary>
        /// Invoked when the mouse is hovered over this control for a bit and a tooltip
        /// should be shown. Can be used as an alternative to ToolTip or TooltipSupplier to perform custom tooltip
        /// logic such as showing a more complex tooltip control.
        ///
        /// Any custom tooltip controls should typically be added
        /// as a child of UserInterfaceManager.PopupRoot
        /// Handlers can use <see cref="Tooltips.PositionTooltip(Control)"/> to assist with positioning
        /// custom tooltip controls.
        /// </summary>
        public event EventHandler? OnShowTooltip;



        /// <summary>
        /// If this control is currently showing a tooltip provided via TooltipSupplier,
        /// returns that tooltip. Do not move this control within the tree, it should remain in PopupRoot.
        /// Also, as it may be hidden (removed from tree) at any time, saving a reference to this is a Bad Idea.
        /// </summary>
        public Control? SuppliedTooltip => UserInterfaceManagerInternal.GetSuppliedTooltipFor(this);

        /// <summary>
        /// Manually hide the tooltip currently being shown for this control, if there is one.
        /// </summary>
        public void HideTooltip()
        {
            UserInterfaceManagerInternal.HideTooltipFor(this);
        }

        internal void PerformShowTooltip()
        {
            OnShowTooltip?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Invoked when this control is showing a tooltip which should now be hidden.
        /// </summary>
        public event EventHandler? OnHideTooltip;

        internal void PerformHideTooltip()
        {
            OnHideTooltip?.Invoke(this, EventArgs.Empty);
        }


        /// <summary>
        ///     The mode that controls how mouse filtering works. See the enum for how it functions.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public MouseFilterMode MouseFilter { get; set; } = MouseFilterMode.Ignore;

        /// <summary>
        ///     Whether this control can take keyboard focus.
        ///     Keyboard focus is necessary for the control to receive keyboard events.
        /// </summary>
        /// <seealso cref="KeyboardFocusOnClick"/>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanKeyboardFocus
        {
            get => _canKeyboardFocus;
            set
            {
                if (_canKeyboardFocus == value)
                {
                    return;
                }

                _canKeyboardFocus = value;

                if (!value)
                {
                    ReleaseKeyboardFocus();
                }
            }
        }

        /// <summary>
        ///     Whether the control will automatically receive keyboard focus (if possible) when clicked on.
        /// </summary>
        /// <remarks>
        ///     Obviously, <see cref="CanKeyboardFocus"/> must be set to true for this to work.
        /// </remarks>
        public bool KeyboardFocusOnClick { get; set; }

        /// <summary>
        ///     Whether to clip drawing of this control and its children to its rectangle.
        /// </summary>
        /// <remarks>
        ///     By default, controls (and their children) can render outside their rectangle.
        ///     If this is set, rendering is hard clipped to it.
        /// </remarks>
        /// <seealso cref="RectDrawClipMargin"/>
        [ViewVariables]
        public bool RectClipContent { get; set; }

        /// <summary>
        ///     A margin around this control. If this control + this margin is outside its parent's <see cref="RectClipContent" />,
        ///     it will not be drawn.
        /// </summary>
        /// <remarks>
        ///     A control rectangle does not necessarily have to be listened to for drawing.
        ///     So the problem is, how do we know where to stop trying to draw the control if it's clipped away?
        /// </remarks>
        /// <seealso cref="RectClipContent"/>
        [ViewVariables(VVAccess.ReadWrite)]
        public int RectDrawClipMargin { get; set; } = 10;

        // You may wonder why Modulate isn't stylesheet controlled, but ModulateSelf is.
        // Reason is simple: I'm fucking lazy.
        // I'm expecting this comment to last much longer than the problem it's pointing out.

        /// <summary>
        ///     An override for the modulate self from the style sheet.
        /// </summary>
        /// <seealso cref="ActualModulateSelf" />
        [ViewVariables(VVAccess.ReadWrite)]
        public Color? ModulateSelfOverride { get; set; }

        /// <summary>
        ///     Modulates the color of this control and all its children when drawing.
        /// </summary>
        /// <remarks>
        ///     Modulation is multiplying or tinting the color basically.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable]
        public Color Modulate { get; set; } = Color.White;

        /// <summary>
        ///     The value used to modulate this control (and not its siblings) with on top of <see cref="Modulate"/>
        ///     when drawing.
        /// </summary>
        /// <remarks>
        ///     By default this value is pulled from CSS, or <see cref="ModulateSelfOverride"/> if available.
        ///
        ///     Modulation is multiplying or tinting the color basically.
        /// </remarks>
        public Color ActualModulateSelf
        {
            get
            {
                if (ModulateSelfOverride.HasValue)
                {
                    return ModulateSelfOverride.Value;
                }

                if (TryGetStyleProperty(StylePropertyModulateSelf, out Color modulate))
                {
                    return modulate;
                }

                return Color.White;
            }
        }

        /// <summary>
        ///     Default constructor.
        ///     The name of the control is decided based on type.
        /// </summary>
        public Control()
        {
            UserInterfaceManagerInternal = IoCManager.Resolve<IUserInterfaceManagerInternal>();
            StyleClasses = new StyleClassCollection(this);
            Children = new OrderedChildCollection(this);
            XamlChildren = Children;
        }

        /// <summary>
        ///     Called to render this control.
        /// </summary>
        /// <remarks>
        ///     Drawing is done relative to the position of the control.
        ///     It is also done in pixel space, so you should not directly use properties such as <see cref="Size"/>.
        /// </remarks>
        /// <param name="handle">A handle that can be used to draw.</param>
        protected internal virtual void Draw(DrawingHandleScreen handle)
        {
        }

        internal virtual void DrawInternal(IRenderHandle renderHandle)
        {
            Draw(renderHandle.DrawingHandleScreen);
        }

        public void UpdateDraw()
        {
        }

        /// <summary>
        ///     Called when this modal control is closed.
        ///     Only used for controls that are actually modals.
        /// </summary>
        protected internal virtual void ModalRemoved()
        {
        }

        public bool Disposed { get; private set; }

        /// <summary>
        ///     Dispose this control, its own scene control, and all its children.
        ///     Basically the big delete button.
        /// </summary>
        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            Dispose(true);
            Disposed = true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            UserInterfaceManagerInternal.HideTooltipFor(this);

            DisposeAllChildren();
            Parent?.RemoveChild(this);

            OnKeyBindDown = null;
        }

        /// <summary>
        ///     Dispose all children, but leave this one intact.
        /// </summary>
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
        ///     Remove all the children from this control.
        /// </summary>
        public void RemoveAllChildren()
        {
            DebugTools.Assert(!Disposed, "Control has been disposed.");

            foreach (var child in Children.ToArray())
            {
                RemoveChild(child);
            }
        }

        /// <summary>
        ///     Make this child an orphan. i.e. remove it from its parent if it has one.
        /// </summary>
        public void Orphan()
        {
            DebugTools.Assert(!Disposed, "Control has been disposed.");

            Parent?.RemoveChild(this);
        }

        /// <summary>
        ///     Make the provided control a parent of this control.
        /// </summary>
        /// <param name="child">The control to make a child of this control.</param>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if we already have a component with the same name,
        ///     or the provided component is still parented to a different control.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="child" /> is <c>null</c>.
        /// </exception>
        public void AddChild(Control child)
        {
            DebugTools.Assert(!Disposed, "Control has been disposed.");

            if (child == null) throw new ArgumentNullException(nameof(child));
            if (child.Parent != null)
            {
                throw new InvalidOperationException("This component is still parented. Deparent it before adding it.");
            }

            DebugTools.Assert(!child.Disposed, "Child is disposed.");

            if (child == this)
            {
                throw new InvalidOperationException("You can't parent something to itself!");
            }

            // Ensure this control isn't a parent of ours.
            // Doesn't need to happen if the control has no children of course.
            if (child.ChildCount != 0)
            {
                for (var parent = Parent; parent != null; parent = parent.Parent)
                {
                    if (parent == child)
                    {
                        throw new ArgumentException("This control is one of our parents!", nameof(child));
                    }
                }
            }

            child.Parent = this;
            _orderedChildren.Add(child);

            child.Parented(this);
            if (IsInsideTree)
            {
                child._propagateEnterTree();
            }

            ChildAdded(child);
        }

        /// <summary>
        ///     Called after a new child is added to this control.
        /// </summary>
        /// <param name="newChild">The new child.</param>
        protected virtual void ChildAdded(Control newChild)
        {
            MinimumSizeChanged();
        }

        /// <summary>
        ///     Called when this control gets made a child of a different control.
        /// </summary>
        /// <param name="newParent">The new parent component.</param>
        protected virtual void Parented(Control newParent)
        {
            StylesheetUpdateRecursive();
            UpdateLayout();
        }

        /// <summary>
        ///     Removes the provided child from this control.
        /// </summary>
        /// <param name="child">The child to remove.</param>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if the provided child is not one of this control's children.
        /// </exception>
        public void RemoveChild(Control child)
        {
            DebugTools.Assert(!Disposed, "Control has been disposed.");

            if (child.Parent != this)
            {
                throw new InvalidOperationException("The provided control is not a direct child of this control.");
            }

            _orderedChildren.Remove(child);

            child.Parent = null;

            child.Deparented();
            if (IsInsideTree)
            {
                child._propagateExitTree();
            }

            ChildRemoved(child);
        }

        /// <summary>
        ///     Called when a child is removed from this child.
        /// </summary>
        /// <param name="child">The former child.</param>
        protected virtual void ChildRemoved(Control child)
        {
            MinimumSizeChanged();
        }

        /// <summary>
        ///     Called when this control is removed as child from the former parent.
        /// </summary>
        protected virtual void Deparented()
        {
        }

        /// <summary>
        ///     Called when the order index of a child changes.
        /// </summary>
        /// <param name="child">The child that was changed.</param>
        /// <param name="oldIndex">The previous index of the child.</param>
        /// <param name="newIndex">The new index of the child.</param>
        protected virtual void ChildMoved(Control child, int oldIndex, int newIndex)
        {
        }

        /// <summary>
        ///     Called to test whether this control has a certain point,
        ///     for the purposes of finding controls under the cursor.
        /// </summary>
        /// <param name="point">The relative point, in virtual pixels.</param>
        /// <returns>True if this control does have the point and should be counted as a hit.</returns>
        protected internal virtual bool HasPoint(Vector2 point)
        {
            // This is effectively the same implementation as the default Godot one in Control.cpp.
            // That one gets ignored because to Godot it looks like we're ALWAYS implementing a custom HasPoint.
            var size = Size;
            return point.X >= 0 && point.X <= size.X && point.Y >= 0 && point.Y <= size.Y;
        }

        /// <summary>
        ///     Gets the immediate child of this control with the specified index.
        /// </summary>
        /// <param name="index">The index of the child.</param>
        /// <returns>The child.</returns>
        public Control GetChild(int index)
        {
            return _orderedChildren[index];
        }

        /// <summary>
        ///     Gets the "index" in the parent.
        ///     This index is used for ordering of actions like input and drawing among siblings.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if this control has no parent.
        /// </exception>
        public int GetPositionInParent()
        {
            if (Parent == null)
            {
                throw new InvalidOperationException("This control has no parent!");
            }

            return Parent._orderedChildren.IndexOf(this);
        }

        /// <summary>
        ///     Sets the index of this control in the parent.
        ///     This pretty much corresponds to layout and drawing order in relation to its siblings.
        /// </summary>
        /// <param name="position"></param>
        /// <exception cref="InvalidOperationException">This control has no parent.</exception>
        public void SetPositionInParent(int position)
        {
            if (Parent == null)
            {
                throw new InvalidOperationException("No parent to change position in.");
            }

            var posInParent = GetPositionInParent();
            if (posInParent == position)
            {
                return;
            }

            Parent._orderedChildren.RemoveAt(posInParent);
            Parent._orderedChildren.Insert(position, this);
            Parent.ChildMoved(this, posInParent, position);
        }

        /// <summary>
        ///     Makes this the first control among its siblings,
        ///     So that it's first in things such as drawing order.
        /// </summary>
        /// <exception cref="InvalidOperationException">This control has no parent.</exception>
        public void SetPositionFirst()
        {
            SetPositionInParent(0);
        }

        /// <summary>
        ///     Makes this the last control among its siblings,
        ///     So that it's last in things such as drawing order.
        /// </summary>
        /// <exception cref="InvalidOperationException">This control has no parent.</exception>
        public void SetPositionLast()
        {
            if (Parent == null)
            {
                throw new InvalidOperationException("No parent to change position in.");
            }

            SetPositionInParent(Parent.ChildCount - 1);
        }

        /// <summary>
        ///     Called when this control receives keyboard focus.
        /// </summary>
        protected internal virtual void FocusEntered()
        {
        }

        /// <summary>
        ///     Called when this control loses keyboard focus.
        /// </summary>
        protected internal virtual void FocusExited()
        {
        }

        /// <summary>
        ///     Check if this control currently has keyboard focus.
        /// </summary>
        /// <returns></returns>
        public virtual bool HasKeyboardFocus()
        {
            return UserInterfaceManager.KeyboardFocused == this;
        }

        /// <summary>
        ///     Grab keyboard focus if this control doesn't already have it.
        /// </summary>
        /// <remarks>
        ///     <see cref="CanKeyboardFocus"/> must be true for this to work.
        /// </remarks>
        public void GrabKeyboardFocus()
        {
            UserInterfaceManager.GrabKeyboardFocus(this);
        }

        /// <summary>
        ///     Release keyboard focus from this control if it has it.
        ///     If a different control has keyboard focus, nothing happens.
        /// </summary>
        public void ReleaseKeyboardFocus()
        {
            UserInterfaceManager.ReleaseKeyboardFocus(this);
        }

        /// <summary>
        ///     Called when the size of the control changes.
        /// </summary>
        protected virtual void Resized()
        {
        }

        internal void DoUpdate(FrameEventArgs args)
        {
            Update(args);
            foreach (var child in Children)
            {
                child.DoUpdate(args);
            }
        }

        /// <summary>
        ///     This is called every process frame.
        /// </summary>
        protected virtual void Update(FrameEventArgs args)
        {
        }

        internal void DoFrameUpdate(FrameEventArgs args)
        {
            FrameUpdate(args);
            foreach (var child in Children)
            {
                child.DoFrameUpdate(args);
            }
        }

        /// <summary>
        ///     This is called before every render frame.
        /// </summary>
        protected virtual void FrameUpdate(FrameEventArgs args)
        {
            ProcessAnimations(args);
        }

        // These are separate from StandardCursorShape so that
        // in the future we could have an API to override the styling.

        public override string ToString()
        {
            return $"{Name} ({GetType().Name})";
        }

        /// <summary>
        ///     Mode that will be tested when testing controls to invoke mouse button events on.
        /// </summary>
        public enum MouseFilterMode : byte
        {
            /// <summary>
            ///     The control will be able to receive mouse buttons events.
            ///     Furthermore, if a control with this mode does get clicked,
            ///     the event automatically gets marked as handled after every other candidate has been tried,
            ///     so that the rest of the game does not receive it.
            /// </summary>
            Pass = 1,

            /// <summary>
            ///     The control will be able to receive mouse button events like <see cref="Pass" />,
            ///     but the event will be stopped and handled even if the relevant events do not handle it.
            /// </summary>
            Stop = 0,

            /// <summary>
            ///     The control will not be considered at all, and will not have any effects.
            /// </summary>
            Ignore = 2,
        }

        public class OrderedChildCollection : ICollection<Control>, IReadOnlyCollection<Control>
        {
            private readonly Control Owner;

            public OrderedChildCollection(Control owner)
            {
                Owner = owner;
            }

            public Enumerator GetEnumerator()
            {
                return new(Owner);
            }

            IEnumerator<Control> IEnumerable<Control>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public void Add(Control item)
            {
                Owner.AddChild(item);
            }

            public void Clear()
            {
                Owner.RemoveAllChildren();
            }

            public bool Contains(Control item)
            {
                return item?.Parent == Owner;
            }

            public void CopyTo(Control[] array, int arrayIndex)
            {
                Owner._orderedChildren.CopyTo(array, arrayIndex);
            }

            public bool Remove(Control item)
            {
                if (item?.Parent != Owner)
                {
                    return false;
                }

                DebugTools.AssertNotNull(Owner);
                Owner.RemoveChild(item);

                return true;
            }

            int ICollection<Control>.Count => Owner.ChildCount;
            int IReadOnlyCollection<Control>.Count => Owner.ChildCount;

            public bool IsReadOnly => false;


            public struct Enumerator : IEnumerator<Control>
            {
                private List<Control>.Enumerator _enumerator;

                internal Enumerator(Control control)
                {
                    _enumerator = control._orderedChildren.GetEnumerator();
                }

                public bool MoveNext()
                {
                    return _enumerator.MoveNext();
                }

                public void Reset()
                {
                    ((IEnumerator) _enumerator).Reset();
                }

                public Control Current => _enumerator.Current;

                object IEnumerator.Current => Current;

                public void Dispose()
                {
                    _enumerator.Dispose();
                }
            }
        }
    }

    public delegate Control? TooltipSupplier(Control sender);
}
