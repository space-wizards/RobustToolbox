using SS14.Client.GodotGlue;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared.IoC;
using System;
using System.Collections;
using System.Collections.Generic;
using SS14.Shared.Log;
using SS14.Shared.Interfaces.Reflection;
using System.Reflection;
using SS14.Shared.Maths;
using SS14.Client.Utility;
using SS14.Client.Graphics.Drawing;
using SS14.Shared.Utility;
using SS14.Client.Interfaces.ResourceManagement;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.Input;
using SS14.Client.ResourceManagement.ResourceTypes;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.ViewVariables;

namespace SS14.Client.UserInterface
{
    /// <summary>
    ///     A node in the GUI system.
    ///     NOTE: For docs, most of these are direct proxies to Godot's Control.
    ///     See the official docs for more help: https://godot.readthedocs.io/en/3.0/classes/class_control.html
    /// </summary>
    [PublicAPI]
    [ControlWrap(typeof(Godot.Control))]
    // ReSharper disable once RequiredBaseTypesIsNotInherited
    public partial class Control : IDisposable
    {
        /// <summary>
        ///     The name of this control.
        ///     Names must be unique between the control's siblings.
        /// </summary>
        [ViewVariables]
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

                var index = 0;
                if (Parent != null)
                {
                    if (Parent.HasChild(value))
                    {
                        throw new ArgumentException($"Parent already has a child with name {value}.");
                    }

                    index = Parent._children[_name].orderedIndex;
                    Parent._children.Remove(_name);
                }

                _name = value;

                if (GameController.OnGodot)
                {
                    SceneControl.SetName(_name);
                }

                if (Parent != null)
                {
                    Parent._children[_name] = (this, index);
                }
            }
        }

        private string _name;

        [ViewVariables]
        public Control Parent { get; private set; }

        internal IUserInterfaceManagerInternal UserInterfaceManagerInternal { get; }

        /// <summary>
        ///     The UserInterfaceManager we belong to, for convenience.
        /// </summary>
        public IUserInterfaceManager UserInterfaceManager => UserInterfaceManagerInternal;

        /// <summary>
        ///     Gets an enumerable over all the children of this control.
        /// </summary>
        [ViewVariables]
        public OrderedChildEnumerable Children => new OrderedChildEnumerable(this);

        [ViewVariables]
        public int ChildCount => _orderedChildren.Count;

        /// <summary>
        ///     The control's representation in Godot's scene tree.
        /// </summary>
        internal Godot.Control SceneControl => WrappedSceneControl;

        /// <summary>
        ///     Path to the .tscn file for this scene in the VFS.
        ///     This is mainly intended for content loading tscn files.
        ///     Don't use it from the engine.
        /// </summary>
        protected virtual ResourcePath ScenePath => null;

        private ControlWrap WrappedSceneControl;

        public const float ANCHOR_BEGIN = 0;
        public const float ANCHOR_END = 1;

        private float _anchorBottom;

        [ViewVariables]
        public float AnchorBottom
        {
            get => GameController.OnGodot ? SceneControl.AnchorBottom : _anchorBottom;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.AnchorBottom = value;
                }
                else
                {
                    _anchorBottom = value;
                    _updateLayout();
                }
            }
        }

        private float _anchorLeft;

        [ViewVariables]
        public float AnchorLeft
        {
            get => GameController.OnGodot ? SceneControl.AnchorLeft : _anchorLeft;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.AnchorLeft = value;
                }
                else
                {
                    _anchorLeft = value;
                    _updateLayout();
                }
            }
        }

        private float _anchorRight;

        [ViewVariables]
        public float AnchorRight
        {
            get => GameController.OnGodot ? SceneControl.AnchorRight : _anchorRight;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.AnchorRight = value;
                }
                else
                {
                    _anchorRight = value;
                    _updateLayout();
                }
            }
        }

        private float _anchorTop;

        [ViewVariables]
        public float AnchorTop
        {
            get => GameController.OnGodot ? SceneControl.AnchorTop : _anchorTop;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.AnchorTop = value;
                }
                else
                {
                    _anchorTop = value;
                    _updateLayout();
                }
            }
        }

        private float _marginRight;

        [ViewVariables]
        public float MarginRight
        {
            get => GameController.OnGodot ? SceneControl.MarginRight : _marginRight;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.MarginRight = value;
                }
                else
                {
                    _marginRight = value;
                    _updateLayout();
                }
            }
        }

        private float _marginLeft;

        [ViewVariables]
        public float MarginLeft
        {
            get => GameController.OnGodot ? SceneControl.MarginLeft : _marginLeft;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.MarginLeft = value;
                }
                else
                {
                    _marginLeft = value;
                    _updateLayout();
                }
            }
        }

        private float _marginTop;

        [ViewVariables]
        public float MarginTop
        {
            get => GameController.OnGodot ? SceneControl.MarginTop : _marginTop;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.MarginTop = value;
                }
                else
                {
                    _marginTop = value;
                    _updateLayout();
                }
            }
        }

        private float _marginBottom;

        [ViewVariables]
        public float MarginBottom
        {
            get => GameController.OnGodot ? SceneControl.MarginBottom : _marginBottom;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.MarginBottom = value;
                }
                else
                {
                    _marginBottom = value;
                    _updateLayout();
                }
            }
        }

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

        private bool _visible = true;

        [ViewVariables]
        public bool Visible
        {
            get => GameController.OnGodot ? SceneControl.Visible : _visible;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Visible = value;
                }
                else
                {
                    if (_visible == value)
                    {
                        return;
                    }
                    _visible = value;
                    OnVisibilityChanged?.Invoke(this);
                }
            }
        }

        // _marginSetSize is the size calculated by the margins,
        // but it's different from _size if min size is higher.
        private Vector2 _sizeByMargins;
        private Vector2 _size;

        [ViewVariables]
        public Vector2 Size
        {
            get => GameController.OnGodot ? SceneControl.GetSize().Convert() : _size;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.SetSize(value.Convert());
                }
                else
                {
                    var (diffX, diffY) = value - _sizeByMargins;
                    _marginRight += diffX;
                    _marginBottom += diffY;
                    _updateLayout();
                }
            }
        }

        public UIBox2 SizeBox => new UIBox2(Vector2.Zero, Size);
        public float Width => Size.X;
        public float Height => Size.Y;

        private Vector2 _position;

        [ViewVariables]
        public Vector2 Position
        {
            get => GameController.OnGodot ? SceneControl.GetPosition().Convert() : _position;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.SetPosition(value.Convert());
                }
                else
                {
                    var (diffX, diffY) = value - _position;
                    _marginTop += diffY;
                    _marginBottom += diffY;
                    _marginLeft += diffX;
                    _marginRight += diffX;
                    _updateLayout();
                }
            }
        }

        [ViewVariables]
        public Vector2 GlobalPosition
        {
            get
            {
                if (GameController.OnGodot)
                {
                    return SceneControl.GetPosition().Convert();
                }

                var offset = Position;
                var parent = Parent;
                while (parent != null)
                {
                    offset += parent.Position;
                    parent = parent.Parent;
                }

                return offset;
            }
        }

        public UIBox2 Rect => UIBox2.FromDimensions(_position, _size);

        public Vector2 Scale
        {
            get => GameController.OnGodot ? SceneControl.RectScale.Convert() : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.RectScale = value.Convert();
                }
            }
        }

        public string ToolTip
        {
            get => GameController.OnGodot ? SceneControl.GetTooltip() : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.SetTooltip(value);
                }
            }
        }

        private MouseFilterMode _mouseFilter = MouseFilterMode.Stop;

        [ViewVariables]
        public MouseFilterMode MouseFilter
        {
            get => GameController.OnGodot ? (MouseFilterMode) SceneControl.MouseFilter : _mouseFilter;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.MouseFilter = (Godot.Control.MouseFilterEnum) value;
                }
                else
                {
                    _mouseFilter = value;
                }
            }
        }

        private bool _canKeyboardFocus;

        [ViewVariables]
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

        public bool KeyboardFocusOnClick { get; set; }

        private SizeFlags _sizeFlagsH = SizeFlags.Fill;

        [ViewVariables]
        public SizeFlags SizeFlagsHorizontal
        {
            get => GameController.OnGodot ? (SizeFlags) SceneControl.SizeFlagsHorizontal : _sizeFlagsH;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.SizeFlagsHorizontal = (int) value;
                }
                else
                {
                    // TODO: Notify parent container.
                    _sizeFlagsH = value;
                }
            }
        }

        private float _sizeFlagsStretchRatio = 1;

        [ViewVariables]
        public float SizeFlagsStretchRatio
        {
            get => GameController.OnGodot ? SceneControl.SizeFlagsStretchRatio : _sizeFlagsStretchRatio;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.SizeFlagsStretchRatio = value;
                }
                else
                {
                    if (value <= 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be greater than zero.");
                    }

                    // TODO: Notify parent container.
                    _sizeFlagsStretchRatio = value;
                }
            }
        }

        private SizeFlags _sizeFlagsV = SizeFlags.Fill;

        [ViewVariables]
        public SizeFlags SizeFlagsVertical
        {
            get => GameController.OnGodot ? (SizeFlags) SceneControl.SizeFlagsVertical : _sizeFlagsV;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.SizeFlagsVertical = (int) value;
                }
                else
                {
                    // TODO: Notify parent container.
                    _sizeFlagsV = value;
                }
            }
        }

        private bool _rectClipContent = false;
        [ViewVariables]
        public bool RectClipContent
        {
            get => GameController.OnGodot ? SceneControl.RectClipContent : _rectClipContent;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.RectClipContent = _rectClipContent;
                }
                else
                {
                    _rectClipContent = value;
                }
            }
        }

        public Color? ModulateSelfOverride { get; set; }

        public Color Modulate
        {
            get => SceneControl.Modulate.Convert();
            set => SceneControl.Modulate = value.Convert();
        }

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
        ///     A combination of <see cref="CustomMinimumSize" /> and <see cref="CalculateMinimumSize" />,
        ///     Whichever is greater.
        ///     Use this for whenever you need the *actual* minimum size of something.
        /// </summary>
        [ViewVariables]
        public Vector2 CombinedMinimumSize
        {
            get
            {
                if (GameController.OnGodot)
                {
                    return SceneControl.GetCombinedMinimumSize().Convert();
                }
                else
                {
                    if (!_calculatedMinimumSize.HasValue)
                    {
                        _updateMinimumSize();
                        DebugTools.Assert(_calculatedMinimumSize.HasValue);
                    }

                    return Vector2.ComponentMax(CustomMinimumSize, _calculatedMinimumSize.Value);
                }
            }
        }

        private Vector2? _calculatedMinimumSize;
        private Vector2 _customMinimumSize;

        /// <summary>
        ///     A custom minimum size. If the control-calculated size is is smaller than this, this is used instead.
        /// </summary>
        /// <seealso cref="CalculateMinimumSize" />
        /// <seealso cref="CombinedMinimumSize" />
        [ViewVariables]
        public Vector2 CustomMinimumSize
        {
            get => GameController.OnGodot ? SceneControl.RectMinSize.Convert() : _customMinimumSize;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.RectMinSize = value.Convert();
                }
                else
                {
                    _customMinimumSize = Vector2.ComponentMax(Vector2.Zero, value);
                    MinimumSizeChanged();
                }
            }
        }

        public bool LayoutLocked => Parent is Container;

        private void _updateMinimumSize()
        {
            _calculatedMinimumSize = Vector2.ComponentMax(Vector2.Zero, CalculateMinimumSize());
        }

        public Vector2 GlobalMousePosition =>
            GameController.OnGodot
                ? SceneControl.GetGlobalMousePosition().Convert()
                : IoCManager.Resolve<IInputManager>().MouseScreenPosition;

        private readonly Dictionary<string, (Control, int orderedIndex)> _children =
            new Dictionary<string, (Control, int)>();

        private readonly List<Control> _orderedChildren = new List<Control>();

        public event Action<Control> OnMinimumSizeChanged;
        public event Action<Control> OnVisibilityChanged;

        /// <summary>
        ///     Default constructor.
        ///     The name of the control is decided based on type.
        /// </summary>
        public Control()
        {
            UserInterfaceManagerInternal = IoCManager.Resolve<IUserInterfaceManagerInternal>();

            if (GameController.OnGodot)
            {
                SetupSceneControl();
            }

            else
            {
                SetDefaults();
                if (ScenePath != null)
                {
                    _manualNodeSetup();
                }
            }

            Name = GetType().Name;
            Initialize();
            _applyPropertyMap();
            Restyle();
        }

        /// <param name="name">The name the component will have.</param>
        public Control(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name must not be null or whitespace.", nameof(name));
            }

            UserInterfaceManagerInternal = IoCManager.Resolve<IUserInterfaceManagerInternal>();

            if (GameController.OnGodot)
            {
                SetupSceneControl();
            }
            else
            {
                SetDefaults();
                if (ScenePath != null)
                {
                    _manualNodeSetup();
                }
            }

            Name = name;
            Initialize();
            _applyPropertyMap();
            Restyle();
        }


        /// <summary>
        ///     Wrap the provided Godot control with this one.
        ///     This does NOT set up parenting correctly!
        /// </summary>
        internal Control(Godot.Control control)
        {
            UserInterfaceManagerInternal = IoCManager.Resolve<IUserInterfaceManagerInternal>();
            _name = control.GetName();
            InjectControlWrap(control);
            SetupSignalHooks();
            //Logger.Debug($"Wrapping control {Name} ({GetType()} -> {control.GetType()})");
            Initialize();
            Restyle();
        }

        /// <summary>
        ///     Use this to do various initialization of the control.
        ///     Ranging from spawning children to prefetching them for later referencing.
        /// </summary>
        protected virtual void Initialize()
        {
        }

        protected virtual void SetDefaults()
        {
        }

        private static Dictionary<string, Type> _manualNodeTypeTranslations;

        private void _manualNodeSetup()
        {
            DebugTools.AssertNotNull(ScenePath);

            if (_manualNodeTypeTranslations == null)
            {
                _initManualNodeTypeTranslations();
            }

            DebugTools.AssertNotNull(_manualNodeTypeTranslations);

            var resourceCache = IoCManager.Resolve<IResourceCache>();
            var asset = (GodotAssetScene) resourceCache.GetResource<GodotAssetResource>(ScenePath).Asset;

            // Go over the inherited scenes with a stack,
            // because you can theoretically have very deep scene inheritance.
            var (_, inheritedSceneStack) = _manualFollowSceneInheritance(asset, resourceCache, false);

            _manualApplyInheritedSceneStack(this, inheritedSceneStack, asset, resourceCache);
        }

        private static void _manualApplyInheritedSceneStack(Control baseControl,
            Stack<GodotAssetScene> inheritedSceneStack, GodotAssetScene asset,
            IResourceCache resourceCache)
        {
            var parentMapping = new Dictionary<string, Control> {["."] = baseControl};
            var propertyMapping =
                new Dictionary<(string parent, string name), Dictionary<string, (object value, GodotAssetScene source)>
                >();

            // Go over the inherited scenes bottom-first.
            while (inheritedSceneStack.Count != 0)
            {
                var inheritedAsset = inheritedSceneStack.Pop();

                foreach (var node in inheritedAsset.Nodes)
                {
                    {
                        if (!propertyMapping.TryGetValue((node.Parent, node.Name), out var propMap))
                        {
                            propMap = new Dictionary<string, (object value, GodotAssetScene source)>();
                            propertyMapping[(node.Parent, node.Name)] = propMap;
                        }

                        foreach (var (key, value) in node.Properties)
                        {
                            propMap[key] = (value, inheritedAsset);
                        }
                    }

                    // It's the base control.
                    if (node.Parent == null)
                    {
                        continue;
                    }

                    Control childControl;
                    if (node.Type != null)
                    {
                        if (!_manualNodeTypeTranslations.TryGetValue(node.Type, out var type))
                        {
                            type = typeof(Control);
                        }

                        childControl = (Control) Activator.CreateInstance(type);
                        childControl.Name = node.Name;
                    }
                    else if (node.Instance != null)
                    {
                        var extResource = asset.GetExtResource(node.Instance.Value);
                        DebugTools.Assert(extResource.Type == "PackedScene");

                        if (_manualNodeTypeTranslations.TryGetValue(extResource.Path, out var type))
                        {
                            childControl = (Control) Activator.CreateInstance(type);
                        }
                        else
                        {
                            var subScene =
                                (GodotAssetScene) resourceCache
                                    .GetResource<GodotAssetResource>(
                                        GodotPathUtility.GodotPathToResourcePath(extResource.Path)).Asset;

                            childControl = ManualSpawnFromScene(subScene);
                        }

                        childControl.Name = node.Name;
                    }
                    else
                    {
                        // This happens if the node def is overriding properties of a node instantiated in an instance.
                        continue;
                    }

                    parentMapping[node.Parent].AddChild(childControl);
                    if (node.Parent == ".")
                    {
                        parentMapping[node.Name] = childControl;
                    }
                    else
                    {
                        parentMapping[$"{node.Parent}/{node.Name}"] = childControl;
                    }
                }
            }

            // Apply all the properties.
            foreach (var ((parent, nodeName), propMap) in propertyMapping)
            {
                Control node;
                switch (parent)
                {
                    case null:
                        // Base control, which isn't initialized yet, so defer until after Initialize().
                        baseControl._toApplyPropertyMapping = propMap;
                        continue;
                    case ".":
                        node = parentMapping[nodeName];
                        break;
                    default:
                        var parentNode = baseControl.GetChild(parent);
                        node = parentNode.GetChild(nodeName);
                        break;
                }

                // We need to defer this until AFTER Initialize() has ran because else everything blows up.
                foreach (var (key, (value, source)) in propMap)
                {
                    node.SetGodotProperty(key, value, source);
                }
            }
        }

        private Dictionary<string, (object value, GodotAssetScene source)>
            _toApplyPropertyMapping;

        private void _applyPropertyMap()
        {
            if (_toApplyPropertyMapping == null)
            {
                return;
            }

            foreach (var (key, (value, source)) in _toApplyPropertyMapping)
            {
                SetGodotProperty(key, value, source);
            }

            _toApplyPropertyMapping = null;
        }

        internal static Control ManualSpawnFromScene(GodotAssetScene scene)
        {
            if (_manualNodeTypeTranslations == null)
            {
                _initManualNodeTypeTranslations();
            }

            DebugTools.AssertNotNull(_manualNodeTypeTranslations);

            var resourceCache = IoCManager.Resolve<IResourceCache>();

            var (controlType, inheritedSceneStack) = _manualFollowSceneInheritance(scene, resourceCache, true);

            var control = (Control) Activator.CreateInstance(controlType);
            control.Name = scene.Nodes[0].Name;

            _manualApplyInheritedSceneStack(control, inheritedSceneStack, scene, resourceCache);

            return control;
        }

        private static (Type, Stack<GodotAssetScene>) _manualFollowSceneInheritance(GodotAssetScene scene,
            IResourceCache resourceCache, bool getType)
        {
            // Go over the inherited scenes with a stack,
            // because you can theoretically have very deep scene inheritance.
            var inheritedSceneStack = new Stack<GodotAssetScene>();
            inheritedSceneStack.Push(scene);

            Type controlType = null;

            while (scene.Nodes[0].Instance != null)
            {
                var extResource = scene.GetExtResource(scene.Nodes[0].Instance.Value);
                DebugTools.Assert(extResource.Type == "PackedScene");

                if (getType && _manualNodeTypeTranslations.TryGetValue(extResource.Path, out controlType))
                {
                    break;
                }

                scene = (GodotAssetScene) resourceCache.GetResource<GodotAssetResource>(
                    GodotPathUtility.GodotPathToResourcePath(extResource.Path)).Asset;

                inheritedSceneStack.Push(scene);
            }

            if (controlType == null)
            {
                if (!getType
                    || scene.Nodes[0].Type == null
                    || !_manualNodeTypeTranslations.TryGetValue(scene.Nodes[0].Type, out controlType))
                {
                    controlType = typeof(Control);
                }
            }

            return (controlType, inheritedSceneStack);
        }

        private static void _initManualNodeTypeTranslations()
        {
            DebugTools.AssertNull(_manualNodeTypeTranslations);

            _manualNodeTypeTranslations = new Dictionary<string, Type>();

            var reflectionManager = IoCManager.Resolve<IReflectionManager>();

            foreach (var type in reflectionManager.FindTypesWithAttribute<ControlWrapAttribute>())
            {
                var attr = type.GetCustomAttribute<ControlWrapAttribute>();
                if (attr.InstanceString != null)
                {
                    _manualNodeTypeTranslations[attr.InstanceString] = type;
                }

                if (attr.ConcreteType != null)
                {
                    _manualNodeTypeTranslations[attr.ConcreteType.Name] = type;
                }
            }
        }


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

            InjectControlWrap(newSceneControl);
            SetupSignalHooks();
            // Certain controls (LineEdit, WindowDialog, etc...) create sub controls automatically,
            // handle these.
            WrapChildControls();
        }

        // ASSUMING DIRECT CONTROL.
        private void InjectControlWrap(Godot.Control control)
        {
            // Inject wrapper script to hook virtual functions.
            // IMPORTANT: Because of how Scripts work in Godot,
            // it has to effectively "replace" the type of the control.
            // It... obviously cannot do this because this is [insert statically typed language].
            // As such: getting an instance to the control AFTER this point will yield a control of type ControlWrap.
            // UPDATE: As of 3.1 alpha 3, the following does not work:
            // Luckily, the old instance seems to still work flawlessy for everything, including signals!\
            // /UPDATE: so yes we need to use _Set and such now. Oh well.
            var script = Godot.GD.Load("res://ControlWrap.cs");

            // So... getting a new reference to ourselves is surprisingly difficult!
            if (control.GetChildCount() > 0)
            {
                // Potentially easiest: if we have a child, get the parent of our child (us).
                var child = control.GetChild(0);
                control.SetScript(script);
                WrappedSceneControl = (ControlWrap) child.GetParent();
            }
            else if (control.GetParent() != null)
            {
                // If not but we have a parent use that.
                var index = control.GetIndex();
                var parent = control.GetParent();
                control.SetScript(script);
                WrappedSceneControl = (ControlWrap) parent.GetChild(index);
            }
            else
            {
                // Ok so we're literally a lone node guess making a temporary child'll be fine.
                var node = new Godot.Node();
                control.AddChild(node);
                control.SetScript(script);
                WrappedSceneControl = (ControlWrap) node.GetParent();
                node.QueueFree();
            }

            WrappedSceneControl.GetMinimumSizeOverride = () => CalculateMinimumSize().Convert();
            WrappedSceneControl.HasPointOverride = point => HasPoint(point.Convert());
            WrappedSceneControl.DrawOverride = DoDraw;
        }

        private protected virtual void SetGodotProperty(string property, object value, GodotAssetScene context)
        {
            switch (property)
            {
                case "margin_left":
                    MarginLeft = (float) value;
                    break;
                case "margin_right":
                    MarginRight = (float) value;
                    break;
                case "margin_top":
                    MarginTop = (float) value;
                    break;
                case "margin_bottom":
                    MarginBottom = (float) value;
                    break;
                case "anchor_left":
                    AnchorLeft = (float) value;
                    break;
                case "anchor_right":
                    AnchorRight = (float) value;
                    break;
                case "anchor_bottom":
                    AnchorBottom = (float) value;
                    break;
                case "anchor_top":
                    AnchorTop = (float) value;
                    break;
                case "mouse_filter":
                    MouseFilter = (MouseFilterMode) (long) value;
                    break;
                case "size_flags_horizontal":
                    SizeFlagsHorizontal = (SizeFlags) (long) value;
                    break;
                case "size_flags_vertical":
                    SizeFlagsVertical = (SizeFlags) (long) value;
                    break;
                case "rect_clip_content":
                    RectClipContent = (bool) value;
                    break;
                case "rect_min_size":
                    CustomMinimumSize = (Vector2) value;
                    break;
            }
        }

        /// <summary>
        ///     Retrieves and instances the object pointed to by either a
        ///     sub resource or ext resource reference in a godot asset.
        /// </summary>
        /// <param name="context">The asset in which said object is referenced.</param>
        /// <param name="value">
        ///     The <see cref="GodotAsset.TokenSubResource"/> or <see cref="GodotAsset.TokenExtResource"/>
        /// </param>
        /// <typeparam name="T">
        ///     The expected type of the resource. This is not a godot type but our equivalent.
        /// </typeparam>
        private protected T GetGodotResource<T>(GodotAsset context, object value)
        {
            GodotAsset.ResourceDef def;
            (GodotAsset, int) defContext;
            // Retrieve the actual ResourceDef for the resource requested.
            if (value is GodotAsset.TokenExtResource ext)
            {
                var extRef = context.GetExtResource(ext);
                var resPath = GodotPathUtility.GodotPathToResourcePath(extRef.Path);
                var res = IoCManager.Resolve<IResourceCache>().GetResource<GodotAssetResource>(resPath);
                def = ((GodotAssetRes) res.Asset).MainResource;
                defContext = (res.Asset, 0);
            }
            else if (value is GodotAsset.TokenSubResource sub)
            {
                def = context.SubResources[(int) sub.ResourceId];
                defContext = (context, (int) sub.ResourceId);
            }
            else
            {
                throw new ArgumentException("Value must be a TokenExtResource or a TokenSubResource", nameof(value));
            }

            // See if we've cached it.
            if (UserInterfaceManagerInternal.GodotResourceInstanceCache.TryGetValue(defContext, out var result))
            {
                return (T) result;
            }

            // If not, here comes the mess of turning that into a native sane type.
            if (def.Type == "StyleBoxFlat")
            {
                var box = new StyleBoxFlat();
                if (def.Properties.TryGetValue("bg_color", out var val))
                {
                    box.BackgroundColor = (Color) val;
                }

                result = box;
            }
            else
            {
                throw new NotImplementedException();
            }

            UserInterfaceManagerInternal.GodotResourceInstanceCache[defContext] = result;
            return (T) result;
        }

        private void DoDraw()
        {
            using (var handle = new DrawingHandleScreen(SceneControl.GetCanvasItem()))
            {
                Draw(handle);
            }
        }

        protected internal virtual void Draw(DrawingHandleScreen handle)
        {
        }

        public void UpdateDraw()
        {
            if (GameController.OnGodot)
            {
                SceneControl.Update();
            }
        }

        /// <summary>
        ///     Overriden by child classes to change the Godot control type.
        ///     ONLY spawn the control in here. Use <see cref="SetSceneControl" /> for holding references to it.
        ///     This is to allow children to override it without breaking the setting.
        /// </summary>
        private protected virtual Godot.Control SpawnSceneControl()
        {
            return new Godot.Control();
        }

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


            if (GameController.OnGodot)
            {
                DisposeSignalHooks();
            }

            if (GameController.OnGodot && !GameController.ShuttingDownHard)
            {
                WrappedSceneControl?.QueueFree();
                WrappedSceneControl?.Dispose();
                WrappedSceneControl = null;
            }
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
        public void AddChild(Control child, bool LegibleUniqueName = false)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));
            if (child.Parent != null)
            {
                throw new InvalidOperationException("This component is still parented. Deparent it before adding it.");
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

            var i = 0;
            var origChildName = child.Name;
            var childName = origChildName;
            while (_children.ContainsKey(childName))
            {
                childName = $"{origChildName}_{++i}";
            }

            if (origChildName != childName)
            {
                child.Name = childName;
            }

            if (GameController.OnGodot)
            {
                SceneControl.AddChild(child.SceneControl, LegibleUniqueName);
                // Godot changes the name automatically if you would cause a naming conflict.
                child._name = child.SceneControl.GetName();
            }

            child.Parent = this;
            _children[child.Name] = (child, _orderedChildren.Count);
            _orderedChildren.Add(child);

            child.Parented(this);
            ChildAdded(child);
        }

        protected virtual void ChildAdded(Control newChild)
        {
        }

        /// <summary>
        ///     Called when this control gets made a child of a different control.
        /// </summary>
        /// <param name="newParent">The new parent component.</param>
        protected virtual void Parented(Control newParent)
        {
            MinimumSizeChanged();
            Restyle();
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
            if (!_children.ContainsKey(child.Name) || _children[child.Name].Item1 != child)
            {
                throw new InvalidOperationException("The provided control is not a direct child of this control.");
            }

            var index = _children[child.Name].orderedIndex;
            _orderedChildren.RemoveAt(index);
            _children.Remove(child.Name);
            _updateChildIndices();

            child.Parent = null;
            if (GameController.OnGodot)
            {
                SceneControl.RemoveChild(child.SceneControl);
            }

            child.Deparented();
            ChildRemoved(child);
        }

        protected virtual void ChildRemoved(Control child)
        {
        }

        /// <summary>
        ///     Called when this control is removed as child from the former parent.
        /// </summary>
        protected virtual void Deparented()
        {
            Restyle();
        }

        protected virtual void ChildMoved(Control child, int oldIndex, int newIndex)
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
            if (GameController.OnGodot)
            {
                SceneControl.MinimumSizeChanged();
                return;
            }

            _calculatedMinimumSize = null;
            _updateLayout();
            OnMinimumSizeChanged?.Invoke(this);
        }

        protected internal virtual bool HasPoint(Vector2 point)
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

        public Control GetChild(int index)
        {
            return _orderedChildren[index];
        }

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
                child = (T) control.Item1;
                return true;
            }

            child = default;
            return false;
        }

        public bool TryGetChild(string name, out Control child)
        {
            if (_children.TryGetValue(name, out var childEntry))
            {
                child = childEntry.Item1;
                return true;
            }

            child = default;
            return false;
        }

        public bool HasChild(string name)
        {
            return _children.ContainsKey(name);
        }

        public int GetPositionInParent()
        {
            if (Parent == null)
            {
                throw new InvalidOperationException("This control has no parent!");
            }

            return Parent._children[Name].orderedIndex;
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

            if (GameController.OnGodot)
            {
                // Because some controls spawn other sub nodes like timers,
                // This position doesn't map to the scene tree.
                var parentScene = Parent.SceneControl;
                var siblingCount = parentScene.GetChildCount();
                var counter = 0;
                for (var i = 0; i < siblingCount; i++)
                {
                    var sibling = parentScene.GetChild(i);
                    // Use a counter that counts the controls to figure out the position to move to.
                    if (sibling is Godot.Control)
                    {
                        if (counter == position)
                        {
                            parentScene.MoveChild(SceneControl, counter);
                            break;
                        }

                        counter += 1;
                    }
                }
            }

            Parent._orderedChildren.RemoveAt(posInParent);
            Parent._orderedChildren.Insert(position, this);
            Parent._updateChildIndices();
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
            SetPositionInParent(Parent.ChildCount-1);
        }

        /// <summary>
        ///     Called when this control receives focus.
        /// </summary>
        protected internal virtual void FocusEntered()
        {
        }

        /// <summary>
        ///     Called when this control loses focus.
        /// </summary>
        protected internal virtual void FocusExited()
        {
        }

        public bool HasKeyboardFocus()
        {
            return GameController.OnGodot ? SceneControl.HasFocus() : UserInterfaceManager.KeyboardFocused == this;
        }

        public void GrabKeyboardFocus()
        {
            if (GameController.OnGodot)
            {
                SceneControl.GrabFocus();
            }
            else
            {
                UserInterfaceManager.GrabKeyboardFocus(this);
            }
        }

        public void ReleaseKeyboardFocus()
        {
            if (GameController.OnGodot)
            {
                SceneControl?.ReleaseFocus();
            }
            else
            {
                UserInterfaceManager.ReleaseKeyboardFocus(this);
            }
        }

        protected virtual void Resized()
        {
        }

        internal static Control InstanceScene(string resourcePath)
        {
            var res = (Godot.PackedScene) Godot.ResourceLoader.Load(resourcePath);
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
                parent._children[newControl.Name] = (newControl, parent._orderedChildren.Count);
                parent._orderedChildren.Add(newControl);
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

                var godotType = attr.ConcreteType;
                if (godotType == null)
                {
                    continue;
                }

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

        public void SetAnchorAndMarginPreset(LayoutPreset preset, LayoutPresetMode mode = LayoutPresetMode.MinSize,
            int margin = 0)
        {
            SetAnchorPreset(preset);
            SetMarginsPreset(preset, mode, margin);
        }

        /// <summary>
        ///     Changes all the anchors of a node at once to common presets.
        /// </summary>
        /// <param name="preset">
        ///     The preset to apply to the anchors.
        /// </param>
        /// <param name="keepMargin">
        ///     If this is true, the control margins themselves will not be changed,
        ///     and the control position will change according to the new anchor parameters.
        ///     If false, the control margins will adjust so that the control position remains the same relative to its parent.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown if <paramref name="preset"/> isn't a valid preset value.
        /// </exception>
        public void SetAnchorPreset(LayoutPreset preset, bool keepMargin = false)
        {
            if (GameController.OnGodot)
            {
                SceneControl.SetAnchorsPreset((Godot.Control.LayoutPreset) preset, keepMargin);
                return;
            }

            // Left Anchor.
            switch (preset)
            {
                case LayoutPreset.TopLeft:
                case LayoutPreset.BottomLeft:
                case LayoutPreset.CenterLeft:
                case LayoutPreset.LeftWide:
                case LayoutPreset.HorizontalCenterWide:
                case LayoutPreset.Wide:
                case LayoutPreset.TopWide:
                case LayoutPreset.BottomWide:
                    AnchorLeft = 0;
                    break;
                case LayoutPreset.CenterTop:
                case LayoutPreset.CenterBottom:
                case LayoutPreset.Center:
                case LayoutPreset.VerticalCenterWide:
                    AnchorLeft = 0.5f;
                    break;
                case LayoutPreset.TopRight:
                case LayoutPreset.BottomRight:
                case LayoutPreset.CenterRight:
                case LayoutPreset.RightWide:
                    AnchorLeft = 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }

            // Top Anchor.
            switch (preset)
            {
                case LayoutPreset.TopLeft:
                case LayoutPreset.TopRight:
                case LayoutPreset.LeftWide:
                case LayoutPreset.TopWide:
                case LayoutPreset.Wide:
                case LayoutPreset.RightWide:
                case LayoutPreset.CenterTop:
                case LayoutPreset.VerticalCenterWide:
                    AnchorTop = 0;
                    break;
                case LayoutPreset.CenterLeft:
                case LayoutPreset.CenterRight:
                case LayoutPreset.HorizontalCenterWide:
                case LayoutPreset.Center:
                    AnchorTop = 0.5f;
                    break;
                case LayoutPreset.CenterBottom:
                case LayoutPreset.BottomLeft:
                case LayoutPreset.BottomRight:
                case LayoutPreset.BottomWide:
                    AnchorTop = 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }

            // Right Anchor.
            switch (preset)
            {
                case LayoutPreset.TopLeft:
                case LayoutPreset.CenterLeft:
                case LayoutPreset.BottomLeft:
                case LayoutPreset.LeftWide:
                    AnchorRight = 0;
                    break;
                case LayoutPreset.CenterTop:
                case LayoutPreset.CenterBottom:
                case LayoutPreset.Center:
                case LayoutPreset.VerticalCenterWide:
                    AnchorRight = 0.5f;
                    break;
                case LayoutPreset.CenterRight:
                case LayoutPreset.TopRight:
                case LayoutPreset.Wide:
                case LayoutPreset.HorizontalCenterWide:
                case LayoutPreset.TopWide:
                case LayoutPreset.BottomWide:
                case LayoutPreset.RightWide:
                case LayoutPreset.BottomRight:
                    AnchorRight = 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }

            // Bottom Anchor.
            switch (preset)
            {
                case LayoutPreset.TopWide:
                case LayoutPreset.TopLeft:
                case LayoutPreset.TopRight:
                case LayoutPreset.CenterTop:
                    AnchorBottom = 0;
                    break;
                case LayoutPreset.CenterLeft:
                case LayoutPreset.CenterRight:
                case LayoutPreset.Center:
                case LayoutPreset.HorizontalCenterWide:
                    AnchorBottom = 0.5f;
                    break;
                case LayoutPreset.CenterBottom:
                case LayoutPreset.BottomLeft:
                case LayoutPreset.BottomRight:
                case LayoutPreset.LeftWide:
                case LayoutPreset.Wide:
                case LayoutPreset.RightWide:
                case LayoutPreset.VerticalCenterWide:
                case LayoutPreset.BottomWide:
                    AnchorBottom = 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }
        }

        /// <param name="preset"></param>
        /// <param name="resizeMode"></param>
        /// <param name="margin">Some extra margin to add depending on the preset chosen.</param>
        public void SetMarginsPreset(LayoutPreset preset, LayoutPresetMode resizeMode = LayoutPresetMode.MinSize,
            int margin = 0)
        {
            if (GameController.OnGodot)
            {
                SceneControl.SetMarginsPreset((Godot.Control.LayoutPreset) preset,
                    (Godot.Control.LayoutPresetMode) resizeMode, margin);
                return;
            }

            var newSize = Size;
            var minSize = CombinedMinimumSize;
            if ((resizeMode & LayoutPresetMode.KeepWidth) == 0)
            {
                newSize = new Vector2(minSize.X, newSize.Y);
            }

            if ((resizeMode & LayoutPresetMode.KeepHeight) == 0)
            {
                newSize = new Vector2(newSize.X, minSize.Y);
            }

            var parentSize = Parent?.Size ?? Vector2.Zero;

            // Left Margin.
            switch (preset)
            {
                case LayoutPreset.TopLeft:
                case LayoutPreset.BottomLeft:
                case LayoutPreset.CenterLeft:
                case LayoutPreset.LeftWide:
                case LayoutPreset.HorizontalCenterWide:
                case LayoutPreset.Wide:
                case LayoutPreset.TopWide:
                case LayoutPreset.BottomWide:
                    // The AnchorLeft bit is to reverse the effect of anchors,
                    // So that the preset result is the same no matter what margins are set.
                    _marginLeft = parentSize.X * (0 - AnchorLeft) + margin;
                    break;
                case LayoutPreset.CenterTop:
                case LayoutPreset.CenterBottom:
                case LayoutPreset.Center:
                case LayoutPreset.VerticalCenterWide:
                    _marginLeft = parentSize.X * (0.5f - AnchorLeft) - newSize.X / 2;
                    break;
                case LayoutPreset.TopRight:
                case LayoutPreset.BottomRight:
                case LayoutPreset.CenterRight:
                case LayoutPreset.RightWide:
                    _marginLeft = parentSize.X * (1 - AnchorLeft) - newSize.X - margin;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }

            // Top Anchor.
            switch (preset)
            {
                case LayoutPreset.TopLeft:
                case LayoutPreset.TopRight:
                case LayoutPreset.LeftWide:
                case LayoutPreset.TopWide:
                case LayoutPreset.Wide:
                case LayoutPreset.RightWide:
                case LayoutPreset.CenterTop:
                case LayoutPreset.VerticalCenterWide:
                    _marginTop = parentSize.Y * (0 - AnchorTop) + margin;
                    break;
                case LayoutPreset.CenterLeft:
                case LayoutPreset.CenterRight:
                case LayoutPreset.HorizontalCenterWide:
                case LayoutPreset.Center:
                    _marginTop = parentSize.Y * (0.5f - AnchorTop) - newSize.Y / 2;
                    break;
                case LayoutPreset.CenterBottom:
                case LayoutPreset.BottomLeft:
                case LayoutPreset.BottomRight:
                case LayoutPreset.BottomWide:
                    _marginTop = parentSize.Y * (1 - AnchorTop) - newSize.Y - margin;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }

            // Right Anchor.
            switch (preset)
            {
                case LayoutPreset.TopLeft:
                case LayoutPreset.CenterLeft:
                case LayoutPreset.BottomLeft:
                case LayoutPreset.LeftWide:
                    _marginRight = parentSize.X * (0 - AnchorRight) + newSize.X + margin;
                    break;
                case LayoutPreset.CenterTop:
                case LayoutPreset.CenterBottom:
                case LayoutPreset.Center:
                case LayoutPreset.VerticalCenterWide:
                    _marginRight = parentSize.X * (0.5f - AnchorRight) + newSize.X;
                    break;
                case LayoutPreset.CenterRight:
                case LayoutPreset.TopRight:
                case LayoutPreset.Wide:
                case LayoutPreset.HorizontalCenterWide:
                case LayoutPreset.TopWide:
                case LayoutPreset.BottomWide:
                case LayoutPreset.RightWide:
                case LayoutPreset.BottomRight:
                    _marginRight = parentSize.X * (1 - AnchorRight) - margin;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }

            // Bottom Anchor.
            switch (preset)
            {
                case LayoutPreset.TopWide:
                case LayoutPreset.TopLeft:
                case LayoutPreset.TopRight:
                case LayoutPreset.CenterTop:
                    _marginBottom = parentSize.Y * (0 - AnchorBottom) + newSize.Y + margin;
                    break;
                case LayoutPreset.CenterLeft:
                case LayoutPreset.CenterRight:
                case LayoutPreset.Center:
                case LayoutPreset.HorizontalCenterWide:
                    _marginBottom = parentSize.Y * (0.5f - AnchorBottom) + newSize.Y;
                    break;
                case LayoutPreset.CenterBottom:
                case LayoutPreset.BottomLeft:
                case LayoutPreset.BottomRight:
                case LayoutPreset.LeftWide:
                case LayoutPreset.Wide:
                case LayoutPreset.RightWide:
                case LayoutPreset.VerticalCenterWide:
                case LayoutPreset.BottomWide:
                    _marginBottom = parentSize.Y * (1 - AnchorBottom) - margin;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }

            _updateLayout();
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

        /// <seealso cref="Control.SetMarginsPreset"/>
        [Flags]
        [PublicAPI]
        public enum LayoutPresetMode : byte
        {
            /// <summary>
            ///     Reset control size to minimum size.
            /// </summary>
            MinSize = 0,

            /// <summary>
            ///     Reset height to minimum but keep width the same.
            /// </summary>
            KeepWidth = 1,

            /// <summary>
            ///     Reset width to minimum but keep height the same.
            /// </summary>
            KeepHeight = 2,

            /// <summary>
            ///     Do not modify control size at all.
            /// </summary>
            KeepSize = KeepWidth | KeepHeight,
        }

        /// <summary>
        ///     Controls how a control changes size when inside a container.
        /// </summary>
        [Flags]
        [PublicAPI]
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
            if (!GameController.OnGodot)
            {
                return;
            }

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
        }

        protected Color? GetColorOverride(string name)
        {
            if (!GameController.OnGodot)
            {
                return default;
            }

            return SceneControl.HasColorOverride(name) ? SceneControl.GetColor(name).Convert() : (Color?) null;
        }

        protected void SetConstantOverride(string name, int? constant)
        {
            if (!GameController.OnGodot)
            {
                return;
            }

            if (constant != null)
            {
                SceneControl.AddConstantOverride(name, constant.Value);
            }
            else
            {
                SceneControl.Set($"custom_constants/{name}", null);
            }
        }

        protected int? GetConstantOverride(string name)
        {
            if (!GameController.OnGodot)
            {
                return default;
            }

            return SceneControl.HasConstantOverride(name) ? SceneControl.GetConstant(name) : (int?) null;
        }

        protected void SetStyleBoxOverride(string name, StyleBox styleBox)
        {
            if (!GameController.OnGodot)
            {
                return;
            }

            SceneControl.AddStyleboxOverride(name, styleBox.GodotStyleBox);
        }

        protected StyleBox GetStyleBoxOverride(string name)
        {
            if (!GameController.OnGodot)
            {
                return default;
            }

            var box = SceneControl.HasStyleboxOverride(name) ? SceneControl.GetStylebox(name) : null;
            return box == null ? null : new GodotStyleBoxWrap(box);
        }

        protected void SetFontOverride(string name, Font font)
        {
            if (!GameController.OnGodot)
            {
                return;
            }

            SceneControl.AddFontOverride(name, font);
        }

        protected Font GetFontOverride(string name)
        {
            if (!GameController.OnGodot)
            {
                return default;
            }

            var font = SceneControl.HasFontOverride(name) ? SceneControl.GetFont(name) : null;
            return font == null ? null : new GodotWrapFont(font);
        }

        internal void DoUpdate(ProcessFrameEventArgs args)
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
        protected virtual void Update(ProcessFrameEventArgs args)
        {
        }

        internal void DoFrameUpdate(RenderFrameEventArgs args)
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
        /// <param name="args"></param>
        protected virtual void FrameUpdate(RenderFrameEventArgs args)
        {
        }

        private void _updateLayout(bool immediate = false)
        {
            _doUpdateLayout();
        }

        private void _doUpdateLayout()
        {
            if (GameController.OnGodot)
            {
                return;
            }

            var (pSizeX, pSizeY) = Parent?._size ?? Vector2.Zero;

            var top = _anchorTop * pSizeY + _marginTop;
            var left = _anchorLeft * pSizeX + _marginLeft;
            var right = _anchorRight * pSizeX + _marginRight;
            var bottom = _anchorBottom * pSizeY + _marginBottom;

            _position = new Vector2(left, top);
            var oldSize = _size;
            _sizeByMargins = new Vector2(right - left, bottom - top);
            _size = Vector2.ComponentMax(_sizeByMargins, CombinedMinimumSize);

            if (_size != oldSize)
            {
                Resized();
            }

            foreach (var child in _orderedChildren)
            {
                child._doUpdateLayout();
            }
        }

        /// <summary>
        ///     Updates the indices stored inside <see cref="_children"/>.
        /// </summary>
        private void _updateChildIndices()
        {
            for (var i = 0; i < _orderedChildren.Count; i++)
            {
                var child = _orderedChildren[i];
                _children[child._name] = (child, i);
            }
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
            get => GameController.OnGodot ? (CursorShape) SceneControl.GetDefaultCursorShape() : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.SetDefaultCursorShape((Godot.Control.CursorShape) value);
                }
            }
        }

        public override string ToString()
        {
            return $"{Name} ({GetType().Name})";
        }

        /// <summary>
        ///     Mode that will be tested when testing controls to invoke mouse button events on.
        /// </summary>
        public enum MouseFilterMode
        {
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
            Stop = 0,

            /// <summary>
            ///     The control will not be considered at all, and will not have any effects.
            /// </summary>
            Ignore = 2,
        }

        /// <summary>
        /// Convenient helper to load a Godot scene without all the casting. Does NOT wrap the nodes (duh!).
        /// </summary>
        /// <param name="path">The resource path to the scene file to load.</param>
        /// <returns>The root of the loaded scene.</returns>
        private protected static Godot.Control LoadScene(string path)
        {
            var scene2 = (Godot.PackedScene) Godot.ResourceLoader.Load(path);
            return (Godot.Control) scene2.Instance();
        }

        public readonly struct OrderedChildEnumerable : IEnumerable<Control>
        {
            private readonly Control Owner;

            public OrderedChildEnumerable(Control owner)
            {
                Owner = owner;
            }

            public List<Control>.Enumerator GetEnumerator()
            {
                return Owner._orderedChildren.GetEnumerator();
            }

            IEnumerator<Control> IEnumerable<Control>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        [AttributeUsage(AttributeTargets.Class, Inherited = false)]
        [BaseTypeRequired(typeof(Control))]
        internal class ControlWrapAttribute : Attribute
        {
            public readonly string InstanceString;
            public readonly Type ConcreteType;

            public ControlWrapAttribute(Type concreteType)
            {
                ConcreteType = concreteType;
            }

            public ControlWrapAttribute(Type concreteType, string instanceString)
            {
                ConcreteType = concreteType;
                InstanceString = instanceString;
            }

            public ControlWrapAttribute(string instanceString)
            {
                InstanceString = instanceString;
            }
        }
    }
}
