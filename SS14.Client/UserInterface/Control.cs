using SS14.Client.GodotGlue;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using SS14.Shared.Log;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.ContentPack;
using System.Reflection;
using SS14.Shared.Maths;
using SS14.Client.Utility;

namespace SS14.Client.UserInterface
{
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
                SceneControl.SetName(_name);

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

        public bool Visible
        {
            get => SceneControl.Visible;
            set => SceneControl.Visible = value;
        }

        public Vector2 Size
        {
            get => SceneControl.GetSize().Convert();
            set => SceneControl.SetSize(value.Convert());
        }

        public Vector2 Position
        {
            get => SceneControl.GetPosition().Convert();
            set => SceneControl.SetPosition(value.Convert());
        }

        public Box2 Rect
        {
            get => SceneControl.GetRect().Convert();
        }

        public Vector2 MinimumSize => SceneControl.GetMinimumSize().Convert();

        public Vector2 GlobalMousePosition
        {
            get => SceneControl.GetGlobalMousePosition().Convert();
        }

        private readonly Dictionary<string, Control> _children = new Dictionary<string, Control>();

        /// <summary>
        ///     Default constructor.
        ///     The name of the control is decided based on type.
        /// </summary>
        public Control()
        {
            UserInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();
            SetupSceneControl();
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
            SetupSceneControl();
            Name = name;
            Initialize();
        }

        /// <summary>
        ///     Wrap the provided Godot control with this one.
        ///     This does NOT set up parenting correctly!
        /// </summary>
        public Control(Godot.Control control)
        {
            SetSceneControl(control);
            UserInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();
            _name = control.GetName();
            SetupSignalHooks();
            //Logger.Debug($"Wrapping control {Name} ({GetType()} -> {control.GetType()})");
            Initialize();
        }

        protected virtual void Initialize()
        {
        }

        private void SetupSceneControl()
        {
            SetSceneControl(SpawnSceneControl());
            SetupSignalHooks();
            // Certain controls (LineEdit, WindowDialog, etc...) create sub controls automatically,
            // handle these.
            WrapChildControls();
        }

        /// <summary>
        ///     Overriden by child classes to change the Godot control type.
        /// </summary>
        /// <returns></returns>
        protected virtual Godot.Control SpawnSceneControl()
        {
            return new Godot.Control();
        }

        protected virtual void SetSceneControl(Godot.Control control)
        {
            SceneControl = control;
        }

        protected bool Disposed { get; private set; } = false;

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

            DisposeSignalHooks();

            SceneControl.QueueFree();
            SceneControl.Dispose();
            SceneControl = null;
        }

        ~Control()
        {
            Dispose(false);
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
        public virtual void AddChild(Control child, bool LegibleUniqueName = false)
        {
            if (child.Parent != null)
            {
                throw new InvalidOperationException("This component is still parented. Deparent it before adding it.");
            }

            child.Parent = this;
            child.Parented(this);
            SceneControl.AddChild(child.SceneControl, LegibleUniqueName);
            // Godot changes the name automtically if you would cause a naming conflict.
            if (child.SceneControl.GetName() != child._name)
            {
                child._name = child.SceneControl.GetName();
            }
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
            SceneControl.RemoveChild(child.SceneControl);
        }

        /// <summary>
        ///     Called when this control is removed as child from the former parent.
        /// </summary>
        protected virtual void Deparented()
        {
        }

        public T GetChild<T>(string name) where T : Control
        {
            return (T)GetChild(name);
        }

        public Control GetChild(string name)
        {
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
                child = (T)control;
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
            return SceneControl.HasFocus();
        }

        public void GrabFocus()
        {
            SceneControl.GrabFocus();
        }

        public void ReleaseFocus()
        {
            SceneControl?.ReleaseFocus();
        }

        public static Control InstanceScene(string resourcePath)
        {
            var res = (Godot.PackedScene)Godot.ResourceLoader.Load(resourcePath);
            return InstanceScene(res);
        }

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
        public static Control InstanceScene(Godot.PackedScene scene)
        {
            var root = (Godot.Control)scene.Instance();
            return WrapGodotControl(null, root);
        }

        private static Control WrapGodotControl(Control parent, Godot.Control control)
        {
            var type = FindGodotTranslationType(control);
            var newControl = (Control)Activator.CreateInstance(type, control);

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

            throw new InvalidOperationException("Managed to pass Godot.Control when finding translations. This should be impossible!");
        }

        private static void SetupGodotTranslationCache()
        {
            GodotTranslationCache = new Dictionary<Type, Type>();
            var refl = IoCManager.Resolve<IReflectionManager>();
            var godotAsm = AppDomain.CurrentDomain.GetAssemblyByName("GodotSharp");
            foreach (var childType in refl.GetAllChildren<Control>(inclusive: true))
            {
                var childName = childType.Name;
                var godotType = godotAsm.GetType($"Godot.{childName}");
                if (godotType == null)
                {
                    Logger.Debug($"Unable to find Godot type for {childType}.");
                    continue;
                }

                if (GodotTranslationCache.TryGetValue(godotType, out var dupe))
                {
                    Logger.Error($"Found multiple SS14 Control types pointing to a single Godot Control type. Godot: {godotType}, first: {dupe}, second: {childType}");
                    continue;
                }

                GodotTranslationCache[godotType] = childType;
            }

            if (!GodotTranslationCache.ContainsKey(typeof(Godot.Control)))
            {
                GodotTranslationCache = null;
                throw new InvalidOperationException("We don't even have the base Godot Control in the translation cache. We can't use scene instancing like this!");
            }
        }

        public void SetAnchorPreset(LayoutPreset preset, bool keepMargin = false)
        {
            SceneControl.SetAnchorsPreset((Godot.Control.LayoutPreset)preset, keepMargin);
        }

        public enum LayoutPreset : byte
        {
            TopLeft = Godot.Control.LayoutPreset.TopLeft,
            TopRight = Godot.Control.LayoutPreset.TopRight,
            BottomLeft = Godot.Control.LayoutPreset.BottomLeft,
            BottomRight = Godot.Control.LayoutPreset.BottomRight,
            CenterLeft = Godot.Control.LayoutPreset.CenterLeft,
            CenterTop = Godot.Control.LayoutPreset.CenterTop,
            CenterRight = Godot.Control.LayoutPreset.CenterRight,
            CenterBottom = Godot.Control.LayoutPreset.CenterBottom,
            Center = Godot.Control.LayoutPreset.Center,
            LeftWide = Godot.Control.LayoutPreset.LeftWide,
            TopWide = Godot.Control.LayoutPreset.TopWide,
            RightWide = Godot.Control.LayoutPreset.RightWide,
            BottomWide = Godot.Control.LayoutPreset.BottomWide,
            VerticalCenterWide = Godot.Control.LayoutPreset.VcenterWide,
            HorizontalCenterWide = Godot.Control.LayoutPreset.VcenterWide,
            Wide = Godot.Control.LayoutPreset.Wide,
        }

        public void AddColorOverride(string name, Color color)
        {
            SceneControl.AddColorOverride(name, color.Convert());
        }

        public void AddConstantOverride(string name, int constant)
        {
            SceneControl.AddConstantOverride(name, constant);
        }

        public void DoUpdate(FrameEventArgs args)
        {
            Update(args);
            foreach (var child in Children)
            {
                child.DoUpdate(args);
            }
        }

        protected virtual void Update(FrameEventArgs args)
        {
        }

        public enum CursorShape
        {
            Arrow = Godot.Control.CursorShape.Arrow,
            IBeam = Godot.Control.CursorShape.Ibeam,
            PointingHand = Godot.Control.CursorShape.PointingHand,
            Cross = Godot.Control.CursorShape.Cross,
            Wait = Godot.Control.CursorShape.Wait,
            Busy = Godot.Control.CursorShape.Busy,
            Drag = Godot.Control.CursorShape.Drag,
            CanDrop = Godot.Control.CursorShape.CanDrop,
            Forbidden = Godot.Control.CursorShape.Forbidden,
            VSize = Godot.Control.CursorShape.Vsize,
            HSize = Godot.Control.CursorShape.Hsize,
            BDiagSize = Godot.Control.CursorShape.Bdiagsize,
            FDiagSize = Godot.Control.CursorShape.Fdiagsize,
            Move = Godot.Control.CursorShape.Move,
            VSplit = Godot.Control.CursorShape.Vsplit,
            HSplit = Godot.Control.CursorShape.Hsplit,
            Help = Godot.Control.CursorShape.Help,
        }

        public CursorShape DefaultCursorShape
        {
            get => (CursorShape)SceneControl.GetDefaultCursorShape();
            set => SceneControl.SetDefaultCursorShape((Godot.Control.CursorShape)value);
        }

        /// <summary>
        ///     Mode that will be tested when testing controls to invoke mouse button events on.
        /// </summary>
        public enum MouseFilterMode
        {
            /// <summary>
            ///     The control will not be considered at all, and will not have any effects.
            /// </summary>
            Ignore = Godot.Control.MouseFilterEnum.Ignore,
            /// <summary>
            ///     The control will be able to receive mouse buttons events.
            ///     Furthermore, if a control with this mode does get clicked,
            ///     the event automatically gets marked as handled.
            /// </summary>
            Pass = Godot.Control.MouseFilterEnum.Pass,
            /// <summary>
            ///     The control will be able to receive mouse button events like <see cref="Pass"/>,
            ///     but the event will be stopped and handled even if the relevant events do not handle it.
            /// </summary>
            Stop = Godot.Control.MouseFilterEnum.Stop,
        }

        public MouseFilterMode MouseFilter
        {
            get => (MouseFilterMode)SceneControl.MouseFilter;
            set => SceneControl.MouseFilter = (Godot.Control.MouseFilterEnum)value;
        }

        /// <summary>
        /// Convenient helper to load a Godot scene without all the casting. Does NOT wrap the nodes (duh!).
        /// </summary>
        /// <param name="path">The resource path to the scene file to load.</param>
        /// <returns>The root of the loaded scene.</returns>
        protected static Godot.Control LoadScene(string path)
        {
            var res = (Godot.PackedScene)Godot.ResourceLoader.Load(path);
            return (Godot.Control)res.Instance();
        }

        public Vector2 Scale
        {
            get => SceneControl.RectScale.Convert();
            set => SceneControl.RectScale = value.Convert();
        }
    }
}
