using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     A good simple menu bar control, like the one at the top of your IDE!
    /// </summary>
    [Virtual]
    public class MenuBar : PanelContainer
    {
        private readonly List<Menu> _menus = new();
        private readonly List<MenuTopButton> _buttons = new();
        private readonly BoxContainer _hBox;
        private readonly Popup _popup;
        private readonly BoxContainer _popupVBox;
        private bool _popupOpen;
        private bool _isSubmenu = false;

        public IList<Menu> Menus { get; }

        public MenuBar() : this(false)
        {
        }

        public MenuBar(bool isSubmenu = false)
        {
            _isSubmenu = isSubmenu;
            _popup = new Popup
            {
                Children =
                {
                    (_popupVBox = new BoxContainer
                    {
                        Orientation = LayoutOrientation.Vertical,
                        MinSize = (300, 0)
                    })
                }
            };
            _popup.OnPopupHide += PopupHidden;
            UserInterfaceManager.ModalRoot.AddChild(_popup);
            Menus = new MenuCollection(this);
            AddChild(_hBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                SeparationOverride = 8
            });
        }

        private void AddMenu(Menu menu)
        {
            MenuTopButton button = _isSubmenu ? new SubMenuTopButton(menu) : new MenuBarTopButton(menu);
            _menus.Add(menu);
            _buttons.Add(button);
            _hBox.AddChild(button);

            button.OnKeyBindDown += _ => OpenPopupFor(button);

            button.OnMouseEntered += _ =>
            {
                if (_popupOpen)
                {
                    OpenPopupFor(button);
                }
            };
        }

        private void OpenPopupFor(MenuTopButton button)
        {
            _popupVBox.RemoveAllChildren();
            var menu = button.ChildMenu;
            ConstructMenu(menu, _popupVBox);

            var globalPos = button.GlobalPosition;
            globalPos += (_isSubmenu ? button.Width : 0, _isSubmenu ? 0 : button.Height);
            _popup.Open(UIBox2.FromDimensions(globalPos, _popupVBox.Size));

            // Set this after running open so that if this is called from MouseEntered,
            // It won't get set to false by Open() closing the popup to move it.
            _popupOpen = true;
        }

        private void PopupHidden()
        {
            _popupOpen = false;
        }

        private bool RemoveMenu(Menu menu)
        {
            var index = _menus.IndexOf(menu);
            if (index < 0)
            {
                return false;
            }

            var button = _buttons[index];

            _hBox.RemoveChild(button);
            _buttons.RemoveAt(index);
            _menus.RemoveAt(index);

            return false;
        }

        private void ConstructMenu(Menu menu, Control container)
        {
            foreach (var entry in menu.Entries)
            {
                switch (entry)
                {
                    case MenuButton menuButton:
                        var pushButton = new Button
                        {
                            Text = menuButton.Text,
                            ClipText = true,
                            Disabled = menuButton.Disabled,
                            TextAlign = Label.AlignMode.Left
                        };
                        pushButton.OnPressed += _ =>
                        {
                            _popup.Visible = false;
                            menuButton.OnPressed?.Invoke();
                        };
                        container.AddChild(pushButton);
                        break;

                    case MenuSeparator _:
                        var control = new Control {MinSize = (0, 6)};
                        container.AddChild(control);
                        break;

                    case SubMenu submenuButton:
                        var subButton = new MenuBar(true);
                        container.AddChild(subButton);
                        container.OnChildRemoved += _ =>
                        {
                            if (subButton._popupOpen)
                            {
                                subButton._popup.RemoveAllChildren();
                            }
                        };

                        var subButtonMenu = new Menu(){
                            Title = submenuButton.Text
                        };
                        subButtonMenu.Entries.AddRange(submenuButton.Entries);
                        subButton.Menus.Add(subButtonMenu);

                        break;
                }
            }
        }

        private sealed class MenuCollection : IList<Menu>
        {
            private readonly MenuBar _menuBar;

            public MenuCollection(MenuBar menuBar)
            {
                _menuBar = menuBar;
            }

            public IEnumerator<Menu> GetEnumerator()
            {
                return _menuBar._menus.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public void Add(Menu item)
            {
                _menuBar.AddMenu(item);
            }

            public void Clear()
            {
                foreach (var menu in this.ToList())
                {
                    Remove(menu);
                }
            }

            public bool Contains(Menu item)
            {
                return _menuBar._menus.Contains(item);
            }

            public void CopyTo(Menu[] array, int arrayIndex)
            {
                _menuBar._menus.CopyTo(array, arrayIndex);
            }

            public bool Remove(Menu item)
            {
                return _menuBar.RemoveMenu(item);
            }

            public int Count => _menuBar._menus.Count;
            public bool IsReadOnly => false;

            public int IndexOf(Menu item)
            {
                return _menuBar._menus.IndexOf(item);
            }

            public void Insert(int index, Menu item)
            {
                throw new NotImplementedException();
            }

            public void RemoveAt(int index)
            {
                Remove(this[index]);
            }

            public Menu this[int index]
            {
                get => _menuBar._menus[index];
                set => throw new NotImplementedException();
            }
        }

        public abstract class MenuTopButton : PanelContainer
        {
            public const string StylePseudoClassHover = "hover";
            public Menu ChildMenu;

            public MenuTopButton(Menu menu)
            {
                MouseFilter = MouseFilterMode.Pass;
                ChildMenu = menu;
            }

            protected internal override void MouseEntered()
            {
                base.MouseEntered();

                SetOnlyStylePseudoClass(StylePseudoClassHover);
            }

            protected internal override void MouseExited()
            {
                base.MouseExited();

                SetOnlyStylePseudoClass(null);
            }
        }

        public sealed class MenuBarTopButton : MenuTopButton
        {
            public Label Label { get; }

            public MenuBarTopButton(Menu menu) : base(menu)
            {
               AddChild(Label = new Label {Text = menu.Title});
            }
        }

        public sealed class SubMenuTopButton : MenuTopButton
        {
            public Button Button { get; } //basically just replicating the style of the other menu elements

            public SubMenuTopButton(Menu menu) : base(menu)
            {
                Button = new Button {
                    Text = menu.Title,
                    MinSize = (300, 0),
                    MouseFilter = MouseFilterMode.Pass
                };

                Label arrowLabel =  new Label {Text = ">", HorizontalAlignment = HAlignment.Right};
                Button.AddChild(arrowLabel);


                AddChild(Button);

            }
        }

        /// <summary>
        ///     An entry in a menu of a menu bar.
        /// </summary>
        public abstract class MenuEntry
        {
            private protected MenuEntry()
            {
            }
        }

        /// <summary>
        ///     A menu in a menu bar, like file...
        /// </summary>
        public sealed class Menu
        {
            public string? Title { get; set; }

            public List<MenuEntry> Entries { get; } = new();
        }

        /// <summary>
        ///     A basic button entry in a menu bar menu.
        /// </summary>
        public sealed class MenuButton : MenuEntry
        {
            public string? Text { get; set; }
            public bool Disabled { get; set; }
            public Action? OnPressed { get; set; }
        }

        public sealed class MenuSeparator : MenuEntry
        {
        }

        public sealed class SubMenu : MenuEntry
        {
            public string? Text { get; set; }

            public List<MenuEntry> Entries { get; set; } = new();
        }
    }

}
