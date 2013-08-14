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
        public double heatEnergy = 100;
        public double nextHeatEnergy = 100;
        public double nextGasAmount = 100;
        public double gasAmount = 100;
        public bool sink = false;
        public bool blocking = false;
        bool calculated = true;
        public Vector2 GasVel;
        public Vector2 NextGasVel;
        private readonly Dictionary<GasType, IGasProperties> gasProperties;
        private readonly AtmosManager _atmosManager;
        public readonly GasMixture GasMixture;

        //Constants
        float SourceDamping = .05f;
        float RecieverDamping = .075f;
        public const float quarterpi = (float)Math.PI / 4;
        int circleDiameter = 15;
        private float FlowConstant = .1f;

        public GasCell(Ellipse e, double _x, double _y, GasCell[,] _cellArray, AtmosManager atmosManager)
        {
            _atmosManager = atmosManager;
            attachedellipse = e;
            x = _x;
            y = _y;
            GasMixture = new GasMixture(atmosManager);
            SetGasDisplay();
            cellArray = _cellArray;
            GasVel = new Vector2(0, 0);
            NextGasVel = new Vector2(0, 0);
        }

        public void InitSTP()
        {
            AddGas(0.0172f, GasType.Oxygen);
            AddGas(0.0627f, GasType.Nitrogen);
            AddGas(0.0010f, GasType.CO2);
        }

        public void AddGas(float amount, GasType gas)
        {
            if (amount == 1.0f)
            {
                GasMixture.SetNextTemperature(5000f);
            }
            else
            {
                GasMixture.AddNextGas(amount, gas);
            }
        }

        public void Update(bool draw)
        {
            if (sink)
            {
                GasMixture.SetNextTemperature(0);
                foreach(var g in GasMixture.gasses)
                {
                    GasMixture.nextGasses[g.Key] = 0;
                }
            }
            GasVel = NextGasVel;
            NextGasVel = new Vector2(0, 0);
            
            GasMixture.Burn();
            GasMixture.Update();

            if(draw)
                SetGasDisplay();
            

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
            float gas = GasMixture.Pressure;
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
            SetRadius(circleDiameter * gas / 200);
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
        /*
        public void CalculateNextGasAmount()
        {
            if (blocking)
                return;
            double DAmount;
            double Flow = 0;
            

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
                    if (neighbor.calculated) // TODO work out rebound
                        continue;

                    double dheat = heatEnergy - neighbor.heatEnergy;
                    DAmount = gasAmount - neighbor.gasAmount;

                    double chaos = (double)rand.Next(70000, 100000) / 100000; // Get a random number between .7 and 1 with 4 sig figs

                    //If there's nothing to do then return
                    if (Math.Abs(dheat) < .1 && (DAmount == 0 || Math.Abs(DAmount) < 0.1))
                    {
                        return;
                    }


                    ///Calculate initial flow
                    Flow = FlowConstant * DAmount;
                    if(dheat > 1) // Kludgy shite
                        Flow = Flow + ((gasAmount + neighbor.gasAmount) / (dheat * 80)) * chaos;
                    Flow = Clamp(Flow, gasAmount / 8, neighbor.gasAmount / 8);

                    double HeatFlow = FlowConstant * dheat;
                    HeatFlow = Clamp(HeatFlow, heatEnergy / 8, neighbor.heatEnergy / 8);

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

                    //Wall destruction
                    if (neighbor.blocking)
                    {
                        if (Flow > 500)
                        {
                            neighbor.blocking = false; // Incident flow is > 750 so the wall is destroyed
                            Flow = Flow * .75; //Dying wall takes out some of the flow.
                        }
                        else
                            continue;
                    }

                    nextGasAmount -= Flow;
                    neighbor.nextGasAmount += Flow;

                    nextHeatEnergy -= HeatFlow;
                    neighbor.nextHeatEnergy += HeatFlow;

                    
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
                      *
                    calculated = true;
                }
        }*/
        public void CalculateNextGasAmount()
        {
            if (calculated)
                return;

            if (blocking)
                return;

            float DAmount;

            GasCell neighbor;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) // If we're on this cell
                        continue;
                    if (arrX + i < 0 || arrX + i >= 38 || arrY + j < 0 || arrY + j >= 38)
                        // If out of bounds
                        continue;

                    if (Math.Abs(i) + Math.Abs(j) == 2) //If its a corner
                    {
                        if (cellArray[arrX + i, arrY].blocking && cellArray[arrX, arrY + i].blocking)
                            // And it is a corner separated from us by 2 blocking walls
                            continue; //Don't process it. These cells are not connected.
                    }

                    neighbor = (GasCell) cellArray[arrX + i, arrY + j];
                    if (neighbor.calculated) // if neighbor's already been calculated, skip it
                        continue;


                    if (!neighbor.blocking)
                    {
                        if (GasMixture.Burning || neighbor.GasMixture.Burning)
                        {
                            neighbor.GasMixture.Expose();
                            GasMixture.Expose();
                        }
                        DAmount = GasMixture.Pressure - neighbor.GasMixture.Pressure;
                        if (Math.Abs(DAmount) < 50.0f)
                        {
                            GasMixture.Diffuse(neighbor.GasMixture);
                        }
                        else
                        {

                            var neighDir = new Vector2(i, j);
                            float angle = 0.0f;
                            float pAmount = 0.0f;
                            float proportion = 0.075f;



                            if (GasVel.Magnitude > 0.0f)
                            {
                                angle = (float)Math.Abs(neighDir.Angle(GasVel));
                                if (Math.Abs(angle) < quarterpi)
                                {
                                    pAmount += VelClamp((float)GasVel.Magnitude);//Clamp(GasMixture.Pressure / neighbor.GasMixture.Pressure);
                                    proportion += Math.Abs(((1f / quarterpi) * (quarterpi - angle)) * pAmount);// (float)GasVel.Magnitude / 100f);
                                }
                            }

                            if (neighbor.GasVel.Magnitude > 0.0f)
                            {
                                var Dir = neighDir * -1;
                                angle = (float)Math.Abs(Dir.Angle(neighbor.GasVel));
                                if (angle < quarterpi)
                                {
                                    pAmount += VelClamp((float)neighbor.GasVel.Magnitude); //Clamp(neighbor.GasMixture.Pressure / GasMixture.Pressure);
                                    proportion += Math.Abs(((1f / quarterpi) * (quarterpi - angle)) * pAmount);//(float)neighbor.GasVel.Magnitude / 1000f));
                                }
                            }
                            if (proportion > 0.3f)
                                proportion = 0.3f;
                            
                            
                            if (proportion > 0.0f)
                            {
                                GasMixture.Diffuse(neighbor.GasMixture, 1f / proportion);
                                neighbor.NextGasVel += new Vector2(DAmount * proportion * i, DAmount * proportion * j) * RecieverDamping;
                                NextGasVel += new Vector2(DAmount * proportion * i, DAmount * proportion * j) * SourceDamping;
                            }
                        }
                        
                        
                    }

                    #region old gas code

                    /*
                    if (DAmount == 0 || Math.Abs(DAmount) < 10)
                    {
                        gasMixture.Diffuse(neighbor.gasMixture);
                        return;
                    }

                    Log.LogManager.Log("High pressure difference");
                    ///Calculate initial flow
                    Flow = FlowConstant * DAmount;
                    Flow = Clamp(Flow, gasMixture.Pressure / 8, neighbor.gasMixture.Pressure / 8);
                    //Velocity application code
                    Vector2 Dir = new Vector2(i, j);
                    float proportion = 0;
                    float componentangle;

                    //Process velocity flow to neighbor
                    if (gasVel.Magnitude > 0.01) 
                    {
                        componentangle = Dir.Angle(gasVel);
                        if (Math.Abs(componentangle) < quarterpi) //If the gas velocity vector is within 45 degrees of the direction of the neighbor
                        {
                            proportion = Math.Abs((1 / quarterpi) * (quarterpi - componentangle)); // Get the proper proportion of the vel vector
                            float velflow = proportion * gasVel.Magnitude; // Calculate flow due to gas velocity
                            if (velflow > gasMixture.TotalGas / 2.5f)
                                velflow = gasMixture.TotalGas / 2.5f;
                            Flow += velflow;
                        }
                    }

                    //Process velocity flow from neighbor
                    Dir = -1 * Dir; // Reverse Dir and apply same process to neighbor
                    if (neighbor.gasVel.Magnitude > 0.01) //If the gas velocity vector is within 45 degrees of the direction of the neighbor
                    {
                        componentangle = Dir.Angle(neighbor.gasVel);
                        if (Math.Abs(componentangle) < quarterpi)
                        {
                            proportion = Math.Abs((1 / quarterpi) * (quarterpi - componentangle)); // Get the proper proportion of the vel vector
                            float velflow = proportion * neighbor.gasVel.Magnitude; // Calculate flow due to gas velocity
                            if (velflow > neighbor.gasMixture.TotalGas / 2.5f)
                                velflow = neighbor.gasMixture.TotalGas / 2.5f;
                            Flow -= velflow;
                        }
                    }


                    //  what the fuck is this doing here
                    /*Wall destruction 
                    if (!neighbor.attachedTile.GasPermeable)
                    {
                        if (Flow > 5 && neighbor.attachedTile.GetType() == typeof(Wall))
                        {
                            neighbor.attachedTile.GasPermeable = false; // Incident flow is > 750 so the wall is destroyed
                            neighbor.attachedTile.TileState = TileState.Dead;
                            Flow = Flow * .75f; //Dying wall takes out some of the flow.
                        }
                        else
                            continue;
                    }*/
                    /*
                    if (Flow > 0) // Flow is to neighbor
                    {
                        foreach (var g in gasMixture.gasses)
                        {
                            gasMixture.RemoveNextGas(Flow * (g.Value / gasMixture.TotalGas), g.Key);
                            neighbor.gasMixture.AddNextGas(Flow * (g.Value / gasMixture.TotalGas), g.Key);
                        }
                    }
                    if (Flow < 0) // Flow is from neighbor
                    {
                        foreach (var g in neighbor.gasMixture.gasses)
                        {
                            gasMixture.AddNextGas(Flow * (g.Value / neighbor.gasMixture.TotalGas), g.Key);
                            neighbor.gasMixture.RemoveNextGas(Flow * (g.Value / neighbor.gasMixture.TotalGas), g.Key);
                        }
                    }

                    float chaos = (float)rand.Next(70000, 100000) / 100000; // Get a random number between .7 and 1 with 4 sig figs

                    //Process next velocities
                    if (Flow > 0) //Flow is to neighbor
                    {
                        Vector2 addvel = new Vector2(i, j);
                        addvel.Magnitude = (float)Math.Abs(Flow); // Damping coefficient of .5
                        neighbor.NextGasVel = neighbor.NextGasVel + addvel * chaos * RecieverDamping;
                        NextGasVel = NextGasVel + addvel * chaos * SourceDamping;
                    }
                    if (Flow < 0) // Flow is from neighbor
                    {
                        Vector2 addvel = new Vector2(-1 * i, -1 * j);
                        addvel.Magnitude = (float)Math.Abs(Flow);
                        neighbor.NextGasVel = neighbor.NextGasVel + addvel * chaos * SourceDamping;
                        NextGasVel = NextGasVel + addvel * chaos * RecieverDamping;
                    }*/

                    #endregion
                }
            }

            calculated = true;
        }


        public float Clamp(float Flow)
        {
            if (Flow > 0.3f)
                return 0.3f;
            if (Flow <= 0.0f)
                return 0.01f;
            return Flow;
        }

        public float VelClamp(float vel)
        {
            float returnVel = vel / 100f;
            if (returnVel > 0.4f)
                returnVel = 0.4f;
            else if (returnVel < 0.05f)
                returnVel = 0.05f;
            return returnVel;
        }
    }
}
