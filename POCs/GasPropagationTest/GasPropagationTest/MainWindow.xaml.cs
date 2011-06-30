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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        GasCell[,] cellArray;
        DateTime lastUpdate;
        double propagationthreshold = 1;
        double propagationrateconstant = 0.9;
        int updatePeriod = 40; //update period in ms

        public MainWindow()
        {
            cellArray = new GasCell[40, 40];
            InitializeComponent();
            Initialize();
            CompositionTarget.Rendering += Update;
            KeyDown += KeyDownEventHandler;
            KeyUp += KeyUpEventHandler;
            MouseDown += MouseDownEventHandler;
            Instructions i = new Instructions();
            i.Show();
        }

        public void Initialize()
        {
            lastUpdate = DateTime.Now;
            Ellipse e;
            for (int i = 0; i < 39; i++)
            {
                for (int j = 0; j < 39; j++)
                {
                    e = new Ellipse();
                    e.Width = 15;
                    e.Height = 15;
                    e.Fill = Brushes.Blue;                    
                    canvas.Children.Add(e);
                    Canvas.SetLeft(e, (double)i * 15);
                    Canvas.SetTop(e, (double)j * 15);
                    cellArray[i, j] = new GasCell(e, i*15, j*15);
                }
            }

            //cellArray[20, 20].nextGasAmount = 10;
        }

        public void Update(object sender, EventArgs e)
        {
            TimeSpan t = DateTime.Now - lastUpdate;
            if (t.TotalMilliseconds < updatePeriod)
                return;
            else
                lastUpdate = DateTime.Now;
            
            GasCell toCell;
            for (int i = 0; i < 39; i++)
            {
                for (int j = 0; j < 39; j++)
                {
                    SpreadGas(i, j);

                }
            }

            for (int i = 0; i < 39; i++)
            {
                for (int j = 0; j < 39; j++)
                {
                    cellArray[i, j].Update();
                }
            }

        }

        public void SpreadGas(int x, int y)
        {
            Dictionary<GasCell, double> l;
            GasCell currentCell;

            currentCell = cellArray[x, y];
            if (currentCell.gasAmount < 0.1)
                return;//Threshold
            l = new Dictionary<GasCell, double>();
            if (x > 0 && y > 0 && currentCell.gasAmount > cellArray[x - 1, y - 1].gasAmount && !cellArray[x - 1, y - 1].blocking)
                l.Add(cellArray[x - 1, y - 1], (currentCell.gasAmount - cellArray[x - 1, y - 1].gasAmount) * .6);

            if (x < 38 && y < 38 && currentCell.gasAmount > cellArray[x + 1, y + 1].gasAmount && !cellArray[x + 1, y + 1].blocking)
                l.Add(cellArray[x + 1, y + 1], (currentCell.gasAmount - cellArray[x + 1, y + 1].gasAmount) * .6);

            if (x < 38 && y > 0 && currentCell.gasAmount > cellArray[x + 1, y - 1].gasAmount && !cellArray[x + 1, y - 1].blocking)
                l.Add(cellArray[x + 1, y - 1], (currentCell.gasAmount - cellArray[x + 1, y - 1].gasAmount) * .6);

            if (x > 0 && y < 38 && currentCell.gasAmount > cellArray[x - 1, y + 1].gasAmount && !cellArray[x - 1, y + 1].blocking)
                l.Add(cellArray[x - 1, y + 1], (currentCell.gasAmount - cellArray[x - 1, y + 1].gasAmount)* .6);

            if (y > 0 && currentCell.gasAmount > cellArray[x, y - 1].gasAmount && !cellArray[x, y - 1].blocking)
                l.Add(cellArray[x, y - 1], currentCell.gasAmount - cellArray[x, y - 1].gasAmount);

            if (y < 38 && currentCell.gasAmount > cellArray[x, y + 1].gasAmount && !cellArray[x, y + 1].blocking)
                l.Add(cellArray[x, y + 1], currentCell.gasAmount - cellArray[x, y + 1].gasAmount);

            if (x > 0 && currentCell.gasAmount > cellArray[x - 1, y].gasAmount && !cellArray[x - 1, y].blocking)
                l.Add(cellArray[x - 1, y], currentCell.gasAmount - cellArray[x - 1, y].gasAmount);

            if (x < 38 && currentCell.gasAmount > cellArray[x + 1, y].gasAmount && !cellArray[x + 1, y].blocking)
                l.Add(cellArray[x + 1, y], currentCell.gasAmount - cellArray[x + 1, y].gasAmount);

            if (l.Count == 0) //if there are no gas differences, continue.
                return;

            /// Find the sum of all differences, average them and subtract that amount of gas from the source.
            double differenceSum = 0;
            foreach (var c in l)
            {
                differenceSum += c.Value;
            }
            if (differenceSum < propagationthreshold) // If the difference is neglibible, continue.
                return;
            double adjustedSum = differenceSum / l.Count * propagationrateconstant;
            //if (adjustedSum > currentCell.gasAmount / 2)
            //    adjustedSum = currentCell.gasAmount / 2; // Limit the amount of gas that can flow out of a cell per turn

            foreach (var c in l)
            {
                //Transfer gas
                c.Key.nextGasAmount += (c.Value / differenceSum) * adjustedSum;
                /*
                                        // If we transferred too much...
                                        if (c.Key.nextGasAmount > currentCell.nextGasAmount - adjustedSum)
                                        {
                                            //transfer some back.
                                            double halfofdiff = (c.Key.nextGasAmount - (currentCell.nextGasAmount - adjustedSum))/2;
                                            c.Key.nextGasAmount -= halfofdiff;
                                            currentCell.nextGasAmount += halfofdiff;

                                        }
                 */
                // Smoothing
            }

            currentCell.nextGasAmount -= adjustedSum;
            if (currentCell.nextGasAmount < 0)
                currentCell.nextGasAmount = 0;
        }
        
        public void SetAllRadii(int n)
        {
            for (int i = 0; i < 39; i++)
            {
                for (int j = 0; j < 39; j++)
                {
                    cellArray[i, j].gasAmount = n * 0.1;
                }
            }
        }

        private GasCell GetCellAtPoint(Point p)
        {
            int x = (int)Math.Round(p.X / 15);
            int y = (int)Math.Round(p.Y / 15);
            if (x < 0 || y < 0 || x > 38 || y > 38)
                return null;
            else
                return cellArray[x, y];
        }

        private void KeyUpEventHandler(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.D0:
                    SetAllRadii(0);
                    break;
                case Key.D1:
                    SetAllRadii(1);
                    break;
                case Key.D2:
                    SetAllRadii(2);
                    break;
                case Key.D3:
                    SetAllRadii(3);
                    break;
                case Key.D4:
                    SetAllRadii(4);
                    break;
                case Key.D5:
                    SetAllRadii(5);
                    break;
                case Key.D6:
                    SetAllRadii(6);
                    break;
                case Key.D7:
                    SetAllRadii(7);
                    break;
                case Key.D8:
                    SetAllRadii(8);
                    break;
                case Key.D9:
                    SetAllRadii(9);
                    break;

            }
        }

        private void KeyDownEventHandler(object sender, KeyEventArgs e)
        {

        }

        private void MouseDownEventHandler(object sender, MouseEventArgs e)
        {
            double addamount = 100;
            Point p = e.GetPosition(this);
            p.X -= 7.5;
            p.Y -= 7.5;
            GasCell g = GetCellAtPoint(p);
            if (g == null)
                return;
            if (e.LeftButton == MouseButtonState.Pressed && Keyboard.IsKeyDown(Key.LeftShift))
                g.sink = !g.sink;
            else if (e.RightButton == MouseButtonState.Pressed && Keyboard.IsKeyDown(Key.LeftShift))
                g.blocking = !g.blocking;
            else if (e.LeftButton == MouseButtonState.Pressed && Keyboard.IsKeyDown(Key.LeftAlt))
                g.nextGasAmount += addamount * 100;
            else if (e.LeftButton == MouseButtonState.Pressed)
                g.nextGasAmount += addamount;
            else if (e.RightButton == MouseButtonState.Pressed)
                g.nextGasAmount += addamount * 10;

            
        }
    }
}
