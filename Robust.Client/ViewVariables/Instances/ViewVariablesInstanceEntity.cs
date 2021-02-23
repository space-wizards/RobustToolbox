using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.ViewVariables.Traits;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using static Robust.Client.UserInterface.Control;
using static Robust.Client.UserInterface.Controls.LineEdit;

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

        private TabContainer _tabs = default!;
        private IEntity _entity = default!;

        private ViewVariablesAddComponentWindow? _addComponentWindow;
        private bool _addComponentServer;

        private ViewVariablesRemoteSession? _entitySession;

        private ViewVariablesBlobMembers? _membersBlob;

        private VBoxContainer _clientComponents = default!;

        private VBoxContainer _serverVariables = default!;
        private VBoxContainer _serverComponents = default!;

        private Button _clientComponentsAddButton = default!;
        private Button _serverComponentsAddButton = default!;

        private LineEdit _clientComponentsSearchBar = default!;
        private LineEdit _serverComponentsSearchBar = default!;

        private bool _serverLoaded;

        public ViewVariablesInstanceEntity(IViewVariablesManagerInternal vvm, IEntityManager entityManager, IRobustSerializer robustSerializer) : base(vvm, robustSerializer)
        {
            _entityManager = entityManager;
        }

        public override void Initialize(SS14Window window, object obj)
        {
            _entity = (IEntity) obj;

            var scrollContainer = new ScrollContainer();
            //scrollContainer.SetAnchorPreset(Control.LayoutPreset.Wide, true);
            window.Contents.AddChild(scrollContainer);
            var vBoxContainer = new VBoxContainer();
            scrollContainer.AddChild(vBoxContainer);

            // Handle top bar displaying type and ToString().
            {
                Control top;
                var stringified = PrettyPrint.PrintUserFacingWithType(obj, out var typeStringified);
                if (typeStringified != "")
                {
                    //var smallFont = new VectorFont(_resourceCache.GetResource<FontResource>("/Fonts/CALIBRI.TTF"), 10);
                    // Custom ToString() implementation.
                    var headBox = new VBoxContainer {SeparationOverride = 0};
                    headBox.AddChild(new Label {Text = stringified, ClipText = true});
                    headBox.AddChild(new Label
                    {
                        Text = typeStringified,
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

                if (_entity.TryGetComponent(out ISpriteComponent? sprite))
                {
                    var hBox = new HBoxContainer();
                    top.HorizontalExpand = true;
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

            var first = true;
            foreach (var group in LocalPropertyList(obj, ViewVariablesManager, _robustSerializer))
            {
                ViewVariablesTraitMembers.CreateMemberGroupHeader(
                    ref first,
                    TypeAbbreviation.Abbreviate(group.Key),
                    clientVBox);

                foreach (var control in group)
                {
                    clientVBox.AddChild(control);
                }
            }

            _clientComponents = new VBoxContainer {SeparationOverride = 0};
            _tabs.AddChild(_clientComponents);
            _tabs.SetTabTitle(TabClientComponents, "Client Components");

            PopulateClientComponents();

            if (!_entity.Uid.IsClientSide())
            {
                _serverVariables = new VBoxContainer {SeparationOverride = 0};
                _tabs.AddChild(_serverVariables);
                _tabs.SetTabTitle(TabServerVars, "Server Variables");

                _serverComponents = new VBoxContainer {SeparationOverride = 0};
                _tabs.AddChild(_serverComponents);
                _tabs.SetTabTitle(TabServerComponents, "Server Components");

                PopulateServerComponents(false);
            }
        }

        private void PopulateClientComponents()
        {
            _clientComponents.DisposeAllChildren();

            _clientComponents.AddChild(_clientComponentsSearchBar = new LineEdit
            {
                PlaceHolder = Loc.GetString("Search"),
                HorizontalExpand = true,
            });

            _clientComponents.AddChild(_clientComponentsAddButton = new Button()
            {
                Text = Loc.GetString("Add Component"),
                HorizontalExpand = true,
            });

            _clientComponentsAddButton.OnPressed += OnClientComponentsAddButtonPressed;
            _clientComponentsSearchBar.OnTextChanged += OnClientComponentsSearchBarChanged;

            var componentList = _entity.GetAllComponents().OrderBy(c => c.GetType().ToString());

            foreach (var component in componentList)
            {
                var button = new Button {Text = TypeAbbreviation.Abbreviate(component.GetType()), TextAlign = Label.AlignMode.Left};
                var removeButton = new TextureButton()
                {
                    StyleClasses = { SS14Window.StyleClassWindowCloseButton },
                    HorizontalAlignment = HAlignment.Right
                };
                button.OnPressed += _ => ViewVariablesManager.OpenVV(component);
                removeButton.OnPressed += _ => RemoveClientComponent(component);
                button.AddChild(removeButton);
                _clientComponents.AddChild(button);
            }
        }

        private async void PopulateServerComponents(bool request = true)
        {
            _serverComponents.DisposeAllChildren();

            _serverComponents.AddChild(_serverComponentsSearchBar = new LineEdit
            {
                PlaceHolder = Loc.GetString("Search"),
                HorizontalExpand = true,
            });

            _serverComponents.AddChild(_serverComponentsAddButton = new Button()
            {
                Text = Loc.GetString("Add Component"),
                HorizontalExpand = true,
            });

            _serverComponentsSearchBar.OnTextChanged += OnServerComponentsSearchBarChanged;
            _serverComponentsAddButton.OnPressed += OnServerComponentsAddButtonPressed;

            if (!request || _entitySession == null) return;

            var componentsBlob = await ViewVariablesManager.RequestData<ViewVariablesBlobEntityComponents>(_entitySession, new ViewVariablesRequestEntityComponents());

            componentsBlob.ComponentTypes.Sort();

            var componentTypes = componentsBlob.ComponentTypes.AsEnumerable();

            if (!string.IsNullOrEmpty(_serverComponentsSearchBar.Text))
            {
                componentTypes = componentTypes
                    .Where(t => t.Stringified.Contains(_serverComponentsSearchBar.Text,
                        StringComparison.InvariantCultureIgnoreCase));
            }

            componentTypes = componentTypes.OrderBy(t => t.Stringified);

            foreach (var componentType in componentTypes)
            {
                var button = new Button {Text = componentType.Stringified, TextAlign = Label.AlignMode.Left};
                var removeButton = new TextureButton()
                {
                    StyleClasses = { SS14Window.StyleClassWindowCloseButton },
                    HorizontalAlignment = HAlignment.Right
                };
                button.OnPressed += _ =>
                {
                    ViewVariablesManager.OpenVV(
                        new ViewVariablesComponentSelector(_entity.Uid, componentType.FullName));
                };
                removeButton.OnPressed += _ =>
                {
                    // We send a command to remove the component.
                    IoCManager.Resolve<IClientConsoleHost>().RemoteExecuteCommand(null, $"rmcomp {_entity.Uid} {componentType.ComponentName}");
                    PopulateServerComponents();
                };
                button.AddChild(removeButton);
                _serverComponents.AddChild(button);
            }
        }

        private void UpdateClientComponentListVisibility(string? searchStr = null)
        {
            if (string.IsNullOrEmpty(searchStr))
            {
                foreach (var child in _clientComponents.Children)
                {
                    child.Visible = true;
                }

                return;
            }

            foreach (var child in _clientComponents.Children)
            {
                if (child is not Button button || child == _clientComponentsAddButton)
                {
                    continue;
                }

                if (button.Text == null)
                {
                    button.Visible = false;
                    continue;
                }

                if (!button.Text.Contains(searchStr, StringComparison.InvariantCultureIgnoreCase))
                {
                    button.Visible = false;
                    continue;
                }

                button.Visible = true;
            }
        }

        private void UpdateServerComponentListVisibility(string? searchStr = null)
        {
            if (string.IsNullOrEmpty(searchStr))
            {
                foreach (var child in _serverComponents.Children)
                {
                    child.Visible = true;
                }

                return;
            }

            foreach (var child in _serverComponents.Children)
            {
                if (child is not Button button || child == _serverComponentsAddButton)
                {
                    continue;
                }

                if (button.Text == null)
                {
                    button.Visible = false;
                    continue;
                }

                if (!button.Text.Contains(searchStr, StringComparison.InvariantCultureIgnoreCase))
                {
                    button.Visible = false;
                    continue;
                }

                button.Visible = true;
            }
        }

        private void OnClientComponentsSearchBarChanged(LineEditEventArgs args)
        {
            UpdateClientComponentListVisibility(args.Text);
        }

        private void OnServerComponentsSearchBarChanged(LineEditEventArgs args)
        {
            UpdateServerComponentListVisibility(args.Text);
        }

        private void OnClientComponentsAddButtonPressed(BaseButton.ButtonEventArgs _)
        {
            _addComponentWindow?.Dispose();

            _addComponentWindow = new ViewVariablesAddComponentWindow(GetValidComponentsForAdding(), false);
            _addComponentWindow.AddComponentButtonPressed += OnTryAddComponent;
            _addComponentServer = false;

            _addComponentWindow.OpenCentered();
        }

        private async void OnServerComponentsAddButtonPressed(BaseButton.ButtonEventArgs _)
        {
            _addComponentWindow?.Dispose();

            if (_entitySession == null) return;

            _addComponentWindow = new ViewVariablesAddComponentWindow(await GetValidServerComponentsForAdding(), true);
            _addComponentWindow.AddComponentButtonPressed += OnTryAddComponent;
            _addComponentServer = true;

            _addComponentWindow.OpenCentered();
        }

        /// <summary>
        ///     Returns an enumeration of components that can *probably* be added to an entity.
        /// </summary>
        private IEnumerable<string> GetValidComponentsForAdding()
        {
            var componentFactory = IoCManager.Resolve<IComponentFactory>();

            foreach (var type in componentFactory.AllRegisteredTypes)
            {
                if (_entity.HasComponent(type))
                    continue;

                yield return (componentFactory.GetRegistration(type).Name);
            }
        }

        /// <summary>
        ///     Requests and returns an enumeration of server-side components that can *probably* be added to an entity.
        /// </summary>
        private async Task<IEnumerable<string>> GetValidServerComponentsForAdding()
        {
            var blob = (ViewVariablesBlobAllValidComponents)
                await ViewVariablesManager.RequestData(_entitySession!, new ViewVariablesRequestAllValidComponents());

            return blob.ComponentTypes;
        }

        private async void OnTryAddComponent(ViewVariablesAddComponentWindow.AddComponentButtonPressedEventArgs eventArgs)
        {
            if (_addComponentServer)
            {
                // Attempted to add a component to the server entity... We send a command.
                IoCManager.Resolve<IClientConsoleHost>().RemoteExecuteCommand(null, $"addcomp {_entity.Uid} {eventArgs.Component}");
                PopulateServerComponents();
                _addComponentWindow?.Populate(await GetValidServerComponentsForAdding());
                return;
            }

            var componentFactory = IoCManager.Resolve<IComponentFactory>();

            if(!componentFactory.TryGetRegistration(eventArgs.Component, out var registration)) return;

            try
            {
                var comp = (Component) componentFactory.GetComponent(registration.Type);
                comp.Owner = _entity;
                _entityManager.ComponentManager.AddComponent(_entity, comp);
            }
            catch (Exception e)
            {
                Logger.Warning($"Failed to add component!\n{e}");
            }

            PopulateClientComponents();

            // Update list of components.
            _addComponentWindow?.Populate(GetValidComponentsForAdding());
        }

        private void RemoveClientComponent(IComponent component)
        {
            try
            {
                _entityManager.ComponentManager.RemoveComponent(_entity.Uid, component);
            }
            catch (Exception e)
            {
                Logger.Warning($"Couldn't remove component!\n{e}");
            }

            PopulateClientComponents();
        }

        public override async void Initialize(SS14Window window, ViewVariablesBlobMetadata blob, ViewVariablesRemoteSession session)
        {
            // TODO: this is pretty poorly implemented right now.
            // For example, it assumes a client-side entity exists,
            // so it also means client bubbling won't work in this context.

            _entitySession = session;

            _membersBlob = await ViewVariablesManager.RequestData<ViewVariablesBlobMembers>(session, new ViewVariablesRequestMembers());

            var uid = (EntityUid) _membersBlob.MemberGroups.SelectMany(p => p.Item2).Single(p => p.Name == "Uid").Value;

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
            if (tab == TabClientComponents)
            {
                // Repopulate client components in case something changed.
                PopulateClientComponents();
            }

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
            }

            _membersBlob = await ViewVariablesManager.RequestData<ViewVariablesBlobMembers>(_entitySession, new ViewVariablesRequestMembers());

            var otherStyle = false;
            var first = true;
            foreach (var (groupName, groupMembers) in _membersBlob!.MemberGroups)
            {
                ViewVariablesTraitMembers.CreateMemberGroupHeader(ref first, groupName, _serverVariables);

                foreach (var propertyData in groupMembers)
                {
                    var propertyEdit = new ViewVariablesPropertyControl(ViewVariablesManager, _robustSerializer);
                    propertyEdit.SetStyle(otherStyle = !otherStyle);
                    var editor = propertyEdit.SetProperty(propertyData);
                    var selectorChain = new object[] {new ViewVariablesMemberSelector(propertyData.PropertyIndex)};
                    editor.OnValueChanged += o => ViewVariablesManager.ModifyRemote(_entitySession, selectorChain, o);
                    editor.WireNetworkSelector(_entitySession.SessionId, selectorChain);

                    _serverVariables.AddChild(propertyEdit);
                }
            }

            PopulateServerComponents();
        }
    }
}
