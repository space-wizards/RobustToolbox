#if !__MonoCS__ && VS_DEBUGGERVISUALIZERS_EXISTS

using Microsoft.VisualStudio.DebuggerVisualizers;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

[assembly: DebuggerVisualizer(
    typeof(SS14.Client.Debug.TextureVisualizer),
    typeof(SS14.Client.Debug.TextureVisualizerObjectSource),
    Target = typeof(SFML.Graphics.Texture),
    Description = "Texture Visualizer")]

[assembly: DebuggerVisualizer(
    typeof(SS14.Client.Debug.TextureVisualizer),
    typeof(SS14.Client.Debug.RenderTextureVisualizerObjectSource),
    Target = typeof(SFML.Graphics.RenderTexture),
    Description = "RenderTexture Visualizer")]

namespace SS14.Client.Debug
{
    public class TextureVisualizerObjectSource : VisualizerObjectSource
    {
        public override void GetData(object target, Stream outgoingData)
        {
            var tex = (SFML.Graphics.Texture)target;
            using (var img = tex.CopyToImage())
            {
                var writer = new BinaryWriter(outgoingData);
                var pixels = img.Pixels;
                writer.Write(img.Size.X);
                writer.Write(img.Size.Y);
                writer.Write(pixels.Length);
                writer.Write(pixels, 0, pixels.Length);
                outgoingData.Flush();
            }
        }
    }
    public class RenderTextureVisualizerObjectSource : VisualizerObjectSource
    {
        public override void GetData(object target, Stream outgoingData)
        {
            var rt = (SFML.Graphics.RenderTexture)target;
            new TextureVisualizerObjectSource().GetData(rt.Texture, outgoingData);
        }
    }

    public class TextureVisualizer : DialogDebuggerVisualizer
    {
        protected override void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider)
        {
            if (windowService == null)
                throw new ArgumentNullException("windowService");
            if (objectProvider == null)
                throw new ArgumentNullException("objectProvider");

            var reader = new BinaryReader(objectProvider.GetData());
            var width  = reader.ReadInt32(); // unchecked uint to int conversion
            var height = reader.ReadInt32(); // unchecked uint to int conversion
            var src = reader.ReadBytes(reader.ReadInt32());
            
            using (var form = new Form())
            using (var pb = new PictureBox())
            {
                var bmp = new Bitmap(width, height);
                var data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                var length = height * data.Stride;
                var pixels = new byte[length];
                for (int i = 0; i < length; i += 4)
                {
                    pixels[i + 0] = src[i + 2];
                    pixels[i + 1] = src[i + 1];
                    pixels[i + 2] = src[i + 0];
                    pixels[i + 3] = src[i + 3];
                }
                System.Runtime.InteropServices.Marshal.Copy(pixels, 0, data.Scan0, length);
                bmp.UnlockBits(data);

                var transGrid = new Bitmap(16, 16); // Checkerboard pattern for transparent images.
                using (var g = System.Drawing.Graphics.FromImage(transGrid))
                {
                    g.Clear(Color.FromArgb(102, 102, 102));
                    var brush = new SolidBrush(Color.FromArgb(153, 153, 153));
                    g.FillRectangle(brush, 0, 0, 8, 8);
                    g.FillRectangle(brush, 8, 8, 8, 8);
                }
                
                form.Controls.Add(pb);
                form.ClientSize = bmp.Size;
                form.Width = Math.Max(form.Width, 200);
                form.Text = string.Format("Texture Visualizer ({0:#,##0} x {1:#,##0})", width, height);
                form.BackgroundImage = transGrid;
                form.BackgroundImageLayout = ImageLayout.Tile;
                pb.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right | AnchorStyles.Left;
                pb.Bounds = form.ClientRectangle;
                pb.SizeMode = PictureBoxSizeMode.Zoom;
                pb.Image = bmp;
                pb.BackColor = Color.Transparent;

                windowService.ShowDialog(form);
            }
        }
        
        //public static void TestShowVisualizer(object objectToVisualize)
        //{
        //    VisualizerDevelopmentHost visualizerHost = new VisualizerDevelopmentHost(objectToVisualize, typeof(TextureVisualizer));
        //    visualizerHost.ShowVisualizer();
        //}
    }
}

#endif
