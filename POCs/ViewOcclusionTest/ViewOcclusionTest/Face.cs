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
    public class Face
    {
        public int x;
        public int y;
        public Face next; //Next face counter-clockwise
        public Face over = null; //Overlapping face from adjacent tile
        public Line line;
        public Line startLine;
        public Line endLine;
        public Canvas canvas;
        public MainWindow mainWindow;

        public Face(MainWindow _mainWindow)
        {
            mainWindow = _mainWindow;
            canvas = mainWindow.canvas;
        }
        
        public bool IsFacing(Point p)
        {
            double prod = perp_dot(x, y, next.x, next.y, p.X, p.Y); 
            if (prod > 0)
                return true;
            return false;
        }

        //Negative if test vector is to the left, positive if to the right.
        public double perp_dot(double originx, double originy, double endx, double endy, double testx, double testy)
        {
            double v1x = endx - originx;
            double v1y = endy - originy;
            double v2x = testx - originx;
            double v2y = testy - originy;
            return (v1x * v2y - v1y * v2x);
        }

        public void SetLinePoints()
        {
            line.X1 = x;
            line.Y1 = y;
            line.X2 = next.x;
            line.Y2 = next.y;
        }

        public void SetLineColor(Brush brush)
        {
            line.Stroke = brush;
        }

        public void DrawLine(Point viewPoint)
        {
            DrawLine(IsFacing(viewPoint));     
        }

        public void DrawLine(bool facing)
        {
            if (line != null)
                canvas.Children.Remove(line);
            line = new Line();
            SetLinePoints();
            canvas.Children.Add(line);
            Canvas.SetLeft(line, 0);
            Canvas.SetTop(line, 0);
            if (facing && over == null)
            {
                SetLineColor(Brushes.Green);
                Canvas.SetZIndex(line, 21);
                //DrawViewLine(ViewLines.both);
            }
            else
            {
                SetLineColor(Brushes.Black);
                Canvas.SetZIndex(line, 20);
            }
        }

        public void DrawViewLine(ViewLines viewlines)
        {
            double slope;
            switch (viewlines)
            {
                case ViewLines.origin:
                    if (y - mainWindow.viewPoint.Y == 0)
                        slope = 0;
                    else if (x - mainWindow.viewPoint.X == 0)
                        slope = -1 * Math.Sign(y - mainWindow.viewPoint.Y) * 10000;
                    else //Calculate y slope
                        slope = (y - mainWindow.viewPoint.Y) / (x - mainWindow.viewPoint.X);

                    //Generate line
                    startLine = new Line();
                    startLine.X1 = x;
                    startLine.Y1 = y; //Start point


                    if (x > mainWindow.viewPoint.X)
                        startLine.X2 = mainWindow.WindowBottomRight.X;
                    else
                        startLine.X2 = mainWindow.WindowTopLeft.X;
                    startLine.Y2 = slope * startLine.X2 + (startLine.Y1 - startLine.X1 * slope); // y = mx + b ololol
                    
                    startLine.Stroke = Brushes.Blue;
                    canvas.Children.Add(startLine);
                    Canvas.SetLeft(startLine, 0);
                    Canvas.SetTop(startLine, 0);
                    Canvas.SetZIndex(startLine, 30);
                    break;
                case ViewLines.endpoint:
                    if (next.y - mainWindow.viewPoint.Y == 0)
                        slope = 0;
                    else if (next.x - mainWindow.viewPoint.X == 0)
                        slope = -1 * Math.Sign(next.y - mainWindow.viewPoint.Y) * 1000000;
                    else //Calculate y slope
                        slope = (next.y - mainWindow.viewPoint.Y) / (next.x - mainWindow.viewPoint.X);
                    endLine = new Line();
                    endLine.X1 = next.x;
                    endLine.Y1 = next.y;
                    if (next.x > mainWindow.viewPoint.X)
                        endLine.X2 = mainWindow.WindowBottomRight.X;
                    else
                        endLine.X2 = mainWindow.WindowTopLeft.X;
                    endLine.Y2 = slope * endLine.X2 + (endLine.Y1 - endLine.X1 * slope);
                    endLine.Stroke = Brushes.Red;
                    canvas.Children.Add(endLine);
                    Canvas.SetLeft(endLine, 0);
                    Canvas.SetTop(endLine, 0);
                    Canvas.SetZIndex(endLine, 30);
                    break;
                case ViewLines.both:
                    DrawViewLine(ViewLines.origin);
                    DrawViewLine(ViewLines.endpoint);
                    break;
            }
        }

        public void RemoveLines()
        {
            canvas.Children.Remove(line);
            line = null;
            canvas.Children.Remove(startLine);
            startLine = null;
            canvas.Children.Remove(endLine);
            endLine = null;
        }

        public enum ViewLines
        {
            origin,
            endpoint,
            both
        }

        //Get the same face on the adjacent tile in either the direction of the origin or the endpoint.
        public Face GetAdjacentFace(bool origin)
        {
            if (origin && next.next.next.over != null)
                return next.next.next.over.next.next.next; //I know, this is fucking kludgy.
            else if (!origin && next.over != null)
                return next.over.next;
            else
                return null;
        }

        public Polygon GetOcclusionPoly()
        {
            Polygon p = new Polygon();
            PointCollection polygonPoints = new PointCollection();
            Face checkFace = this;
            polygonPoints.Add(new Point(x,y)); //Add start point
            int i = 0;
            while (checkFace.endLine == null && i < 30)
            {
                if (checkFace.GetAdjacentFace(false) == null)
                    checkFace = checkFace.next;
                else if (checkFace.GetAdjacentFace(false) != null && checkFace.GetAdjacentFace(false).over != null)
                {
                    checkFace = checkFace.GetAdjacentFace(false).over.next;
                    polygonPoints.Add(new Point(checkFace.x, checkFace.y));
                }
                else
                    checkFace = checkFace.GetAdjacentFace(false);
                i++;
            }
            if (checkFace.endLine != null)
            {
                polygonPoints.Add(new Point(checkFace.endLine.X1, checkFace.endLine.Y1));
                polygonPoints.Add(new Point(checkFace.endLine.X2, checkFace.endLine.Y2));
            }
            else
                return null;
            
            polygonPoints.Add(new Point(startLine.X2, startLine.Y2));
            p.Points = polygonPoints;
            p.Stroke = Brushes.Black;
            p.Fill = Brushes.Black;
            
            return p;
        }
    }

    
}
