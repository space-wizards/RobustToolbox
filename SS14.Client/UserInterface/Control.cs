#if GODOT
using SS14.Client.GodotGlue;
#endif
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Globalization;
using SS14.Shared.Log;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.ContentPack;
using System.Reflection;
using SS14.Shared.Maths;
using SS14.Client.Utility;
using SS14.Client.Graphics.Drawing;
using SS14.Shared.Utility;
using SS14.Client.Interfaces.ResourceManagement;
using System.IO;
using JetBrains.Annotations;
using SS14.Client.Graphics;

namespace SS14.Client.UserInterface
{
    /// <summary>
    ///     A node in the GUI system.
    ///     NOTE: For docs, most of these are direct proxies to Godot's Control.
    ///     See the official docs for more help: https://godot.readthedocs.io/en/3.0/classes/class_control.html
    /// </summary>
#if GODOT
    [ControlWrap(typeof(Godot.Control))]
    #endif
    // ReSharper disable once RequiredBaseTypesIsNotInherited
    public partial class Control : IDisposable
    {
        /// <summary>
        ///     The name of this control.
        ///     Names must be unique between the control's siblings.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (value == _name)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("New name may not be null or whitespace.", nameof(value));
                }

                if (Parent != null)
                {
                    if (Parent.HasChild(value))
                    {
                        throw new ArgumentException($"Parent already has a child with name {value}.");
                    }

                    Parent._children.Remove(_name);
                }

                _name = value;
#if GODOT
                SceneControl.SetName(_name);
                #endif

                if (Parent != null)
                {
                    Parent._children[_name] = this;
                }
            }
        }

        private string _name;

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

#if GODOT
/// <summary>
///     The control's representation in Godot's scene tree.
/// </summary>
        internal Godot.Control SceneControl { get; private set; }
        #endif

        /// <summary>
        ///     Path to the .tscn file for this scene in the VFS.
        ///     This is mainly intended for content loading tscn files.
        ///     Don't use it from the engine.
        /// </summary>
        protected virtual ResourcePath ScenePath => null;

#if GODOT
        private ControlWrap WrappedSceneControl;
        #endif

        public const float ANCHOR_BEGIN = 0;
        public const float ANCHOR_END = 1;

        public float AnchorBottom
        {
#if GODOT
            get => SceneControl.AnchorBottom;
            set => SceneControl.AnchorBottom = value;
#else
            get => default;
            set { }
#endif
        }

        public float AnchorLeft
        {
#if GODOT
            get => SceneControl.AnchorLeft;
            set => SceneControl.AnchorLeft = value;
#else
            get => default;
            set { }
#endif
        }

        public float AnchorRight
        {
#if GODOT
            get => SceneControl.AnchorRight;
            set => SceneControl.AnchorRight = value;
#else
            get => default;
            set { }
#endif
        }

        public float AnchorTop
        {
#if GODOT
            get => SceneControl.AnchorTop;
            set => SceneControl.AnchorTop = value;
#else
            get => default;
            set { }
#endif
        }

        public float MarginRight
        {
#if GODOT
            get => SceneControl.MarginRight;
            set => SceneControl.MarginRight = value;
#else
            get => default;
            set { }
#endif
        }

        public float MarginLeft
        {
#if GODOT
            get => SceneControl.MarginLeft;
            set => SceneControl.MarginLeft = value;
#else
            get => default;
            set { }
#endif
        }

        public float MarginTop
        {
#if GODOT
            get => SceneControl.MarginTop;
            set => SceneControl.MarginTop = value;
#else
            get => default;
            set { }
#endif
        }

        public float MarginBottom
        {
#if GODOT
            get => SceneControl.MarginBottom;
            set => SceneControl.MarginBottom = value;
#else
            get => default;
            set { }
#endif
        }

        public bool Visible
        {
#if GODOT
            get => SceneControl.Visible;
            set => SceneControl.Visible = value;
#else
            get => default;
            set { }
#endif
        }

        public Vector2 Size
        {
#if GODOT
            get => SceneControl.GetSize().Convert();
            set => SceneControl.SetSize(value.Convert());
#else
            get => default;
            set { }
#endif
        }

        public Vector2 Position
        {
#if GODOT
            get => SceneControl.GetPosition().Convert();
            set => SceneControl.SetPosition(value.Convert());
#else
            get => default;
            set { }
#endif
        }

        public UIBox2 Rect
        {
#if GODOT
            get => SceneControl.GetRect().Convert();
#else
            get => throw new NotImplementedException();
#endif
        }

        public Vector2 Scale
        {
#if GODOT
            get => SceneControl.RectScale.Convert();
            set => SceneControl.RectScale = value.Convert();
#else
            get => default;
            set { }
#endif
        }

        public string ToolTip
        {
#if GODOT
            get => SceneControl.GetTooltip();
            set => SceneControl.SetTooltip(value);
#else
            get => default;
            set { }
#endif
        }

        public MouseFilterMode MouseFilter
        {
#if GODOT
            get => (MouseFilterMode) SceneControl.MouseFilter;
            set => SceneControl.MouseFilter = (Godot.Control.MouseFilterEnum) value;
#else
            get => default;
            set { }
#endif
        }

        public SizeFlags SizeFlagsHorizontal
        {
#if GODOT
            get => (SizeFlags) SceneControl.SizeFlagsHorizontal;
            set => SceneControl.SizeFlagsHorizontal = (int) value;
#else
            get => default;
            set { }
#endif
        }

        public float SizeFlagsStretchRatio
        {
#if GODOT
            get => SceneControl.SizeFlagsStretchRatio;
            set => SceneControl.SizeFlagsStretchRatio = value;
#else
            get => default;
            set { }
#endif
        }

        public SizeFlags SizeFlagsVertical
        {
#if GODOT
            get => (SizeFlags) SceneControl.SizeFlagsVertical;
            set => SceneControl.SizeFlagsVertical = (int) value;
#else
            get => default;
            set { }
#endif
        }

        public bool RectClipContent
        {
#if GODOT
            get => SceneControl.RectClipContent;
            set => SceneControl.RectClipContent = value;
#else
            get => default;
            set { }
#endif
        }

        /// <summary>
        ///     A combination of <see cref="CustomMinimumSize" /> and <see cref="CalculateMinimumSize" />,
        ///     Whichever is greater.
        ///     Use this for whenever you need the *actual* minimum size of something.
        /// </summary>
        public Vector2 CombinedMinimumSize
        {
#if GODOT
            get => SceneControl.GetCombinedMinimumSize().Convert();
#else
            get => default;
            set { }
#endif
        }

        /// <summary>
        ///     A custom minimum size. If the control-calculated size is is smaller than this, this is used instead.
        /// </summary>
        /// <seealso cref="CalculateMinimumSize" />
        /// <seealso cref="CombinedMinimumSize" />
        public Vector2 CustomMinimumSize
        {
#if GODOT
            get => SceneControl.RectMinSize.Convert();
            set => SceneControl.RectMinSize = value.Convert();
#else
            get => default;
            set { }
#endif
        }

        public Vector2 GlobalMousePosition
        {
#if GODOT
            get => SceneControl.GetGlobalMousePosition().Convert();
#else
            get => throw new NotImplementedException();
#endif
        }

        private readonly Dictionary<string, Control> _children = new Dictionary<string, Control>();

        /// <summary>
        ///     Default constructor.
        ///     The name of the control is decided based on type.
        /// </summary>
        public Control()
        {
            UserInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();
#if GODOT
            SetupSceneControl();
            #endif
            Name = GetType().Name;
            Initialize();
        }

        /// <param name="name">The name the component will have.</param>
        public Control(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name must not be null or whitespace.", nameof(name));
            }

            UserInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();
#if GODOT
            SetupSceneControl();
            #endif
            Name = name;
            Initialize();
        }

#if GODOT
/// <summary>
///     Wrap the provided Godot control with this one.
///     This does NOT set up parenting correctly!
/// </summary>
        internal Control(Godot.Control control)
        {
            SetSceneControl(control);
            UserInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();
            _name = control.GetName();
            SetupSignalHooks();
            //Logger.Debug($"Wrapping control {Name} ({GetType()} -> {control.GetType()})");
            Initialize();
            InjectControlWrap();
        }
        #endif

        /// <summary>
        ///     Use this to do various initialization of the control.
        ///     Ranging from spawning children to prefetching them for later referencing.
        /// </summary>
        protected virtual void Initialize()
        {
        }

#if GODOT
        private void SetupSceneControl()
        {
            Godot.Control newSceneControl;
            if (ScenePath == null)
            {
                newSceneControl = SpawnSceneControl();
            }
            else
            {
                // Get disk path with VFS.
                var cache = IoCManager.Resolve<IResourceCache>();
                if (!cache.TryGetDiskFilePath(ScenePath, out var diskPath))
                {
                    throw new FileNotFoundException("Scene path must exist on disk.");
                }

                newSceneControl = LoadScene(diskPath);
            }

            SetSceneControl(newSceneControl);
            SetupSignalHooks();
            // Certain controls (LineEdit, WindowDialog, etc...) create sub controls automatically,
            // handle these.
            WrapChildControls();
            InjectControlWrap();
        }

        // ASSUMING DIRECT CONTROL.
        private void InjectControlWrap()
        {
            // Inject wrapper script to hook virtual functions.
            // IMPORTANT: Because of how Scripts work in Godot,
            // it has to effectively "replace" the type of the control.
            // It... obviously cannot do this because this is [insert statically typed language].
            // As such: getting an instance to the control AFTER this point will yield a control of type ControlWrap.
            // Luckily, the old instance seems to still work flawlessy for everything, including signals!
            var script = Godot.GD.Load("res://ControlWrap.cs");
            SceneControl.SetScript(script);

            // So... getting a new reference to ourselves is surprisingly difficult!
            if (SceneControl.GetChildCount() > 0)
            {
                // Potentially easiest: if we have a child, get the parent of our child (us).
                WrappedSceneControl = (ControlWrap) SceneControl.GetChild(0).GetParent();
            }
            else if (SceneControl.GetParent() != null)
            {
                // If not but we have a parent use that.
                WrappedSceneControl = (ControlWrap) SceneControl.GetParent().GetChild(SceneControl.GetIndex());
            }
            else
            {
                // Ok so we're literally a lone node guess making a temporary child'll be fine.
                var node = new Godot.Node();
                SceneControl.AddChild(node);
                WrappedSceneControl = (ControlWrap) node.GetParent();
                node.QueueFree();
            }

            WrappedSceneControl.GetMinimumSizeOverride = () => CalculateMinimumSize().Convert();
            WrappedSceneControl.HasPointOverride = (point) => HasPoint(point.Convert());
            WrappedSceneControl.DrawOverride = DoDraw;
        }
        #endif

        private void DoDraw()
        {
#if GODOT
            using (var handle = new DrawingHandleScreen(SceneControl.GetCanvasItem()))
            {
                Draw(handle);
            }
            #endif
        }

        protected virtual void Draw(DrawingHandleScreen handle)
        {
        }

        public void UpdateDraw()
        {
#if GODOT
            SceneControl.Update();
            #endif
        }

#if GODOT
/// <summary>
///     Overriden by child classes to change the Godot control type.
///     ONLY spawn the control in here. Use <see cref="SetSceneControl" /> for holding references to it.
///     This is to allow children to override it without breaking the setting.
/// </summary>
        private protected virtual Godot.Control SpawnSceneControl()
        {
            return new Godot.Control();
        }

        /// <summary>
        ///     override by child classes to have a reference to the Godot control for accessing.
        /// </summary>
        /// <param name="control"></param>
        private protected virtual void SetSceneControl(Godot.Control control)
        {
            SceneControl = control;
        }
        #endif

        public bool Disposed { get; private set; } = false;

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
            GC.SuppressFinalize(this);
            Disposed = true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeAllChildren();
                Parent?.RemoveChild(this);

                OnKeyDown = null;
            }

#if GODOT
            DisposeSignalHooks();

            if (!GameController.ShuttingDownHard)
            {
                SceneControl?.QueueFree();
                SceneControl?.Dispose();
                SceneControl = null;

                // Don't QueueFree since these are the same node.
                // Kinda sorta mostly probably hopefully.
                WrappedSceneControl?.Dispose();
                WrappedSceneControl = null;
            }
#endif
        }

        ~Control()
        {
            Dispose(false);
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
        ///     Make the provided control a parent of this control.
        /// </summary>
        /// <param name="child">The control to make a child of this control.</param>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if we already have a component with the same name,
        ///     or the provided component is still parented to a different control.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///    <paramref name="child"/> is <c>null</c>.
        /// </exception>
        public virtual void AddChild(Control child, bool LegibleUniqueName = false)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));
            if (child.Parent != null)
            {
                throw new InvalidOperationException("This component is still parented. Deparent it before adding it.");
            }

            child.Parent = this;
            child.Parented(this);
#if GODOT
            SceneControl.AddChild(child.SceneControl, LegibleUniqueName);
            // Godot changes the name automtically if you would cause a naming conflict.
            if (child.SceneControl.GetName() != child._name)
            {
                child._name = child.SceneControl.GetName();
            }
            #endif

            _children[child.Name] = child;
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
#if GODOT
            SceneControl.RemoveChild(child.SceneControl);
            #endif
        }

        /// <summary>
        ///     Called when this control is removed as child from the former parent.
        /// </summary>
        protected virtual void Deparented()
        {
        }

        /// <summary>
        ///     Override this to calculate a minimum size for this control.
        ///     Do NOT call this directly to get the minimum size for layout purposes!
        ///     Use <see cref="GetCombinedMinimumSize" /> for the ACTUAL minimum size.
        /// </summary>
        protected virtual Vector2 CalculateMinimumSize()
        {
            return Vector2.Zero;
        }

        /// <summary>
        ///     Tells the GUI system that the minimum size of this control may have changed,
        ///     so that say containers will re-sort it.
        /// </summary>
        public void MinimumSizeChanged()
        {
#if GODOT
            SceneControl.MinimumSizeChanged();
            #endif
        }

        protected virtual bool HasPoint(Vector2 point)
        {
            // This is effectively the same implementation as the default Godot one in Control.cpp.
            // That one gets ignored because to Godot it looks like we're ALWAYS implementing a custom HasPoint.
            var size = Size;
            return point.X >= 0 && point.X <= size.X && point.Y >= 0 && point.Y <= size.Y;
        }

        public T GetChild<T>(string name) where T : Control
        {
            return (T) GetChild(name);
        }

        private static readonly char[] SectionSplitDelimiter = {'/'};

        public Control GetChild(string name)
        {
            if (name.IndexOf('/') != -1)
            {
                var current = this;
                foreach (var section in name.Split(SectionSplitDelimiter, StringSplitOptions.RemoveEmptyEntries))
                {
                    current = current.GetChild(section);
                }

                return current;
            }

            if (TryGetChild(name, out var control))
            {
                return control;
            }

            throw new KeyNotFoundException($"No child UI element {name}");
        }

        public bool TryGetChild<T>(string name, out T child) where T : Control
        {
            if (_children.TryGetValue(name, out var control))
            {
                child = (T) control;
                return true;
            }

            child = null;
            return false;
        }

        public bool TryGetChild(string name, out Control child)
        {
            return _children.TryGetValue(name, out child);
        }

        public bool HasChild(string name)
        {
            return _children.ContainsKey(name);
        }

        // TODO: Expose this as public.
        // The problem is that non-Control nodes such as timers mess up the position so it isn't consistent.
        /// <summary>
        ///     Sets the index of this control in Godot's scene tree.
        ///     This pretty much corresponds to layout and drawing order in relation to its siblings.
        /// </summary>
        /// <param name="position"></param>
        /// <exception cref="InvalidOperationException">This control has no parent.</exception>
        internal void SetPositionInParent(int position)
        {
            if (Parent == null)
            {
                throw new InvalidOperationException("No parent to change position in.");
            }

#if GODOT
            Parent.SceneControl.MoveChild(SceneControl, 0);
            #endif
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

#if GODOT
            SetPositionInParent(Parent.SceneControl.GetChildCount());
            #endif
        }

        /// <summary>
        ///     Called when this control receives focus.
        /// </summary>
        protected virtual void FocusEntered()
        {
            UserInterfaceManager.FocusEntered(this);
        }

        /// <summary>
        ///     Called when this control loses focus.
        /// </summary>
        protected virtual void FocusExited()
        {
            UserInterfaceManager.FocusExited(this);
        }

        public bool HasFocus()
        {
#if GODOT
            return SceneControl.HasFocus();
#else
            throw new NotImplementedException();
#endif
        }

        public void GrabFocus()
        {
#if GODOT
            SceneControl.GrabFocus();
            #endif
        }

        public void ReleaseFocus()
        {
#if GODOT
            SceneControl?.ReleaseFocus();
            #endif
        }

        protected virtual void Resized()
        {
        }

#if GODOT
        internal static Control InstanceScene(string resourcePath)
        {
            var res = (Godot.PackedScene) Godot.ResourceLoader.Load(resourcePath);
            return InstanceScene(res);
        }
        #endif

#if GODOT
/// <summary>
///     Instance a packed Godot scene as a child of this one, wrapping all the nodes in SS14 controls.
///     This makes it possible to use Godot's GUI editor relatively comfortably,
///     while still being able to use the better SS14 API.
/// </summary>
/// <param name="scene"></param>
/// <returns></returns>
// TODO: Handle instances inside the provided scene in some way.
//       Shouldn't need more than support for populating the GodotTranslationCache
//         from SS14.Client.Godot I *think*?
        internal static Control InstanceScene(Godot.PackedScene scene)
        {
            var root = (Godot.Control) scene.Instance();
            return WrapGodotControl(null, root);
        }

        private static Control WrapGodotControl(Control parent, Godot.Control control)
        {
            var type = FindGodotTranslationType(control);
            var newControl = (Control) Activator.CreateInstance(type,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance | BindingFlags.NonPublic,
                null, new object[] {control}, null, null);

            if (parent != null)
            {
                newControl.Parent = parent;
                parent._children[newControl.Name] = newControl;
            }

            newControl.WrapChildControls();

            return newControl;
        }

        private void WrapChildControls()
        {
            foreach (var child in SceneControl.GetChildren())
            {
                // Some Godot nodes have subnodes.
                // great example being the LineEdit.
                // These subnodes may be other stuff like timers,
                // so don't blow up on it!
                if (child is Godot.Control childControl)
                {
                    WrapGodotControl(this, childControl);
                }
            }
        }

        private static Dictionary<Type, Type> GodotTranslationCache;

        // Because the translation cache may not include every control,
        // for example controls we don't have SS14 counterparts to,
        // this method will look up the inheritance tree until (last resort) it hits Godot.Control.
        // Filling in the blanks later.
        private static Type FindGodotTranslationType(Godot.Control control)
        {
            if (GodotTranslationCache == null)
            {
                SetupGodotTranslationCache();
            }

            var original = control.GetType();
            var tmp = original;
            // CanvasItem is the parent of Godot.Control so reaching it means we passed Godot.Control.
            while (tmp != typeof(Godot.CanvasItem))
            {
                if (GodotTranslationCache.TryGetValue(tmp, out var info))
                {
                    if (original != tmp)
                    {
                        GodotTranslationCache[original] = info;
                    }

                    return info;
                }

                tmp = tmp.BaseType;
            }

            throw new InvalidOperationException(
                "Managed to pass Godot.Control when finding translations. This should be impossible!");
        }

        private static void SetupGodotTranslationCache()
        {
            GodotTranslationCache = new Dictionary<Type, Type>();
            var refl = IoCManager.Resolve<IReflectionManager>();
            foreach (var childType in refl.GetAllChildren<Control>(inclusive: true))
            {
                var attr = childType.GetCustomAttribute<ControlWrapAttribute>();
                if (attr == null)
                {
                    continue;
                }

                var godotType = attr.GodotType;

                if (GodotTranslationCache.TryGetValue(godotType, out var dupe))
                {
                    Logger.Error(
                        $"Found multiple SS14 Control types pointing to a single Godot Control type. Godot: {godotType}, first: {dupe}, second: {childType}");
                    continue;
                }

                GodotTranslationCache[godotType] = childType;
            }

            if (!GodotTranslationCache.ContainsKey(typeof(Godot.Control)))
            {
                GodotTranslationCache = null;
                throw new InvalidOperationException(
                    "We don't even have the base Godot Control in the translation cache. We can't use scene instancing like this!");
            }
        }
        #endif

        public void SetAnchorPreset(LayoutPreset preset, bool keepMargin = false)
        {
#if GODOT
            SceneControl.SetAnchorsPreset((Godot.Control.LayoutPreset) preset, keepMargin);
            #endif
        }

        public void SetMarginsPreset(LayoutPreset preset, LayoutPresetMode resizeMode = LayoutPresetMode.Minsize,
            int margin = 0)
        {
#if GODOT
            SceneControl.SetMarginsPreset((Godot.Control.LayoutPreset) preset,
                (Godot.Control.LayoutPresetMode) resizeMode, margin);
            #endif
        }

        public enum LayoutPreset : byte
        {
            TopLeft = 0,
            TopRight = 1,
            BottomLeft = 2,
            BottomRight = 3,
            CenterLeft = 4,
            CenterTop = 5,
            CenterRight = 6,
            CenterBottom = 7,
            Center = 8,
            LeftWide = 9,
            TopWide = 10,
            RightWide = 11,
            BottomWide = 12,
            VerticalCenterWide = 13,
            HorizontalCenterWide = 14,
            Wide = 15,
        }

        public enum LayoutPresetMode : byte
        {
            Minsize = 0,
            KeepWidth = 1,
            KeepHeight = 2,
            KeepSize = 3,
        }

        /// <summary>
        ///     Controls how a control changes size when inside a container.
        /// </summary>
        public enum SizeFlags : byte
        {
            /// <summary>
            ///     Do not resize inside containers.
            /// </summary>
            None = 0,

            /// <summary>
            ///     Fill as much space as possible in a container, without pushing others.
            /// </summary>
            Fill = 1,

            /// <summary>
            ///     Fill as much space as possible in a container, pushing other nodes.
            ///     The ratio of pushing if there's multiple set to expand is depenent on <see cref="SizeFlagsStretchRatio" />
            /// </summary>
            Expand = 2,

            /// <summary>
            ///     Combination of <see cref="Fill" /> and <see cref="Expand" />.
            /// </summary>
            FillExpand = 3,

            /// <summary>
            ///     Shrink inside a container, aligning to the center.
            /// </summary>
            ShrinkCenter = 4,

            /// <summary>
            ///     Shrink inside a container, aligning to the end.
            /// </summary>
            ShrinkEnd = 8,
        }

        protected void SetColorOverride(string name, Color? color)
        {
#if GODOT
// So here's an interesting one.
// Godot's AddColorOverride and such API on controls
// Doesn't actually have a way to REMOVE the override.
// Passing null via Set() does though.
            if (color != null)
            {
                SceneControl.AddColorOverride(name, color.Value.Convert());
            }
            else
            {
                SceneControl.Set($"custom_colors/{name}", null);
            }
            #endif
        }

        protected Color? GetColorOverride(string name)
        {
#if GODOT
            return SceneControl.HasColorOverride(name) ? SceneControl.GetColor(name).Convert() : (Color?) null;
#else
            throw new NotImplementedException();
#endif
        }

        protected void SetConstantOverride(string name, int? constant)
        {
#if GODOT
            if (constant != null)
            {
                SceneControl.AddConstantOverride(name, constant.Value);
            }
            else
            {
                SceneControl.Set($"custom_constants/{name}", null);
            }
            #endif
        }

        protected int? GetConstantOverride(string name)
        {
#if GODOT
            return SceneControl.HasConstantOverride(name) ? SceneControl.GetConstant(name) : (int?) null;
#else
            throw new NotImplementedException();
#endif
        }

        protected void SetStyleBoxOverride(string name, StyleBox styleBox)
        {
#if GODOT
            SceneControl.AddStyleboxOverride(name, styleBox.GodotStyleBox);
            #endif
        }

        protected StyleBox GetStyleBoxOverride(string name)
        {
#if GODOT
            var box = SceneControl.HasStyleboxOverride(name) ? SceneControl.GetStylebox(name) : null;
            return box == null ? null : new GodotStyleBoxWrap(box);
#else
            throw new NotImplementedException();
#endif
        }

        protected void SetFontOverride(string name, Font font)
        {
#if GODOT
            SceneControl.AddFontOverride(name, font);
            #endif
        }

        protected Font GetFontOverride(string name)
        {
#if GODOT
            var font = SceneControl.HasFontOverride(name) ? SceneControl.GetFont(name) : null;
            return font == null ? null : new GodotWrapFont(font);
#else
            throw new NotImplementedException();
#endif
        }

        public void DoUpdate(ProcessFrameEventArgs args)
        {
            Update(args);
            foreach (var child in Children)
            {
                child.DoUpdate(args);
            }
        }

        protected virtual void Update(ProcessFrameEventArgs args)
        {
        }

        public enum CursorShape
        {
            Arrow = 0,
            IBeam = 1,
            PointingHand = 2,
            Cross = 3,
            Wait = 4,
            Busy = 5,
            Drag = 6,
            CanDrop = 7,
            Forbidden = 8,
            VSize = 9,
            HSize = 10,
            BDiagSize = 11,
            FDiagSize = 12,
            Move = 13,
            VSplit = 14,
            HSplit = 15,
            Help = 16,
        }

        public CursorShape DefaultCursorShape
        {
#if GODOT
            get => (CursorShape) SceneControl.GetDefaultCursorShape();
            set => SceneControl.SetDefaultCursorShape((Godot.Control.CursorShape) value);
#else
            get => default;
            set { }
#endif
        }

        /// <summary>
        ///     Mode that will be tested when testing controls to invoke mouse button events on.
        /// </summary>
        public enum MouseFilterMode
        {
            /// <summary>
            ///     The control will not be considered at all, and will not have any effects.
            /// </summary>
            Ignore = 0,

            /// <summary>
            ///     The control will be able to receive mouse buttons events.
            ///     Furthermore, if a control with this mode does get clicked,
            ///     the event automatically gets marked as handled.
            /// </summary>
            Pass = 1,

            /// <summary>
            ///     The control will be able to receive mouse button events like <see cref="Pass"/>,
            ///     but the event will be stopped and handled even if the relevant events do not handle it.
            /// </summary>
            Stop = 2,
        }

#if GODOT
/// <summary>
/// Convenient helper to load a Godot scene without all the casting. Does NOT wrap the nodes (duh!).
/// </summary>
/// <param name="path">The resource path to the scene file to load.</param>
/// <returns>The root of the loaded scene.</returns>
        private protected static Godot.Control LoadScene(string path)
        {
            // See https://github.com/godotengine/godot/issues/21667 for why pNoCache is necessary.
            var scene2 = (Godot.PackedScene) Godot.ResourceLoader.Load(path, pNoCache: true);
            return (Godot.Control) scene2.Instance();
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
        [BaseTypeRequired(typeof(Control))]
        internal class ControlWrapAttribute : Attribute
        {
            public readonly Type GodotType;

            public ControlWrapAttribute(Type type)
            {
                GodotType = type;
            }
        }
        #endif
    }
}
