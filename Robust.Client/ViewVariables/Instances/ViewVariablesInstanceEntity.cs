using System.Linq;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.ViewVariables.Editors;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.ViewVariables.Instances
{
    // TODO:
    // You're gonna hate me for this, future person.
    // This code is already legacy and due for a refactor.
    // Yes, there have been exactly 2 PRs relating to it.
    // Oh well.
    // Anyways, the different tabs in the entity view need to be moved to traits,
    // and this class basically made a child of InstanceObject for the code that doesn't fit in a trait.

    internal class ViewVariablesInstanceEntity : ViewVariablesInstance
    {
        private readonly IEntityManager _entityManager;

        private const int TabClientVars = 0;
        private const int TabClientComponents = 1;
        private const int TabServerVars = 2;
        private const int TabServerComponents = 3;

        private TabContainer _tabs;
        private IEntity _entity;

        private ViewVariablesRemoteSession _entitySession;

        private ViewVariablesBlobMembers _membersBlob;

        private VBoxContainer _serverVariables;
        private VBoxContainer _serverComponents;

        private bool _serverLoaded;

        public ViewVariablesInstanceEntity(IViewVariablesManagerInternal vvm, IResourceCache resCache, IEntityManager entityManager) : base(vvm, resCache)
        {
            _entityManager = entityManager;
        }

        public override void Initialize(SS14Window window, object obj)
        {
            _entity = (IEntity) obj;

            var type = obj.GetType();

            var scrollContainer = new ScrollContainer();
            //scrollContainer.SetAnchorPreset(Control.LayoutPreset.Wide, true);
            window.Contents.AddChild(scrollContainer);
            var vBoxContainer = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.FillExpand,
                SizeFlagsVertical = Control.SizeFlags.FillExpand,
            };
            scrollContainer.AddChild(vBoxContainer);

            // Handle top bar displaying type and ToString().
            {
                Control top;
                var stringified = obj.ToString();
                if (type.FullName != stringified)
                {
                    //var smallFont = new VectorFont(_resourceCache.GetResource<FontResource>("/Fonts/CALIBRI.TTF"), 10);
                    // Custom ToString() implementation.
                    var headBox = new VBoxContainer {SeparationOverride = 0};
                    headBox.AddChild(new Label {Text = stringified, ClipText = true});
                    headBox.AddChild(new Label
                    {
                        Text = type.FullName,
                    //    FontOverride = smallFont,
                        FontColorOverride = Color.DarkGray,
                        ClipText = true
                    });
                    top = headBox;
                }
                else
                {
                    top = new Label {Text = stringified};
                }

                if (_entity.TryGetComponent(out ISpriteComponent sprite))
                {
                    var hBox = new HBoxContainer();
                    top.SizeFlagsHorizontal = Control.SizeFlags.FillExpand;
                    hBox.AddChild(top);
                    hBox.AddChild(new SpriteView {Sprite = sprite});
                    vBoxContainer.AddChild(hBox);
                }
                else
                {
                    vBoxContainer.AddChild(top);
                }
            }

            _tabs = new TabContainer();
            _tabs.OnTabChanged += _tabsOnTabChanged;
            vBoxContainer.AddChild(_tabs);

            var clientVBox = new VBoxContainer {SeparationOverride = 0};
            _tabs.AddChild(clientVBox);
            _tabs.SetTabTitle(TabClientVars, "Client Variables");

            foreach (var control in LocalPropertyList(obj, ViewVariablesManager, _resourceCache))
            {
                clientVBox.AddChild(control);
            }

            var clientComponents = new VBoxContainer {SeparationOverride = 0};
            _tabs.AddChild(clientComponents);
            _tabs.SetTabTitle(TabClientComponents, "Client Components");

            // See engine#636 for why the Distinct() call.
            var componentList = _entity.GetAllComponents().OrderBy(c => c.GetType().ToString());
            foreach (var component in componentList)
            {
                var button = new Button {Text = TypeAbbreviation.Abbreviate(component.GetType().ToString()), TextAlign = Label.AlignMode.Left};
                button.OnPressed += args => { ViewVariablesManager.OpenVV(component); };
                clientComponents.AddChild(button);
            }

            if (!_entity.Uid.IsClientSide())
            {
                _serverVariables = new VBoxContainer {SeparationOverride = 0};
                _tabs.AddChild(_serverVariables);
                _tabs.SetTabTitle(TabServerVars, "Server Variables");

                _serverComponents = new VBoxContainer {SeparationOverride = 0};
                _tabs.AddChild(_serverComponents);
                _tabs.SetTabTitle(TabServerComponents, "Server Components");
            }
        }

        public override async void Initialize(SS14Window window, ViewVariablesBlobMetadata blob, ViewVariablesRemoteSession session)
        {
            // TODO: this is pretty poorly implemented right now.
            // For example, it assumes a client-side entity exists,
            // so it also means client bubbling won't work in this context.

            _entitySession = session;

            _membersBlob = await ViewVariablesManager.RequestData<ViewVariablesBlobMembers>(session, new ViewVariablesRequestMembers());

            var uid = (EntityUid) _membersBlob.Members.Single(p => p.Name == "Uid").Value;

            var entity = _entityManager.GetEntity(uid);
            Initialize(window, entity);
        }

        public override void Close()
        {
            base.Close();

            if (_entitySession != null && !_entitySession.Closed)
            {
                ViewVariablesManager.CloseSession(_entitySession);
                _entitySession = null;
            }
        }

        private async void _tabsOnTabChanged(int tab)
        {
            if (_serverLoaded || tab != TabServerComponents && tab != TabServerVars)
            {
                return;
            }

            _serverLoaded = true;

            if (_entitySession == null)
            {
                try
                {
                    _entitySession =
                        await ViewVariablesManager.RequestSession(new ViewVariablesEntitySelector(_entity.Uid));
                }
                catch (SessionDenyException e)
                {
                    var text = $"Server denied VV request: {e.Reason}";
                    _serverVariables.AddChild(new Label {Text = text});
                    _serverComponents.AddChild(new Label {Text = text});
                    return;
                }

                _membersBlob = await ViewVariablesManager.RequestData<ViewVariablesBlobMembers>(_entitySession, new ViewVariablesRequestMembers());
            }

            var otherStyle = false;
            foreach (var propertyData in _membersBlob.Members)
            {
                var propertyEdit = new ViewVariablesPropertyControl(ViewVariablesManager, _resourceCache);
                propertyEdit.SetStyle(otherStyle = !otherStyle);
                var editor = propertyEdit.SetProperty(propertyData);
                editor.OnValueChanged += o =>
                    ViewVariablesManager.ModifyRemote(_entitySession, new object[] {new ViewVariablesMemberSelector(propertyData.PropertyIndex)}, o);
                if (editor is ViewVariablesPropertyEditorReference refEditor)
                {
                    refEditor.OnPressed += () =>
                        ViewVariablesManager.OpenVV(
                            new ViewVariablesSessionRelativeSelector(_entitySession.SessionId,
                                new object[] {new ViewVariablesMemberSelector(propertyData.PropertyIndex)}));
                }

                _serverVariables.AddChild(propertyEdit);
            }

            var componentsBlob = await ViewVariablesManager.RequestData<ViewVariablesBlobEntityComponents>(_entitySession, new ViewVariablesRequestEntityComponents());

            _serverComponents.DisposeAllChildren();
            componentsBlob.ComponentTypes.Sort();
            foreach (var componentType in componentsBlob.ComponentTypes.OrderBy(t => t.Stringified))
            {
                var button = new Button {Text = componentType.Stringified, TextAlign = Label.AlignMode.Left};
                button.OnPressed += args =>
                {
                    ViewVariablesManager.OpenVV(
                        new ViewVariablesComponentSelector(_entity.Uid, componentType.FullName));
                };
                _serverComponents.AddChild(button);
            }
        }
    }
}
