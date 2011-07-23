using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ViewOcclusionTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public int TileSize = 15;
        public DateTime lastUpdate;
        public DateTime lastDraw;
        public DateTime lastDataUpdate;
        public Tile[,] tileArray;
        public Point arrayDims;

        public Point viewPoint;
        public Ellipse viewer;
        public Point WindowTopLeft;
        public Point WindowBottomRight;

        public MainWindow()
        {
            arrayDims = new Point(60, 60);
            tileArray = new Tile[60, 60];
            
            InitializeComponent();
            Initialize();

            CompositionTarget.Rendering += Update;
            MouseDown += MouseDownEventHandler;
        }

        public void Initialize()
        {
            //Set up viewpoint dot
            viewer = new Ellipse();
            viewer.Width = 5;
            viewer.Height = 5;
            viewer.Stroke = Brushes.Red;
            viewer.Fill = Brushes.Red;
            canvas.Children.Add(viewer);
            Canvas.SetZIndex(viewer, 10);
            setViewPoint(new Point(300, 300), true);

            lastUpdate = DateTime.Now;
            lastDraw = DateTime.Now;
            lastDataUpdate = DateTime.Now;
            Rectangle r;
            for (int i = 0; i < arrayDims.X; i++)
            {
                for (int j = 0; j < arrayDims.Y; j++)
                {
                    r = new Rectangle();
                    r.Width = TileSize;
                    r.Height = TileSize;
                    canvas.Children.Add(r);
                    Canvas.SetLeft(r, (double)i * TileSize);
                    Canvas.SetTop(r, (double)j * TileSize);
                    Canvas.SetZIndex(r, 1);
                    tileArray[i, j] = new Tile(i,j,r,this);
                    tileArray[i, j].IsSolid = false;
                }
            }
            Random rand = new Random();
            for (int i = 0; i < 400; i++)
            {
                tileArray[rand.Next(0, (int)arrayDims.X), rand.Next(0, (int)arrayDims.Y)].IsSolid = true;
            }

            CullViewFrustum();
            
        }

        private void MouseDownEventHandler(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition(this);

            if (e.LeftButton == MouseButtonState.Pressed)
                setViewPoint(p);
        }

        public void Update(object sender, EventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.A) && viewPoint.X >= 1)
                setViewPoint(new Point(viewPoint.X - 3, viewPoint.Y));
            if (Keyboard.IsKeyDown(Key.D) && viewPoint.X <= 650)
                setViewPoint(new Point(viewPoint.X + 3, viewPoint.Y));
            if (Keyboard.IsKeyDown(Key.W) && viewPoint.Y >= 1)
                setViewPoint(new Point(viewPoint.X, viewPoint.Y - 3));
            if (Keyboard.IsKeyDown(Key.S) && viewPoint.Y <= 650)
                setViewPoint(new Point(viewPoint.X, viewPoint.Y + 3));

        }

        public void CullViewFrustum()
        {
            //Populate faces
            for (int i = 0; i < arrayDims.X; i++)
            {
                for (int j = 0; j < arrayDims.Y; j++)
                {
                    tileArray[i, j].PopulateOverlapFaces();
                }
            }

            for (int i = 0; i < arrayDims.X; i++)
            {
                for (int j = 0; j < arrayDims.Y; j++)
                {
                    if (i * TileSize < WindowTopLeft.X || i * TileSize > WindowBottomRight.X ||
                        j * TileSize > WindowBottomRight.Y || j * TileSize < WindowTopLeft.Y)
                    {
                        tileArray[i, j].IsInWindow = false;
                        tileArray[i, j].RemoveLines();
                        tileArray[i, j].RemoveOcclusionPoly();
                    }
                    else
                    {
                        tileArray[i, j].IsInWindow = true;
                        tileArray[i, j].RemoveLines();
                        tileArray[i, j].DrawLines();
                    }
                }
            }

            for (int i = 0; i < arrayDims.X; i++)
            {
                for (int j = 0; j < arrayDims.Y; j++)
                {
                    tileArray[i, j].DrawOcclusionPoly();
                }
            }

        }

        public void setViewPoint(Point p, bool initial = false)
        {
            viewPoint = p;

            Canvas.SetLeft(viewer, p.X);
            Canvas.SetTop(viewer, p.Y);

            //Set window
            WindowTopLeft = new Point(p.X - 200, p.Y - 200);
            WindowBottomRight = new Point(p.X + 160, p.Y + 160);

            if(!initial)
                CullViewFrustum();
        }
    }
}
