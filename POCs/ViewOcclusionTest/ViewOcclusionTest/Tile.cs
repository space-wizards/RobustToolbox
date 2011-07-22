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
    public class Tile
    {
        public int x;
        public int y;
        public Rectangle rect;
        public Tile[,] tileArray;
        public int tileSize;
        private bool solid;
        public MainWindow mainWindow;

        Canvas canvas;

        //FACES
        Face faceN = null;
        Face faceE = null;
        Face faceS = null;
        Face faceW = null;

        public bool IsSolid
        {
            get { return solid; }
            set
            {
                solid = value;
                if (solid)
                {
                    GenerateFaces();
                    PopulateOverlapFaces();
                }
                else
                {
                    //Clear faces
                    if (faceN != null && faceN.over != null)
                        faceN.over.over = null;
                    faceN = null;
                    if (faceS != null && faceS.over != null)
                        faceS.over.over = null;
                    faceS = null;
                    if (faceE != null && faceE.over != null)
                        faceE.over.over = null;
                    faceE = null;
                    if (faceW != null && faceW.over != null)
                        faceW.over.over = null;
                    faceW = null;
                }

                if(!inwindow)
                    return;
                
            }
        }
        private bool inwindow = true;
        public bool IsInWindow
        {
            get { return inwindow; }
            set
            {
                inwindow = value;
                if (inwindow)
                {
                    SetSolidDisplay(solid);
                }
                else
                {
                    rect.Fill = Brushes.White;
                    rect.Stroke = Brushes.White;
                }
            }
        }

        public void SetSolidDisplay(bool display)
        {
            if(display)
            {
                rect.Fill = Brushes.Black;
                rect.Stroke = Brushes.White;
            }
            else
            {
                rect.Fill = Brushes.White;
                rect.Stroke = Brushes.White;
            }
        }

        public Tile(int _x, int _y, Rectangle _rect, MainWindow _mainWindow)
        {
            x = _x; // These are the array coords
            y = _y;
            rect = _rect;
            mainWindow = _mainWindow;
            tileArray = mainWindow.tileArray;
            tileSize = mainWindow.TileSize;
            canvas = mainWindow.canvas;
        }

        public void GenerateFaces()
        {
            //Faces are defined as the start point and the next adjoining face. We're only dealing with squares so no need to do lots of general poly shite.
            //The point defined per face is the right vertex of the face as if you were inside the square.
            faceW = new Face();
            faceW.x = x * tileSize;
            faceW.y = y * tileSize;
            faceS = new Face();
            faceS.x = x * tileSize;
            faceS.y = (y + 1) * tileSize;
            faceE = new Face();
            faceE.x = (x + 1) * tileSize;
            faceE.y = (y + 1) * tileSize;
            faceN = new Face();
            faceN.x = (x + 1) * tileSize;
            faceN.y = y * tileSize;

            faceW.next = faceS;
            faceS.next = faceE;
            faceE.next = faceN;
            faceN.next = faceW;
        }

        /// <summary>
        /// Populate overlapping faces
        /// </summary>
        public void PopulateOverlapFaces()
        {
            //North
            if (y > 0 && faceN != null)
                faceN.over = tileArray[x, y - 1].faceS;
            //South
            if (y < mainWindow.arrayDims.Y - 1 && faceS != null)
                faceS.over = tileArray[x, y + 1].faceN;
            //East
            if (x < mainWindow.arrayDims.X - 1 && faceE != null)
                faceE.over = tileArray[x + 1, y].faceW;
            //West
            if (x > 0 && faceW != null)
                faceW.over = tileArray[x - 1, y].faceE;
        }

        public void DrawLines()
        {
            if (!IsSolid)
                return;

            if (IsInWindow)
            {
                if (faceW.line == null)
                {
                    faceW.line = new Line();
                    faceW.SetLinePoints();
                    canvas.Children.Add(faceW.line);
                    Canvas.SetLeft(faceW.line, 0);
                    Canvas.SetTop(faceW.line, 0);
                    Canvas.SetZIndex(faceW.line, 20);
                    if (faceW.IsFacing(mainWindow.viewPoint) && faceW.over == null)
                    {
                        faceW.SetLineColor(Brushes.Green);
                        Canvas.SetZIndex(faceW.line, 21);
                    }
                    else
                    {
                        faceW.SetLineColor(Brushes.Black);
                        Canvas.SetZIndex(faceW.line, 20);
                    }
                }
                if (faceS.line == null)
                {
                    faceS.line = new Line();
                    faceS.SetLinePoints();
                    canvas.Children.Add(faceS.line);
                    Canvas.SetLeft(faceS.line, 0);
                    Canvas.SetTop(faceS.line, 0);
                    Canvas.SetZIndex(faceS.line, 20);
                    if (faceS.IsFacing(mainWindow.viewPoint) && faceS.over == null)
                    {
                        faceS.SetLineColor(Brushes.Green);
                        Canvas.SetZIndex(faceS.line, 21);
                    }
                    else
                    {
                        faceS.SetLineColor(Brushes.Black);
                        Canvas.SetZIndex(faceS.line, 20);
                    }
                }

                if (faceE.line == null)
                {
                    faceE.line = new Line();
                    faceE.SetLinePoints();
                    canvas.Children.Add(faceE.line);
                    Canvas.SetLeft(faceE.line, 0);
                    Canvas.SetTop(faceE.line, 0);
                    Canvas.SetZIndex(faceE.line, 20);
                    if (faceE.IsFacing(mainWindow.viewPoint) && faceE.over == null)
                    {
                        faceE.SetLineColor(Brushes.Green);
                        Canvas.SetZIndex(faceE.line, 21);
                    }
                    else
                    {
                        faceE.SetLineColor(Brushes.Black);
                        Canvas.SetZIndex(faceE.line, 20);
                    }
                }

                if (faceN.line == null)
                {
                    faceN.line = new Line();
                    faceN.SetLinePoints();
                    canvas.Children.Add(faceN.line);
                    Canvas.SetLeft(faceN.line, 0);
                    Canvas.SetTop(faceN.line, 0);
                    Canvas.SetZIndex(faceN.line, 20);
                    if (faceN.IsFacing(mainWindow.viewPoint) && faceN.over == null)
                    {
                        faceN.SetLineColor(Brushes.Green);
                        Canvas.SetZIndex(faceN.line, 21);
                    }
                    else
                    {
                        faceN.SetLineColor(Brushes.Black);
                        Canvas.SetZIndex(faceN.line, 20);
                    }

                }
            }
            else
            {
                RemoveLines();
            }

        }

        public void RemoveLines()
        {
            if (!IsSolid)
                return;
            canvas.Children.Remove(faceW.line);
            faceW.line = null;
            canvas.Children.Remove(faceS.line);
            faceS.line = null;
            canvas.Children.Remove(faceE.line);
            faceE.line = null;
            canvas.Children.Remove(faceN.line);
            faceN.line = null;
        }
    }
}
