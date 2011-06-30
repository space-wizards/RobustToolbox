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
        GasCell[,] cellArray;
        public int arrX;
        public int arrY;
        double x;
        double y;
        public double nextGasAmount = 10;
        public double gasAmount = 10;
        int circleDiameter = 15;
        public bool sink = false;
        public bool blocking = false;
        private float FlowConstant = 0.1f;
        bool calculated = true;
        private double vx=0;
        private double vy=0;
        private double nextvx = 0;
        private double nextvy = 0;

        public GasCell(Ellipse e, double _x, double _y, GasCell[,] _cellArray)
        {
            attachedellipse = e;
            x = _x;
            y = _y;
            SetGasDisplay();
            cellArray = _cellArray;
        }

        public void Update(bool draw)
        {
            if (sink || blocking)
                nextGasAmount = 0;

            vx = nextvx;
            vy = nextvy;
            nextvx = 0;
            nextvy = 0;

            if (gasAmount != nextGasAmount)
            {
                gasAmount = nextGasAmount;
                if(draw)
                    SetGasDisplay();
            }

            if (blocking && attachedellipse.Fill != Brushes.Black)
            {
                attachedellipse.Height = 15;
                attachedellipse.Width = 15;
                Canvas.SetLeft(attachedellipse, x);
                Canvas.SetTop(attachedellipse, y);
                attachedellipse.Fill = Brushes.Black;
            }
            if (sink && attachedellipse.Fill != Brushes.LightGray)
            {
                attachedellipse.Height = 15;
                attachedellipse.Width = 15;
                Canvas.SetLeft(attachedellipse, x);
                Canvas.SetTop(attachedellipse, y);
                attachedellipse.Fill = Brushes.LightGray;
            }
            calculated = false;
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

        public void CalculateNextGasAmount()
        {
            if (blocking)
                return;
            double DAmount;
            double Flow;
            double FlowX;
            double FlowY;
            float RateConstant = 1;
            GasCell neighbor;
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) // If we're on this cell
                        continue;
                    //if (Math.Abs(i) + Math.Abs(j) == 2)
                      //  continue;
                    if (arrX + i < 0 || arrX + i > 38 || arrY + j < 0 || arrY + j > 38) // If out of bounds
                        continue;

                    neighbor = cellArray[arrX+i,arrY+j];
                    if (neighbor.calculated || neighbor.blocking)
                        continue;

                    DAmount = gasAmount - neighbor.gasAmount;
                    if (DAmount == 0)
                    {
                        return;
                    }
                    /// Calculate change due to velocity
                    //FlowX = 0; FlowY = 0;
                    //FlowX = vx * .15;
                    //FlowY = vy * .15;
                                        
                    ///Calculate change
                    Flow = FlowConstant * DAmount;// +FlowX + FlowY;
                    Flow = Clamp(Flow, gasAmount / 4, neighbor.gasAmount / 4);
                    nextGasAmount -= Flow * RateConstant;
                    neighbor.nextGasAmount += Flow * RateConstant;

                    /*//Calculate new velocity
                    double v = Flow * RateConstant;
                    if (Flow > 0)
                    {
                        neighbor.nextvx += v * i;
                        neighbor.nextvy += v * j;
                    }
                    else if (Flow < 0)
                    {
                        nextvx += v * i;
                        nextvy += v * j;
                    }*/

                    if(nextGasAmount < 0)
                        nextGasAmount = 0;
                    if (neighbor.nextGasAmount < 0)
                        neighbor.nextGasAmount = 0;
                      
                    calculated = true;
                }
        }

        public double Clamp(double Flow, double amount, double neighamount)
        {
            double clampedFlow = Flow;

            if (amount - clampedFlow < 0)
                clampedFlow = clampedFlow + (amount - clampedFlow);
            if (neighamount + clampedFlow < 0)
                clampedFlow = clampedFlow + (neighamount + clampedFlow);

            return clampedFlow;
        }
    }
}
