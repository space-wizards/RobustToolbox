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

namespace GasPropagationTest
{
    public class GasCell
    {
        Ellipse attachedellipse;
        double x;
        double y;
        public double nextGasAmount = 0.1;
        public double gasAmount = 0.1;
        int circleDiameter = 15;
        public bool sink = false;

        public GasCell(Ellipse e, double _x, double _y)
        {
            attachedellipse = e;
            x = _x;
            y = _y;
        }

        public void Update()
        {
            if (sink)
                nextGasAmount = 0;
            gasAmount = nextGasAmount;
            
            SetRadius(circleDiameter * gasAmount/2);
        }

        public void SetRadius(double r)
        {
            if (r <= 7.5)
            {
                attachedellipse.Fill = Brushes.Blue;
                attachedellipse.Height = 2 * r;
                attachedellipse.Width = 2 * r;
                Canvas.SetLeft(attachedellipse, 7.5 - r + x);
                Canvas.SetTop(attachedellipse, 7.5 - r + y);
                return;
            }
            else if (r <= 15 && r > 7.5) 
            {
                attachedellipse.Fill = Brushes.Purple;
            }
            else
            {
                attachedellipse.Fill = Brushes.Red;
            }
            r = 7.5;
            attachedellipse.Height = 2 * r;
            attachedellipse.Width = 2 * r;
            Canvas.SetLeft(attachedellipse, 7.5 - r + x);
            Canvas.SetTop(attachedellipse, 7.5 - r + y);
        }
    }
}
