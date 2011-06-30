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
        public double nextGasAmount = 10;
        public double gasAmount = 10;
        int circleDiameter = 15;
        public bool sink = false;
        public bool blocking = false;

        public GasCell(Ellipse e, double _x, double _y)
        {
            attachedellipse = e;
            x = _x;
            y = _y;
            SetGasDisplay();
        }

        public void Update()
        {
            if (sink || blocking)
                nextGasAmount = 0;

            if (gasAmount != nextGasAmount)
            {
                gasAmount = nextGasAmount;
                SetGasDisplay();
            }

            if (blocking)
            {
                attachedellipse.Height = 15;
                attachedellipse.Width = 15;
                Canvas.SetLeft(attachedellipse, x);
                Canvas.SetTop(attachedellipse, y);
                attachedellipse.Fill = Brushes.Black;
            }
            if (sink)
            {
                attachedellipse.Height = 15;
                attachedellipse.Width = 15;
                Canvas.SetLeft(attachedellipse, x);
                Canvas.SetTop(attachedellipse, y);
                attachedellipse.Fill = Brushes.LightGray;
            }
                
        }

        public void SetGasDisplay()
        {
            float gas = (float)gasAmount;
            if (gas < 1)
                gas = 1;
            else if (gas > 10000)
                gas = 10000;

            float blue = (10 / (float)Math.Sqrt(gas));
            float red = gas/1000;
            if (red > 1)
                red = 1;
            if (blue > 1)
                blue = 1;

            Color c = new Color();
            c.R = (byte)Math.Round(red * 255);
            c.G = 0;
            c.B = (byte)Math.Round(blue * 255);
            c.A = 255;
            attachedellipse.Fill = new SolidColorBrush(c);
            SetRadius(circleDiameter * gasAmount / 200);
        }

        public void SetRadius(double r)
        {
            if(r > 7.5)
                r = 7.5;
            attachedellipse.Height = 2 * r;
            attachedellipse.Width = 2 * r;
            Canvas.SetLeft(attachedellipse, 7.5 - r + x);
            Canvas.SetTop(attachedellipse, 7.5 - r + y);
        }
    }
}
