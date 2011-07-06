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
        public Vector2 GasVel;
        public Vector2 NextGasVel;
        public const double quarterpi = Math.PI / 4;

        public GasCell(Ellipse e, double _x, double _y, GasCell[,] _cellArray)
        {
            attachedellipse = e;
            x = _x;
            y = _y;
            SetGasDisplay();
            cellArray = _cellArray;
            GasVel = new Vector2(0, 0);
            NextGasVel = new Vector2(0, 0);
        }

        public void Update(bool draw)
        {
            if (sink || blocking)
                nextGasAmount = 0;

            GasVel = NextGasVel;
            NextGasVel = new Vector2(0, 0);

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
            if (r < 0)
                r = 0;
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
            double Flow = 0;
            
            //Constants
            double SourceDamping = .5;
            double RecieverDamping = .9;

            GasCell neighbor;
            Random rand = new Random();
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) // If we're on this cell
                        continue;
                    if (arrX + i < 0 || arrX + i > 38 || arrY + j < 0 || arrY + j > 38) // If out of bounds
                        continue;

                    neighbor = cellArray[arrX+i,arrY+j];
                    if (neighbor.calculated || neighbor.blocking) // TODO work out rebound
                        continue;

                    DAmount = gasAmount - neighbor.gasAmount;
                    if (DAmount == 0 || Math.Abs(DAmount) < 0.1)
                    {
                        return;
                    }

                    ///Calculate initial flow
                    Flow = FlowConstant * DAmount;
                    Flow = Clamp(Flow, gasAmount / 8, neighbor.gasAmount / 8);

                    //Velocity application code
                    Vector2 Dir = new Vector2(i, j);
                    double proportion = 0;
                    double componentangle;
                    //Process velocity flow to neighbor
                    if (GasVel.Magnitude > 0.01) //If the gas velocity vector is within 45 degrees of the direction of the neighbor
                    {
                        componentangle = Dir.Angle(GasVel);
                        if (Math.Abs(componentangle) < quarterpi)
                        {
                            proportion = Math.Abs((1 / quarterpi) * (quarterpi - componentangle)); // Get the proper proportion of the vel vector
                            double velflow = proportion * GasVel.Magnitude; // Calculate flow due to gas velocity
                            if (velflow > gasAmount / 2.5)
                                velflow = gasAmount / 2.5;
                            Flow += velflow;
                        }
                    }
                    //Process velocity flow from neighbor
                    Dir = -1 * Dir; // Reverse Dir and apply same process to neighbor
                    if (neighbor.GasVel.Magnitude > 0.01) //If the gas velocity vector is within 45 degrees of the direction of the neighbor
                    {
                        componentangle = Dir.Angle(neighbor.GasVel);
                        if (Math.Abs(componentangle) < quarterpi)
                        {
                            proportion = Math.Abs((1 / quarterpi) * (quarterpi - componentangle)); // Get the proper proportion of the vel vector
                            double velflow = proportion * neighbor.GasVel.Magnitude; // Calculate flow due to gas velocity
                            if (velflow > neighbor.gasAmount / 2.5)
                                velflow = neighbor.gasAmount / 2.5;
                            Flow -= velflow;
                        }
                    }

                    nextGasAmount -= Flow;
                    neighbor.nextGasAmount += Flow;

                    double chaos = (double)rand.Next(70000, 100000)/100000; // Get a random number between .7 and 1 with 4 sig figs

                    //Process next velocities
                    if (Flow > 0) //Flow is to neighbor
                    {
                        Vector2 addvel = new Vector2(i, j);
                        addvel.Magnitude = Math.Abs(Flow); // Damping coefficient of .5
                        neighbor.NextGasVel = neighbor.NextGasVel + addvel * chaos * RecieverDamping;
                        NextGasVel = NextGasVel + addvel * chaos * SourceDamping;
                    }
                    if (Flow < 0) // Flow is from neighbor
                    {
                        Vector2 addvel = new Vector2(-1 * i, -1 * j);
                        addvel.Magnitude = Math.Abs(Flow);
                        neighbor.NextGasVel = neighbor.NextGasVel + addvel * chaos * SourceDamping;
                        NextGasVel = NextGasVel + addvel * chaos * RecieverDamping;

                    }



                    // Rescue clause. If this is needed to avoid crashes, something is wrong
                    /*if(nextGasAmount < 0)
                        nextGasAmount = 0;
                    if (neighbor.nextGasAmount < 0)
                        neighbor.nextGasAmount = 0;
                      */
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
