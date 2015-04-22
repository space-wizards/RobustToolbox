using System;
using System.Drawing;
using System.Collections.Generic;
using SFML.Window;
using SFML.Graphics;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprite;
using SS14.Client.Graphics.Event;
using SS14.Client.Interfaces.State;
using SS14.Client.Services.UserInterface.Components;
using Color = System.Drawing.Color;

namespace SS14.Client.Services.State.States
{
    public class TestState : State, IState
    {
        private List<ICluwneDrawable> _sprites;
        private List<GuiComponent> _UI;
        private SpriteBatch _testBatch;
        private CluwneSprite _floorTile;
        private uint updatecount=0;
        public TestState (IDictionary<Type, object> managers)
            : base(managers)
        {
            _sprites = new List<ICluwneDrawable>();
            _UI = new List<GuiComponent>();
            // setup everything we want to draw here.
            TextSprite CluwneEngineText = new TextSprite("TEST", "CluwneEngine", ResourceManager.GetFont("CALIBRI"));
            CluwneEngineText.Position = new Shared.Maths.Vector2(450,600);
            CluwneEngineText.Color = Color.DarkRed;
            CluwneEngineText.Text = " SS14: Running on CluwneEngine";
            _sprites.Add(CluwneEngineText);

            // testing TextSprites
            TextSprite VersionText = new TextSprite("TEST", "version", ResourceManager.GetFont("CALIBRI"));
            VersionText.Position = new Shared.Maths.Vector2(500, 650);
            VersionText.Color = Color.Gold;
            VersionText.Text = "( Running SFML v2.0 ) " ;
            _sprites.Add(VersionText);

            TextSprite ProjNotDeadText = new TextSprite("TEST", "ProjNoDed", ResourceManager.GetFont("CALIBRI"));
            ProjNotDeadText.Position = new Shared.Maths.Vector2(512, 700);
            ProjNotDeadText.Color = Color.Gold;
            ProjNotDeadText.Text = "  Project != Dead :)";
            _sprites.Add(ProjNotDeadText);

            // testing CluwneSprite from SFML Texture
            Texture Cluwnelogo = new Texture(ResourceManager.GetImage("Textures/CluwneLibLogo.png"));
            CluwneSprite CluwneEngineLogo = new CluwneSprite(Cluwnelogo);
            CluwneEngineLogo.Position = new SFML.System.Vector2f(150,100);
            _sprites.Add(CluwneEngineLogo);

            Texture _Tiles = new Texture(ResourceManager.GetImage("Textures/0_Tiles.png"));
            CluwneSprite _TilesSprite = new CluwneSprite(_Tiles);
            _TilesSprite.Position = new SFML.System.Vector2f(50, 50);
            _sprites.Add(_TilesSprite);

            // Test a sprite extracted from the spritesheet
            CluwneSprite _OneTile = ResourceManager.GetSprite("locker_closed");
            _OneTile.Position = new SFML.System.Vector2f(0, 0);

            _sprites.Add(_OneTile);


            ImageButton _button = new ImageButton {
                ImageNormal = "connect_norm",
                ImageHover = "connect_hover",
            };
            _button.Update(0);
            _button.Position = new System.Drawing.Point(0, 0);
            _UI.Add(_button);

            Texture _Items = new Texture(ResourceManager.GetImage("Textures/0_Items.png"));
            CluwneSprite _TilesItems = new CluwneSprite(_Items);
            _TilesItems.Position = new SFML.System.Vector2f(0, 500);
            _sprites.Add(_TilesItems);

            Texture _Objects = new Texture(ResourceManager.GetImage("Textures/0_Objects.png"));
            CluwneSprite _TilesObjects = new CluwneSprite(_Objects);
            _TilesObjects.Position = new SFML.System.Vector2f(970, 400);
            _sprites.Add(_TilesObjects);

            Texture _Decals = new Texture(ResourceManager.GetImage("Textures/0_Decals.png"));
            CluwneSprite _TilesDecals = new CluwneSprite(_Decals);
            _TilesDecals.Position = new SFML.System.Vector2f(1000, 0);
            _sprites.Add(_TilesDecals);

            _floorTile = ResourceManager.GetSprite("floor_texture");
            _testBatch = new SpriteBatch();
}
         
        public void Startup() {}
        public void Shutdown() {}

        public void Update(FrameEventArgs e) {
            // draw a patch of floortile to test SpriteBatch()
            if (++updatecount % 10==0) {
                // but only every 10 frames, to test batch-reuse.
                // The first 9 frames won't have it drawn, to test empty batches.
                _testBatch.Begin();
                for(int y=0;y<5; y++)
                    for(int x=0;x<5;x++) {
                        _floorTile.SetPosition(x*_floorTile.Width, y*_floorTile.Height);
                        _testBatch.Draw(_floorTile);
                    }
                _testBatch.End();
            }

        }
        public void Render(FrameEventArgs e) {
            if (_testBatch.Count>0)
                CluwneLib.CurrentRenderTarget.Draw(_testBatch);
            foreach (ICluwneDrawable d in _sprites)
                d.Draw();
            foreach (GuiComponent g in _UI)
                g.Render();

        }
        public void KeyDown(KeyEventArgs e) {
            StateManager.RequestStateChange<MainScreen>();
        }
        public void KeyUp(KeyEventArgs e) {}
        public void MousePressed(MouseButtonEventArgs e) {}
        public void MouseUp(MouseButtonEventArgs e) {
            StateManager.RequestStateChange<MainScreen>();
        }
        public void MouseDown(MouseButtonEventArgs e) {}
        public void MouseMoved(MouseMoveEventArgs e) {}
        public void MouseMove(MouseMoveEventArgs e) {}
        public void MouseWheelMove(MouseWheelEventArgs e) {}
        public void FormResize() {}
    }
}