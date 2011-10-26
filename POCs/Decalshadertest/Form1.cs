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

namespace Decalshadertest
{
    public partial class Form1 : Form
    {
        string mediadir;
        Sprite bouncesprite;
        Sprite decalsprite;
        BounceySprite[] bounceysprites;
        Vector2D position = new Vector2D(20,20);
        Vector2D direction = new Vector2D(1, 1);
        Random random = new Random(DateTime.Now.Millisecond);
        FXShader decalShader;

        public Form1()
        {
            InitializeComponent();
            mediadir = Directory.GetCurrentDirectory() + @"\..\..\..\..\Media";
        }


        private void SetupGorgon()
        {
            Gorgon.Initialize(true, false);
            Gorgon.SetMode(this);
            Gorgon.AllowBackgroundRendering = true;
            Gorgon.Screen.BackgroundColor = Color.FromArgb(50, 50, 50);

            //Gorgon.CurrentClippingViewport = new Viewport(0, 20, Gorgon.Screen.Width, Gorgon.Screen.Height - 20);
            PreciseTimer preciseTimer = new PreciseTimer();
            //Gorgon.MinimumFrameTime = PreciseTimer.FpsToMilliseconds(66);
            Gorgon.Idle += new FrameEventHandler(Gorgon_Idle);
            Gorgon.FrameStatsVisible = true;

            bouncesprite = new Sprite("bouncey", GorgonLibrary.Graphics.Image.FromFile(mediadir + @"\textures\Items\Armour.png"));
            bouncesprite.SetScale(2, 2);
            decalsprite = new Sprite("decal", GorgonLibrary.Graphics.Image.FromFile(mediadir + @"\textures\Decals\blood_decal.png"));
            decalsprite.SetScale(2, 2);

            bounceysprites = new BounceySprite[5000];
            for (int i = 0; i < 5000; i++)
            {
                bounceysprites[i] = new BounceySprite(bouncesprite, new Vector2D(random.Next(0, Gorgon.Screen.Width), random.Next(0, Gorgon.Screen.Height)), 
                                        new Vector2D((float)random.Next(-100000,100000) / 100000, (float)random.Next(-100000,100000) / 100000),
                                        decalsprite, new Vector2D(random.Next(-20,20), random.Next(-20,20)));
            }

            decalShader = FXShader.FromFile(mediadir + @"\shaders\decalshader.fx", ShaderCompileOptions.Debug);
            decalShader.Parameters["tex1"].SetValue(decalsprite.Image);

            //ShaderParameterType blah = decalShader.Techniques["dummy"].Parameters["sampler1"].ValueType;
            
        }

        private void Gorgon_Idle(object sender, FrameEventArgs e)
        {
            Gorgon.Screen.Clear(System.Drawing.Color.Black);
            Gorgon.CurrentShader = decalShader.Techniques["dummy"];
            
            foreach (BounceySprite spr in bounceysprites)
                spr.Draw();

            Gorgon.CurrentShader = null;
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
        Sprite sprite;
        Sprite decalsprite;
        Vector2D decalspritepos;
        public BounceySprite(Sprite _sprite, Vector2D _position, Vector2D _direction, Sprite _decal, Vector2D _decalpos)
        {
            sprite = _sprite;
            position = _position;
            direction = _direction;
        }

        public void Draw()
        {
            position += direction * 2;
            sprite.SetPosition(position.X, position.Y);
            if (sprite.AABB.Right > Gorgon.Screen.Width)
                direction.X = -1 * Math.Abs(direction.X);
            if (sprite.AABB.Left < 0)
                direction.X = Math.Abs(direction.X);
            if (sprite.AABB.Top < 0)
                direction.Y = Math.Abs(direction.Y);
            if (sprite.AABB.Bottom > Gorgon.Screen.Height)
                direction.Y = -1 * Math.Abs(direction.Y);

            sprite.Draw();
        }

    }
}
