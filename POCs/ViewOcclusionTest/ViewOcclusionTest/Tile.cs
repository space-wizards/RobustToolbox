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
        public Polygon poly;

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
            faceW = new Face(mainWindow);
            faceW.x = x * tileSize;
            faceW.y = y * tileSize;
            faceS = new Face(mainWindow);
            faceS.x = x * tileSize;
            faceS.y = (y + 1) * tileSize;
            faceE = new Face(mainWindow);
            faceE.x = (x + 1) * tileSize;
            faceE.y = (y + 1) * tileSize;
            faceN = new Face(mainWindow);
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
                faceW.DrawLine(mainWindow.viewPoint);
                faceS.DrawLine(mainWindow.viewPoint);
                faceE.DrawLine(mainWindow.viewPoint);
                faceN.DrawLine(mainWindow.viewPoint);

                DrawViewLines();
            }
            else
            {
                RemoveLines();
            }

        }

        public void DrawViewLines()
        {
            if(!IsSolid)
                return;

            Point vp = mainWindow.viewPoint;

            //Draw endpoints.
            if (faceW.IsFacing(vp) && !faceW.next.IsFacing(vp) && faceW.over == null && faceW.GetAdjacentFace(false) == null)
                faceW.DrawViewLine(Face.ViewLines.endpoint);
            if (faceS.IsFacing(vp) && !faceS.next.IsFacing(vp) && faceS.over == null && faceS.GetAdjacentFace(false) == null)
                faceS.DrawViewLine(Face.ViewLines.endpoint);
            if (faceE.IsFacing(vp) && !faceE.next.IsFacing(vp) && faceE.over == null && faceE.GetAdjacentFace(false) == null)
                faceE.DrawViewLine(Face.ViewLines.endpoint);
            if (faceN.IsFacing(vp) && !faceN.next.IsFacing(vp) && faceN.over == null && faceN.GetAdjacentFace(false) == null)
                faceN.DrawViewLine(Face.ViewLines.endpoint);

            //Draw startpoints.
            if (faceW.IsFacing(vp) && !faceW.next.next.next.IsFacing(vp) && faceW.over == null && faceW.GetAdjacentFace(true) == null)
                faceW.DrawViewLine(Face.ViewLines.origin);
            if (faceS.IsFacing(vp) && !faceS.next.next.next.IsFacing(vp) && faceS.over == null && faceS.GetAdjacentFace(true) == null)
                faceS.DrawViewLine(Face.ViewLines.origin);
            if (faceE.IsFacing(vp) && !faceE.next.next.next.IsFacing(vp) && faceE.over == null && faceE.GetAdjacentFace(true) == null)
                faceE.DrawViewLine(Face.ViewLines.origin);
            if (faceN.IsFacing(vp) && !faceN.next.next.next.IsFacing(vp) && faceN.over == null && faceN.GetAdjacentFace(true) == null)
                faceN.DrawViewLine(Face.ViewLines.origin);

        }

        public void RemoveOcclusionPoly()
        {
            if (poly != null)
            {
                canvas.Children.Remove(poly);
                poly = null;
            }
        }

        public void DrawOcclusionPoly()
        {
            if (!IsSolid || !IsInWindow)
                return;

            RemoveOcclusionPoly();

            if (faceW.startLine != null)
                poly = faceW.GetOcclusionPoly();
            if (faceS.startLine != null)
                poly = faceS.GetOcclusionPoly();
            if (faceE.startLine != null)
                poly = faceE.GetOcclusionPoly();
            if (faceN.startLine != null)
                poly = faceN.GetOcclusionPoly();

            if (poly != null)
            {
                canvas.Children.Add(poly);
                Canvas.SetZIndex(poly, 40);
            }



        }

        public void RemoveLines()
        {
            if (!IsSolid)
                return;
            faceW.RemoveLines();
            faceS.RemoveLines();
            faceE.RemoveLines();
            faceN.RemoveLines();
        }



        public enum Faces
        {
            west,
            south,
            east,
            north
        }
    }
}
