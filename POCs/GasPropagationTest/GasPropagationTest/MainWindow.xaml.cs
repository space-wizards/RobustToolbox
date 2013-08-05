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
        DateTime lastUpdate;
        DateTime lastDraw;
        DateTime lastDataUpdate;
        System.Drawing.Size mapSize = new System.Drawing.Size(39,39);
        int updatePeriod = 30; //update period in ms
        int drawPeriod = 66;
        int dataPeriod = 1000;
        public AtmosManager AtmosManager;

        public MainWindow()
        {
            InitializeComponent();
            AtmosManager = new AtmosManager(mapSize, canvas);
            Initialize();
            CompositionTarget.Rendering += Update;
            KeyDown += KeyDownEventHandler;
            KeyUp += KeyUpEventHandler;
            MouseDown += MouseDownEventHandler;
        }

        public void Initialize()
        {
            lastUpdate = DateTime.Now;
            lastDraw = DateTime.Now;
            lastDataUpdate = DateTime.Now;
            //cellArray[20, 20].nextGasAmount = 10;
            AtmosManager.InitializeGasCells();
        }

        public void Update(object sender, EventArgs e)
        {
            Update(false);
        }

        public void Update(bool clicked)
        {
            Random r = new Random();
            TimeSpan x = DateTime.Now - lastDataUpdate;
            if (x.TotalMilliseconds > dataPeriod)
                DataUpdate();
                

            TimeSpan t = DateTime.Now - lastUpdate;
            if (t.TotalMilliseconds < updatePeriod)
                return;
            else
                lastUpdate = DateTime.Now;

            AtmosManager.Calculate();

            bool draw = false;
            TimeSpan d = DateTime.Now - lastDraw;
            if (d.TotalMilliseconds >= drawPeriod)
                draw = true;
            draw = draw || clicked;

            AtmosManager.Update();

        }

        public void DataUpdate()
        {
            double Sum = 0;
            double HeatSum = 0;
            for (int i = 0; i < 39; i++)
            {
                for (int j = 0; j < 39; j++)
                {
                    Sum += AtmosManager.cellArray[i, j].gasAmount;
                    HeatSum += AtmosManager.cellArray[i, j].heatEnergy;
                }
            }

            totalheat.Text = HeatSum.ToString();
            totalgas.Text = Sum.ToString();
            lastDataUpdate = DateTime.Now;
        }

        public bool BoundsCheck(int x, int y)
        {
            if (x < 0 || y < 0 || x > 38 || y > 38)
                return false;
            return true;
        }

        public void SetAllRadii(int n)
        {
            for (int i = 0; i < 39; i++)
            {
                for (int j = 0; j < 39; j++)
                {
                    AtmosManager.cellArray[i, j].gasAmount = n * 0.1;
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
                return AtmosManager.cellArray[x, y];
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
                g.AddGas(2, GasType.Oxygen);
            else if (e.RightButton == MouseButtonState.Pressed && Keyboard.IsKeyDown(Key.LeftAlt))
                g.AddGas(20, GasType.Oxygen);
            else if (e.LeftButton == MouseButtonState.Pressed && Keyboard.IsKeyDown(Key.RightShift))
                g.GasVel.X += 1000;
            else if (e.RightButton == MouseButtonState.Pressed && Keyboard.IsKeyDown(Key.RightShift))
                g.GasVel.X += 100000;
            else if (e.LeftButton == MouseButtonState.Pressed && Keyboard.IsKeyDown(Key.LeftCtrl))
                g.nextHeatEnergy += 100;
            else if (e.LeftButton == MouseButtonState.Pressed)
                g.nextGasAmount += addamount;
            else if (e.RightButton == MouseButtonState.Pressed)
                g.nextGasAmount += addamount * 10;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Instructions i = new Instructions();
            i.Show();
        }
    }
}
