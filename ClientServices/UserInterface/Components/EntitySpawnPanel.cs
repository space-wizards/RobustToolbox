using System;
using System.Linq;
using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.GOC;
using ClientInterfaces.Placement;
using GorgonLibrary;
using GorgonLibrary.InputDevices;
using CGO;
using SS13_Shared;

namespace ClientServices.UserInterface.Components
{
    class EntitySpawnPanel : Window
    {
        private readonly IResourceManager _resourceManager;
        private readonly IPlacementManager _placementManager;

        private readonly ScrollableContainer _entityList;
        private readonly Label _clearLabel;
        private readonly Textbox _entSearchTextbox;
        private readonly SimpleImageButton _eraserButton;

        public EntitySpawnPanel(Size size, IResourceManager resourceManager, IPlacementManager placementManager)
            : base("Entity Spawn Panel", size, resourceManager)
        {
            _resourceManager = resourceManager;
            _placementManager = placementManager;

            _entityList = new ScrollableContainer("entspawnlist", new Size(200, 400), _resourceManager) {Position = new Point(5, 5)};

            var searchLabel = new Label("Entity Search:", _resourceManager) {Position = new Point(210, 0)};
            components.Add(searchLabel);

            _entSearchTextbox = new Textbox(125, _resourceManager) {Position = new Point(210, 20)};
            _entSearchTextbox.OnSubmit += entSearchTextbox_OnSubmit;
            components.Add(_entSearchTextbox);

            _clearLabel = new Label("[Clear Filter]", _resourceManager)
                             {
                                 DrawBackground = true,
                                 DrawBorder = true,
                                 Position = new Point(210, 55)
                             };

            _clearLabel.Clicked += ClearLabelClicked;
            _clearLabel.BackgroundColor = Color.Gray;
            components.Add(_clearLabel);

            _eraserButton = new SimpleImageButton("erasericon", _resourceManager)
                               {
                                   Position = new Point(_clearLabel.Position.X + _clearLabel.ClientArea.Width + 5, _clearLabel.Position.Y)
                               };

            //eraserButton.Position = new Point(clearLabel.ClientArea.Right + 5, clearLabel.ClientArea.Top); Clientarea not updating properly. FIX THIS
            _eraserButton.Clicked += EraserButtonClicked;
            components.Add(_eraserButton);

            BuildEntityList();

            Position = new Point((int)(Gorgon.Screen.Width / 2f) - (int)(ClientArea.Width / 2f), (int)(Gorgon.Screen.Height / 2f) - (int)(ClientArea.Height / 2f));
            _placementManager.PlacementCanceled += PlacementManagerPlacementCanceled;
        }

        void EraserButtonClicked(SimpleImageButton sender)
        {
            _placementManager.ToggleEraser();
        }

        void ClearLabelClicked(Label sender)
        {
            _clearLabel.BackgroundColor = Color.Gray;
            BuildEntityList();
        }

        void entSearchTextbox_OnSubmit(string text)
        {
            BuildEntityList(text);
        }

        void PlacementManagerPlacementCanceled(object sender, EventArgs e)
        {
            foreach (var curr in _entityList.components.Where(curr => curr.GetType() == typeof(EntitySpawnSelectButton)))
                ((EntitySpawnSelectButton)curr).selected = false;
        }

        private void BuildEntityList(string searchStr = null)
        {
            var maxWidth = 0;
            var yOffset = 5;

            _entityList.components.Clear();
            _entityList.ResetScrollbars();

            var templates = (searchStr == null) ? 
                EntityManager.Singleton.TemplateDB.Templates.ToList() : 
                EntityManager.Singleton.TemplateDB.Templates.Where(x => x.Value.Name.ToLower().Contains(searchStr.ToLower())).ToList();
        

            if (searchStr != null) _clearLabel.BackgroundColor = Color.LightGray;

            foreach (var newButton in templates.Select(entry => new EntitySpawnSelectButton(entry.Value, entry.Key, _resourceManager)))
            {
                _entityList.components.Add(newButton);
                newButton.Position = new Point(5, yOffset);
                newButton.Update();
                yOffset += 5 + newButton.ClientArea.Height;
                newButton.Clicked += NewButtonClicked;

                if (newButton.ClientArea.Width > maxWidth) maxWidth = newButton.ClientArea.Width;
            }

            foreach (var curr in _entityList.components.Where(curr => curr.GetType() == typeof(EntitySpawnSelectButton)))
                ((EntitySpawnSelectButton)curr).fixed_width = maxWidth;
        }

        void NewButtonClicked(EntitySpawnSelectButton sender, IEntityTemplate template, string templateName)
        {
            if (sender.selected)
            {
                sender.selected = false;
                _placementManager.Clear();
                return;
            }

            foreach (var curr in _entityList.components.Where(curr => curr.GetType() == typeof(EntitySpawnSelectButton)))
                ((EntitySpawnSelectButton)curr).selected = false;

            var newObjInfo = new PlacementInformation
                                 {
                                     PlacementOption = template.PlacementMode,
                                     EntityType = templateName,
                                     Range = 400,
                                     IsTile = false
                                 };

            _placementManager.BeginPlacing(newObjInfo);

            sender.selected = true; //This needs to be last.
        }

        public override void Update()
        {
            if (disposing || !IsVisible()) return;
            base.Update();
            if (_entityList != null)
            {
                _entityList.Position = new Point(ClientArea.X + 5, ClientArea.Y + 5);
                _entityList.Update();
            }
        }

        public override void Render()
        {
            if (disposing || !IsVisible()) return;
            _eraserButton.Color = _placementManager.Eraser ? Color.Tomato : Color.White;
            base.Render();
            _entityList.Render();
        }

        public override void Dispose()
        {
            if (disposing) return;
            _placementManager.PlacementCanceled -= PlacementManagerPlacementCanceled;
            base.Dispose();
            _entityList.Dispose();
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return false;
            if (_entityList.MouseDown(e)) return true;
            if (base.MouseDown(e)) return true;
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return false;
            if (_entityList.MouseUp(e)) return true;
            if (base.MouseUp(e)) return true;
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return;
            _entityList.MouseMove(e);
            base.MouseMove(e);
        }

        public override bool MouseWheelMove(MouseInputEventArgs e)
        {
            if (_entityList.MouseWheelMove(e)) return true;
            if (base.MouseWheelMove(e)) return true;
            return false;
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            if (_entityList.KeyDown(e)) return true;
            if (base.KeyDown(e)) return true;
            return false;
        }
    }
}