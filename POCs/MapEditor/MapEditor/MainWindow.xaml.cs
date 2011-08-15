using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace MapEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string[,] tileStrings;
        private Rectangle[,] rectangles;
        private TextBlock[,] textBlocks;
        int xTopLeft = 12;
        int yTopLeft = 60;
        int tileSize = 14;
        string brush = "Floor";
        bool mapLoaded = false;

        bool wideSelect = false;
        bool topLeftCornerSelected = false;
        int topLeftCornerx;
        int topLeftCornery;

        int xMax;
        int yMax;
        public MainWindow()
        {
            InitializeComponent();
            MouseDown += new MouseButtonEventHandler(MainWindow_MouseDown);
        }

        void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (mapLoaded)
            {
                var pos = e.GetPosition(this);
                if (pos.X > xTopLeft && pos.X < xTopLeft + (tileSize * xMax) + tileSize 
                    && pos.Y > yTopLeft && pos.Y < yTopLeft + (tileSize * yMax) + tileSize) //If its inside the canvas bounds
                {
                    //Translate to array position.
                    int arrx = (int)Math.Floor((pos.X - xTopLeft) / tileSize);
                    int arry = (int)Math.Floor((pos.Y - yTopLeft) / tileSize);
                    
                    if (!wideSelect)
                    {
                        tileStrings[arrx, arry] = brush;
                        updateTile(arrx, arry);
                    }
                    else if (wideSelect & !topLeftCornerSelected)
                    {
                        topLeftCornerSelected = true;
                        topLeftCornerx = arrx;
                        topLeftCornery = arry;
                        areaSelectButton.Content = "Bottom Right";
                    }
                    else if (wideSelect & topLeftCornerSelected)
                    {
                        wideSelect = false;
                        topLeftCornerSelected = false;
                        areaSelectButton.Content = "Area Select";
                        for (int i = topLeftCornerx; i <= arrx; i++)
                        {
                            for (int j = topLeftCornery; j <= arry; j++)
                            {
                                tileStrings[i, j] = brush;
                                updateTile(i, j);
                            }
                        }
                    }
                }
            }
        }

        private void loadmap_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog loadDialog = new OpenFileDialog();

            loadDialog.DefaultExt = "*.*";
            loadDialog.Filter = "All Files(*.*)|*.*";

            Nullable<bool> result = loadDialog.ShowDialog();

            if (result == true)
            {
                loadFile(loadDialog.FileName);
                drawMap();
                mapLoaded = true;
            }
        }

        private void loadFile(string filename)
        {
            var f = File.ReadAllLines(filename).GetEnumerator();
            f.MoveNext();
            xMax = Convert.ToInt32(f.Current);
            f.MoveNext();
            yMax = Convert.ToInt32(f.Current);
            f.MoveNext();
            tileStrings = new string[xMax,yMax];

            for (int i = 0; i < xMax; i++)
            {
                for (int j = 0; j < yMax; j++)
                {
                    tileStrings[j, i] = f.Current.ToString();
                    f.MoveNext();
                }
            }
            return;
        }

        private void savemap_Button_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            //saveDialog.DefaultExt = "*.*";
            //saveDialog.Filter = "All Files(*.*)|*.*";
            //saveDialog.ValidateNames = false;

            Nullable<bool> result = saveDialog.ShowDialog();

            if (result == true)
            {
                saveFile(saveDialog.FileName);
            }
        }

        private void saveFile(string filename)
        {
            var f = new StreamWriter(filename);
            f.WriteLine(xMax.ToString());
            f.WriteLine(yMax.ToString());
            for (int i = 0; i < xMax; i++)
            {
                for (int j = 0; j < yMax; j++)
                {
                    f.WriteLine(tileStrings[j, i]);
                }
            }

            f.Close();
           
        }

        private void drawMap()
        {
            canvas.Children.Clear();
            rectangles = new Rectangle[xMax, yMax];
            textBlocks = new TextBlock[xMax, yMax];
            for (int i = 0; i < xMax; i++)
            {
                for (int j = 0; j < yMax; j++)
                {
                    Rectangle r = new Rectangle();
                    r.Height = tileSize;
                    r.Width = tileSize;
                    r.Stroke = Brushes.Black;
                    
                    canvas.Children.Add(r);
                    Canvas.SetZIndex(r, 0);
                    Canvas.SetTop(r,j * tileSize);
                    Canvas.SetLeft(r,i * tileSize);

                    TextBlock t = new TextBlock();
                    t.FontSize = 13;

                    canvas.Children.Add(t);
                    Canvas.SetZIndex(t, 10);
                    Canvas.SetTop(t, (j * tileSize) - 1);
                    Canvas.SetLeft(t, (i * tileSize) + 2);
                    rectangles[i, j] = r;
                    textBlocks[i, j] = t;
                    updateTile(i, j);

                }
            }
        }

        private void updateTile(int x, int y)
        {
            TextBlock t = textBlocks[x, y];
            Rectangle r = rectangles[x, y];
            switch (tileStrings[x, y])
            {
                case "Floor":
                    t.Text = "F";
                    t.Foreground = Brushes.Black;
                    r.Fill = Brushes.LightGray;
                    break;
                case "Space":
                    t.Text = "S";
                    t.Foreground = Brushes.White;
                    r.Fill = Brushes.Black;
                    break;
                case "Wall":
                    t.Text = "W";
                    t.Foreground = Brushes.White;
                    r.Fill = Brushes.Brown;
                    break;
                case "None":
                    t.Text = "";
                    r.Fill = Brushes.LightSteelBlue;
                    break;
            }
        }

        public enum TileType
        {
            Floor,
            Wall,
            Space,
            None
        }

        private void Palette_floor_MouseDown(object sender, MouseButtonEventArgs e)
        {
            brush = "Floor";
        }

        private void Palette_none_MouseDown(object sender, MouseButtonEventArgs e)
        {
            brush = "Space";
        }

        private void Palette_space_MouseDown(object sender, MouseButtonEventArgs e)
        {
            brush = "Space";
        }

        private void Palette_wall_MouseDown(object sender, MouseButtonEventArgs e)
        {
            brush = "Wall";
        }

        private void areaSelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!wideSelect)
            {
                areaSelectButton.Content = "Top Left";
                wideSelect = true;
            }
            else // Turn off wide select
            {
                wideSelect = false;
                topLeftCornerSelected = false;
                areaSelectButton.Content = "Area Select";
            }
        }

    }
}
