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
        public Sprite bouncesprite;
        Sprite decalsprite;
        BounceySprite[] bounceysprites;
        Vector2D position = new Vector2D(20,20);
        Vector2D direction = new Vector2D(1, 1);
        Random random = new Random(DateTime.Now.Millisecond);
        public FXShader decalShader;
        public RenderImage baseTarget;
        public Sprite baseTargetSprite;

        public Form1()
        {
            InitializeComponent();
            mediadir = Directory.GetCurrentDirectory() + @"\..\..\..\..\Media";
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

            bouncesprite = new Sprite("bouncey", GorgonLibrary.Graphics.Image.FromFile(mediadir + @"\textures\0_Items.png"), new Vector2D(0,0), new Vector2D(22,20));
            bouncesprite.SetScale(3, 3);
            decalsprite = new Sprite("decal", GorgonLibrary.Graphics.Image.FromFile(mediadir + @"\textures\0_Decals.png"), new Vector2D(56,0), new Vector2D(103,29));
            decalsprite.SetScale(1, 1);
            decalShader = FXShader.FromFile(mediadir + @"\shaders\decalshader.fx", ShaderCompileOptions.Debug);
            decalShader.Parameters["tex1"].SetValue(decalsprite.Image);

            baseTarget = new RenderImage("baseTarget", 32, 32, ImageBufferFormats.BufferRGB888A8);
            baseTargetSprite = new Sprite("baseTargetSprite", baseTarget);

            bounceysprites = new BounceySprite[10];
            for (int i = 0; i < 10; i++)
            {
                bounceysprites[i] = new BounceySprite(bouncesprite, new Vector2D(random.Next(0, Gorgon.Screen.Width), random.Next(0, Gorgon.Screen.Height)), 
                                        new Vector2D((float)random.Next(-100000,100000) / 100000, (float)random.Next(-100000,100000) / 100000),
                                        decalsprite, new Vector2D(random.Next(-10,15), random.Next(-10,15)), decalShader, this);
            }


            //Calculate decal texcoord offsets
            /*Vector2D decalBToffset = new Vector2D(10,5);
            float BTXDTL_x = decalBToffset.X / bouncesprite.Image.Width;
            float BTXDTL_y = decalBToffset.Y / bouncesprite.Image.Height;
            float BTXDBR_x = (decalBToffset.X + decalsprite.Width)/bouncesprite.Image.Width;
            float BTXDBR_y = (decalBToffset.Y + decalsprite.Height)/bouncesprite.Image.Height;
            float CFx = (float)decalsprite.Image.Width/(float)bouncesprite.Image.Width;
            float CFy = (float)decalsprite.Image.Height / (float)bouncesprite.Image.Height;
            float DOtc_xtl = (float)decalsprite.ImageOffset.X / (float)decalsprite.Image.Width;
            float DOtc_ytl = (float)decalsprite.ImageOffset.Y / (float)decalsprite.Image.Height;

            Vector4D decalParms1 = new Vector4D(BTXDTL_x, BTXDTL_y, BTXDBR_x, BTXDBR_y);
            Vector4D decalParms2 = new Vector4D(CFx, CFy, DOtc_xtl, DOtc_ytl);*/
        }



        private void Gorgon_Idle(object sender, FrameEventArgs e)
        {
            Gorgon.Screen.Clear(System.Drawing.Color.Black);
            //Gorgon.CurrentShader = decalShader.Techniques["drawWithDecal"];

            Gorgon.CurrentShader = decalShader.Techniques["drawWithDecal"];
            foreach (BounceySprite spr in bounceysprites)
            {
                spr.Draw();
                
            }
            Gorgon.CurrentShader = null;
            //Gorgon.CurrentShader = null;
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
        Sprite decalsprite;
        Vector2D decalspritepos;
        //FXShader decalShader;
        Vector4D[] decalParams1;
        Vector4D[] decalParams2;
        Form1 form1;

        public BounceySprite(Sprite _sprite, Vector2D _position, Vector2D _direction, Sprite _decal, Vector2D _decalpos, FXShader _decalShader, Form1 _form1)
        {
            position = _position;
            direction = _direction;
            decalsprite = _decal;
            decalspritepos = _decalpos;
            //decalShader = _decalShader;
            form1 = _form1;
            Vector4D decpar1 = decalParms1(decalspritepos, new Vector2D(form1.bouncesprite.Image.Width, form1.bouncesprite.Image.Height), decalsprite.Size);
            Vector4D decpar2 = decalParms2(new Vector2D(decalsprite.Image.Width, decalsprite.Image.Height), new Vector2D(form1.bouncesprite.Image.Width, form1.bouncesprite.Image.Height), decalsprite.ImageOffset);
            decalParams1 = new Vector4D[] { decpar1, decpar1, decpar1, decpar1, decpar1 };
            decalParams2 = new Vector4D[] { decpar2, decpar2, decpar2, decpar2, decpar2 };
            
        }

        public void Draw()
        {
            //position += direction * 2;
            form1.bouncesprite.SetPosition(position.X, position.Y);
            //sprite.SetPosition(0,0);
            /*if (sprite.AABB.Right > Gorgon.Screen.Width)
                direction.X = -1 * Math.Abs(direction.X);
            if (sprite.AABB.Left < 0)
                direction.X = Math.Abs(direction.X);
            if (sprite.AABB.Top < 0)
                direction.Y = Math.Abs(direction.Y);
            if (sprite.AABB.Bottom > Gorgon.Screen.Height)
                direction.Y = -1 * Math.Abs(direction.Y);
            */
            form1.decalShader.Techniques["drawWithDecal"].Parameters["decalParms1"].SetValue(decalParams1);
            form1.decalShader.Techniques["drawWithDecal"].Parameters["decalParms2"].SetValue(decalParams2);
            
            //form1.baseTarget.Width = (int)sprite.Width;
            //form1.baseTarget.Height = (int)sprite.Height;
            //Gorgon.CurrentRenderTarget = form1.baseTarget;
            form1.bouncesprite.Draw(true);

            //Gorgon.CurrentRenderTarget = null;
            //form1.baseTargetSprite.SetPosition(position.X, position.Y);
            //form1.baseTargetSprite.Draw();
        }

        private Vector4D decalParms1(Vector2D decalBToffset, Vector2D baseTexAtlasSize, Vector2D decalSize)
        {
            float BTXDTL_x = decalBToffset.X / baseTexAtlasSize.X;
            float BTXDTL_y = decalBToffset.Y / baseTexAtlasSize.Y;
            float BTXDBR_x = (decalBToffset.X + decalSize.X) / baseTexAtlasSize.X;
            float BTXDBR_y = (decalBToffset.Y + decalSize.Y) / baseTexAtlasSize.Y;
            return new Vector4D(BTXDTL_x, BTXDTL_y, BTXDBR_x, BTXDBR_y);
        }

        private Vector4D decalParms2(Vector2D decalAtlasSize, Vector2D baseTexAtlasSize, Vector2D decalAtlasOffset)
        {
            float CFx = (float)decalAtlasSize.X / (float)baseTexAtlasSize.X;
            float CFy = (float)decalAtlasSize.Y / (float)baseTexAtlasSize.Y;
            float DOtc_xtl = (float)decalAtlasOffset.X / (float)decalAtlasSize.X;
            float DOtc_ytl = (float)decalAtlasOffset.Y / (float)decalAtlasSize.Y;
            return new Vector4D(CFx, CFy, DOtc_xtl, DOtc_ytl);
        }
    }
}
