using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using System.IO;

namespace Balltest
{
    public partial class Balltest : Form
    {
        string mediadir;
        public Sprite bouncesprite;
        BounceySprite[] bounceysprites;
        Vector2D position = new Vector2D(20,20);
        Vector2D direction = new Vector2D(1, 1);
        public Random random = new Random(DateTime.Now.Millisecond);
        public RenderImage baseTarget;
        public Sprite baseTargetSprite;
        public RenderImage screenTarget;

        public Balltest()
        {
            InitializeComponent();
        }


        private void SetupGorgon()
        {
            Gorgon.Initialize(true, false);
            Gorgon.SetMode(this);
            //Gorgon.AllowBackgroundRendering = true;
            //Gorgon.Screen.BackgroundColor = Color.FromArgb(50, 50, 50);

            //Gorgon.CurrentClippingViewport = new Viewport(0, 20, Gorgon.Screen.Width, Gorgon.Screen.Height - 20);
            //PreciseTimer preciseTimer = new PreciseTimer();
            //Gorgon.MinimumFrameTime = PreciseTimer.FpsToMilliseconds(66);
            Gorgon.Idle += new FrameEventHandler(Gorgon_Idle);
            Gorgon.FrameStatsVisible = true;
            Gorgon.DeviceReset += MainWindowResizeEnd;

            bouncesprite = new Sprite("flyingball", GorgonLibrary.Graphics.Image.FromFile(mediadir + @"flyingball.png"));
            //bouncesprite.SetScale(3, 3);

            baseTarget = new RenderImage("baseTarget", 32, 32, ImageBufferFormats.BufferRGB888A8);
            baseTargetSprite = new Sprite("baseTargetSprite", baseTarget);

            screenTarget = new RenderImage("screenTarget", Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height, ImageBufferFormats.BufferRGB888A8);

            bounceysprites = new BounceySprite[10];
            for (int i = 0; i < 10; i++)
            {
                bounceysprites[i] = new BounceySprite(bouncesprite, new Vector2D(random.Next(0, Gorgon.CurrentClippingViewport.Width), random.Next(0, Gorgon.CurrentClippingViewport.Height)), 
                                        new Vector2D((float)random.Next(-100000,100000) / 100000, (float)random.Next(-100000,100000) / 100000)
                                       , this);
            }
        }

        private void MainWindowResizeEnd(object sender, EventArgs e)
        {
            //_input.Mouse.SetPositionRange(0, 0, Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height);
            //_stateManager.CurrentState.FormResize();
            screenTarget.Width = Gorgon.CurrentClippingViewport.Width;
            screenTarget.Height = Gorgon.CurrentClippingViewport.Height;
        }

        private void Gorgon_Idle(object sender, FrameEventArgs e)
        {
            Gorgon.Screen.Clear(Color.Black);
            screenTarget.Clear(Color.Black);
            Gorgon.CurrentRenderTarget = screenTarget;
            foreach (BounceySprite spr in bounceysprites)
            {
                spr.Draw();
            }
            Gorgon.CurrentRenderTarget = null;
            screenTarget.Image.Blit(0,0);
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {

            SetupGorgon();
            Gorgon.Go();
        }
    }

    public class BounceySprite
    {
        Vector2D direction;
        Vector2D position;
        private Sprite sprite;
        Balltest form1;
        private Color color;

        public BounceySprite(Sprite _sprite, Vector2D _position, Vector2D _direction, Balltest _form1)
        {
            position = _position;
            direction = _direction;
            form1 = _form1;
            sprite = _sprite;
            color = randomColor();
        }

        private Color randomColor()
        {
            return Color.FromArgb(form1.random.Next(0, 255), form1.random.Next(0, 255), form1.random.Next(0, 255),
                                  form1.random.Next(0, 255));
        }

        public void Draw()
        {
            position += direction * 2;
            form1.bouncesprite.SetPosition(position.X, position.Y);
            form1.bouncesprite.Color = color;
            //sprite.SetPosition(0,0);
            if (sprite.AABB.Right > Gorgon.Screen.Width)
                direction.X = -1 * Math.Abs(direction.X);
            if (sprite.AABB.Left < 0)
                direction.X = Math.Abs(direction.X);
            if (sprite.AABB.Top < 0)
                direction.Y = Math.Abs(direction.Y);
            if (sprite.AABB.Bottom > Gorgon.Screen.Height)
                direction.Y = -1 * Math.Abs(direction.Y);
            
            
            //form1.baseTarget.Width = (int)sprite.Width;
            //form1.baseTarget.Height = (int)sprite.Height;
            //Gorgon.CurrentRenderTarget = form1.baseTarget;
            form1.bouncesprite.Draw(true);

            //Gorgon.CurrentRenderTarget = null;
            //form1.baseTargetSprite.SetPosition(position.X, position.Y);
            //form1.baseTargetSprite.Draw();
        }
    }
}
