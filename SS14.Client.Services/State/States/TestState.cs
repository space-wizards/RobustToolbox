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
using SS14.Client.Services.Helpers;
using SS14.Client.Graphics.Render;
using SS14.Shared.Maths;

namespace SS14.Client.Services.State.States
{
    public class TestState : State, IState
    {
        private List<CluwneSprite> _sprites;
        private List<GuiComponent> _UI;
        private SpriteBatch _testBatch;
        private CluwneSprite _floorTile;
        private List<TextSprite> _text;
        private uint updatecount=10;
        private GaussianBlur blur;
        private RenderImage test;

        private TextSprite FPS;


        public TestState (IDictionary<Type, object> managers) : base(managers)
        {
            _sprites = new List<CluwneSprite>();
            _text = new List<TextSprite>();
            _UI = new List<GuiComponent>();
            // setup everything we want to draw here.
            test = new RenderImage("bangin donk", (uint)CluwneLib.Screen.Size.X, (uint)CluwneLib.Screen.Size.Y,true);
             

            TextSprite CluwneEngineText = new TextSprite("TEST", "CluwneEngine", ResourceManager.GetFont("CALIBRI"));
            CluwneEngineText.Position = new Shared.Maths.Vector2(450,600);
            CluwneEngineText.Color = Color.DarkRed;
            CluwneEngineText.Text = " SS14: Running on CluwneEngine";
            _text.Add(CluwneEngineText);

            // testing TextSprites
            TextSprite VersionText = new TextSprite("TEST", "version", ResourceManager.GetFont("CALIBRI"));
            VersionText.Position = new Shared.Maths.Vector2(500, 650);
            VersionText.Color = Color.Gold;
            VersionText.Text = "( Running SFML v2.0 ) " ;
            _text.Add(VersionText);


            FPS = new TextSprite("FPSTEST", "X", ResourceManager.GetFont("CALIBRI"));
            FPS.Position = new Vector2(600, 600);
            FPS.Color = Color.White;
            _text.Add(FPS);

            TextSprite ProjNotDeadText = new TextSprite("TEST", "ProjNoDed", ResourceManager.GetFont("CALIBRI"));
            ProjNotDeadText.Position = new Shared.Maths.Vector2(512, 700);
            ProjNotDeadText.Color = Color.Gold;
            ProjNotDeadText.Text = "  Project != Dead :)";
            _text.Add(ProjNotDeadText);

            // testing CluwneSprite from SFML Texture
            Texture Cluwnelogo = new Texture(ResourceManager.GetTexture("CluwneLibLogo"));
            Cluwnelogo.Smooth = true;
            CluwneSprite CluwneEngineLogo = new CluwneSprite("CluwneLibLogo", Cluwnelogo);
            CluwneEngineLogo.Position = new Vector2(0,0);
            _sprites.Add(CluwneEngineLogo);         

            Texture _Tiles = new Texture(ResourceManager.GetTexture("0_Tiles"));
            CluwneSprite _TilesSprite = new CluwneSprite("0_Tiles",_Tiles);
            _TilesSprite.Position = new Vector2(50, 50);
            _sprites.Add(_TilesSprite);

            // Test a sprite extracted from the spritesheet
            CluwneSprite _OneTile = ResourceManager.GetSprite("locker_closed");
            _OneTile.Position = new Vector2(0, 0);
            _sprites.Add(_OneTile);


            ImageButton _button = new ImageButton 
            {
                ImageNormal = "connect_norm",
                ImageHover = "connect_hover",
            };

            _button.Update(0);
            _button.Position = new System.Drawing.Point(0, 0);
            _UI.Add(_button);

            Texture _Items = new Texture(ResourceManager.GetTexture("0_Items"));
            CluwneSprite _TilesItems = new CluwneSprite("0_Items", _Items);
            _TilesItems.Position = new SFML.System.Vector2f(0, 500);
            _sprites.Add(_TilesItems);

            Texture _Objects = new Texture(ResourceManager.GetTexture("0_Objects"));
            CluwneSprite _TilesObjects = new CluwneSprite("0_Objects", _Objects);
            _TilesObjects.Position = new SFML.System.Vector2f(970, 400);
            _sprites.Add(_TilesObjects);

            Texture _Decals = new Texture(ResourceManager.GetTexture("0_Decals"));
            _Decals.Smooth = true;
            CluwneSprite _TilesDecals = new CluwneSprite("0_Decals", _Decals);
            _TilesDecals.Position = new SFML.System.Vector2f(1000, 0);

            _floorTile = ResourceManager.GetSprite("floor_texture");
           

          
            _testBatch = new SpriteBatch();
}
         
        public void Startup() {}
        public void Shutdown() {}

        public void Update(FrameEventArgs e)
        {
           

          
            // draw a patch of floortile to test SpriteBatch()
            if (++updatecount % 10==0) {
                // but only every 10 frames, to test batch-reuse.
                // The first 9 frames won't have it drawn, to test empty batches.
                _testBatch.BeginDrawing();
                for(int y = 0; y < 50; y++)
                    for (int x = 0; x < 50; x++)
                    {
                        _floorTile.SetPosition(x * _floorTile.Width, y * _floorTile.Height);
                        _testBatch.Draw(_floorTile);
                    }
                _testBatch.EndDrawing();
              
            }

           
        }

        public void Render(FrameEventArgs e)
        {
            IntRect rect = new IntRect(0, 0, 1280,768 );

            test.BeginDrawing();
           if (_testBatch.Count > 0)
               test.Draw(_testBatch);
           foreach (CluwneSprite d in _sprites)
           {               
               d.Draw();
           }
           foreach (TextSprite d in _text)
           {
               d.Draw();
           }
           foreach (GuiComponent g in _UI)
           {
               g.Render();
           }
           test.EndDrawing();
          test.Blit(0, 0, test.Width, test.Height ,Color.White, BlitterSizeMode.Crop);
            
           
        }


        public void KeyDown(KeyEventArgs e) 
        {
            StateManager.RequestStateChange<MainScreen>();
        }
        public void KeyUp(KeyEventArgs e) {}
        public void MousePressed(MouseButtonEventArgs e) {}
        public void MouseUp(MouseButtonEventArgs e) 
        {
            StateManager.RequestStateChange<MainScreen>();
        }
        public void MouseDown(MouseButtonEventArgs e) {}
        public void MouseMoved(MouseMoveEventArgs e) {}
        public void MouseMove(MouseMoveEventArgs e) {}
        public void MouseWheelMove(MouseWheelEventArgs e) {}
        public void MouseEntered(EventArgs e) {}
        public void MouseLeft(EventArgs e) {}
        public void FormResize() {}
    }
}