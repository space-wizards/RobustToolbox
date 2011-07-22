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
    class Face
    {
        public int x;
        public int y;
        public Face next; //Next face counter-clockwise
        public Face over = null; //Overlapping face from adjacent tile
        public Line line;
        
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
    }

    
}
