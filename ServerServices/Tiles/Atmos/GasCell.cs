using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BKSystem.IO;
using SS13_Shared;

namespace ServerServices.Tiles.Atmos
{

    public class GasCell
    {
        Tile attachedtile;
        Tile[,] tileArray;
        public int arrX;
        public int arrY;
        public int mapWidth;
        public int mapHeight;
        public float nextGasAmount;
        public float gasAmount;
        public bool sink = false;
        public bool blocking = false;
        bool calculated = true;
        public Vector2 GasVel;
        public Vector2 NextGasVel;
        public Dictionary<GasType, float> gasses;
        public Dictionary<GasType, float> nextGasses;
        public Dictionary<GasType, float> lastSentGasses;
        private float lastVelSent = 0;
        Random rand;
        
        //Constants
        private const float SourceDamping = .5f;
        private const float RecieverDamping = .75f;
        public const float quarterpi = (float)(Math.PI / 4);
        private float FlowConstant = .1f;

        public GasCell(Tile t, int _x, int _y, Tile[,] _tileArray, int _mapWidth, int _mapHeight)
        {
            attachedtile = t;
            arrX = _x;
            arrY = _y;
            mapWidth = _mapWidth;
            mapHeight = _mapHeight;
            SetGasDisplay();
            tileArray = _tileArray;
            GasVel = new Vector2(0, 0);
            NextGasVel = new Vector2(0, 0);
            sink = attachedtile.gasSink;
            blocking = !attachedtile.gasPermeable;
            InitGas();
            rand = new Random();
        }

        private void InitGas()
        {
            gasses = new Dictionary<GasType, float>();
            nextGasses = new Dictionary<GasType, float>();
            lastSentGasses = new Dictionary<GasType, float>();
            
            var gastypes = Enum.GetValues(typeof(GasType));
            foreach (GasType g in gastypes)
            {
                gasses.Add(g, 0);
                nextGasses.Add(g, 0);
                lastSentGasses.Add(g, 0);
            }
            //AddGas(20, GasType.Oxygen);
        }

        public void Update()
        {
            if (sink)
            {
                nextGasAmount = 0;
                foreach (var g in gasses)
                {
                    nextGasses[g.Key] = 0;
                }
            }

            GasVel = NextGasVel;
            NextGasVel = new Vector2(0, 0);

            // Copy next gas values into gas values
            foreach (var ng in nextGasses)
            {
                gasses[ng.Key] = ng.Value;
            }

            CalculateTotals();
            SetGasDisplay();

            calculated = false;
        }

        public void AttachToTile(Tile t)
        {
            attachedtile = t;
            blocking = !attachedtile.gasPermeable;
            sink = attachedtile.gasSink;
        }

        public void SetGasDisplay()
        {
            float gas = (float)gasAmount;
            if (gas < 1)
                gas = 1;
            else if (gas > 10000)
                gas = 10000;

            float red = gas/1000;
            //TODO set display here
        }

        public void SetRadius(float r)
        {
            if(r > 7.5)
                r = 7.5f;
            if (r < 0)
                r = 0;
            //Defunct
        }

        public void AddGas(float amount, GasType gas)
        {
            gasses[gas] += amount;
            nextGasses[gas] += amount;
            CalculateTotals();
        }
        public void CalculateTotals()
        {
            gasAmount = 0;
            foreach (float g in gasses.Values)
                gasAmount += g;

            nextGasAmount = 0;
            foreach (float n in nextGasses.Values)
                nextGasAmount += n;
        }

        private void Diffuse(GasCell a, GasCell b)
        {
            foreach (var gas in a.gasses)
            {
                if (gas.Value > b.gasses[gas.Key])
                {
                    var amount = (gas.Value - b.gasses[gas.Key]) / 8;
                    b.nextGasses[gas.Key] += amount;
                    a.nextGasses[gas.Key] -= amount;
                }
                else if (b.gasses[gas.Key] > gas.Value)
                {
                    var amount = (b.gasses[gas.Key] - gas.Value) / 8;
                    a.nextGasses[gas.Key] += amount;
                    b.nextGasses[gas.Key] -= amount;
                }

            }
        }

        public void CalculateNextGasAmount()
        {
            if (blocking)
                return;
            float DAmount;
            float Flow = 0;
            

            GasCell neighbor;
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) // If we're on this cell
                        continue;
                    if (arrX + i < 0 || arrX + i >= mapWidth || arrY + j < 0 || arrY + j >= mapHeight) // If out of bounds
                        continue;

                    neighbor = tileArray[arrX+i,arrY+j].gasCell;
                    if (neighbor.calculated) // if neighbor's already been calculated, skip it
                        continue;

                    if (Math.Abs(i) + Math.Abs(j) == 2) //If its a corner
                    {
                        if (tileArray[arrX + i, arrY].gasCell.blocking && tileArray[arrX, arrY + j].gasCell.blocking) // And it is a corner separated from us by 2 blocking walls
                            continue; //Don't process it. These cells are not connected.
                    }

                    DAmount = gasAmount - neighbor.gasAmount;
                    if (DAmount == 0 || Math.Abs(DAmount) < 0.1)
                    {
                        Diffuse(neighbor, this);
                        return;
                    }

                    ///Calculate initial flow
                    Flow = FlowConstant * DAmount;
                    Flow = Clamp(Flow, gasAmount / 8, neighbor.gasAmount / 8);

                    //Velocity application code
                    Vector2 Dir = new Vector2(i, j);
                    float proportion = 0;
                    float componentangle;
                    //Process velocity flow to neighbor
                    if (GasVel.Magnitude > 0.01) //If the gas velocity vector is within 45 degrees of the direction of the neighbor
                    {
                        componentangle = Dir.Angle(GasVel);
                        if (Math.Abs(componentangle) < quarterpi)
                        {
                            proportion = Math.Abs((1 / quarterpi) * (quarterpi - componentangle)); // Get the proper proportion of the vel vector
                            float velflow = proportion * GasVel.Magnitude; // Calculate flow due to gas velocity
                            if (velflow > gasAmount / 2.5f)
                                velflow = gasAmount / 2.5f;
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
                            float velflow = proportion * neighbor.GasVel.Magnitude; // Calculate flow due to gas velocity
                            if (velflow > neighbor.gasAmount / 2.5f)
                                velflow = neighbor.gasAmount / 2.5f;
                            Flow -= velflow;
                        }
                    }

                    //Wall destruction
                    if (neighbor.blocking)
                    {
                        if (Flow > 500 && neighbor.attachedtile.GetType() == typeof(Wall))
                        {                           
                            neighbor.blocking = false; // Incident flow is > 750 so the wall is destroyed
                            neighbor.attachedtile.tileState = TileState.Dead;
                            Flow = Flow * .75f; //Dying wall takes out some of the flow.
                        }
                        else
                            continue;
                    }

                    //Transfer gasses
                    nextGasAmount -= Flow;
                    neighbor.nextGasAmount += Flow;
                    if (Flow > 0) // Flow is to neighbor
                    {
                        foreach (var g in gasses)
                        {
                            nextGasses[g.Key] -= Flow * (g.Value / gasAmount);
                            neighbor.nextGasses[g.Key] += Flow * (g.Value / gasAmount);
                        }
                    }
                    if(Flow < 0) // Flow is from neighbor
                    {
                        foreach (var g in neighbor.gasses)
                        {
                            nextGasses[g.Key] -= Flow * (g.Value / neighbor.gasAmount);
                            neighbor.nextGasses[g.Key] += Flow * (g.Value / neighbor.gasAmount);
                        }
                    }
                    CalculateTotals();
                    neighbor.CalculateTotals();

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

        public float Clamp(float Flow, float amount, float neighamount)
        {
            float clampedFlow = Flow;

            if (amount - clampedFlow < 0)
                clampedFlow = clampedFlow + (amount - clampedFlow);
            if (neighamount + clampedFlow < 0)
                clampedFlow = clampedFlow + (neighamount + clampedFlow);

            return clampedFlow;
        }

        /// <summary>
        /// Function to pack an array of one-byte representations of a given gas's visibility.
        /// </summary>
        /// <param name="all">send em all anyway?</param>
        /// <returns></returns>
        /// 
        public int PackDisplayBytes(BitStream bits, bool all = false)
        {
           
            int changedTypes = 0;

            //How many gas types we have!
            int typesCount = Enum.GetValues(typeof(GasType)).Length;
            var gasChanges = new byte[typesCount];
            var bitCount = typesCount;
            for (int i = typesCount - 1; i >= 0; i--)
            {
                byte amount;
                var t = (GasType) i;
                switch(t)
                {
                    case GasType.Toxin:
                        if (gasses[GasType.Toxin] > 10 && (checkUpdateThreshold(GasType.Toxin) || all))
                        {
                            amount = (byte)normalizeGasAmount(gasses[GasType.Toxin]);
                            gasChanges[i] = amount;
                            changedTypes = (changedTypes | (1 << i));
                            lastSentGasses[GasType.Toxin] = gasses[GasType.Toxin];
                            bitCount += 4;
                        }
                        else
                        {
                            lastSentGasses[GasType.Toxin] = 0;
                        }
                        break;
                    case GasType.WVapor:
                        if(gasses[GasType.WVapor] > 10 && (checkUpdateThreshold(GasType.WVapor) || all))
                        {
                            amount = (byte) normalizeGasAmount(gasses[GasType.WVapor]);
                            gasChanges[i] = amount;
                            changedTypes = (changedTypes | (1 << i));
                            lastSentGasses[GasType.WVapor] = gasses[GasType.WVapor];
                            bitCount += 4;
                        }
                        else
                        {
                            lastSentGasses[GasType.WVapor] = 0;
                        }
                        break;
                    case GasType.HighVel:
                        if ((normalizeGasAmount(GasVel.Magnitude, 20) != normalizeGasAmount(lastVelSent, 20)))
                        {
                            amount = (byte)normalizeGasAmount(GasVel.Magnitude, 20);
                            gasChanges[i] = amount;
                            changedTypes = (changedTypes | (1 << i));
                            lastVelSent = GasVel.Magnitude;
                            bitCount += 4;
                        }
                        else
                        {
                            lastSentGasses[GasType.HighVel] = 0;
                        }
                        break;
                    default:
                        break;
                }
            }

            //Make a new bitstream with the number o bits we need.
            bits.Write(changedTypes, 0, typesCount); //write 8 bits for what gas types have changed...
            for(int i = typesCount - 1;i>=0;i--)
            {
                int type = 1 << i;
                //Checks flags in the form of 00001011 -- each 1 is a gas type that needs sending... I know this is nuts but it works great!
                if((changedTypes & type) == type)
                {
                    bits.Write(gasChanges[i], 0, 4);
                }
            }

            return bitCount;
        }

        private bool checkUpdateThreshold(GasType g, float multiplier = 1)
        {
            //If the delta since the last update was sent is greater than 2, send another update.
            if(Math.Abs(normalizeGasAmount(gasses[g], multiplier) - normalizeGasAmount(lastSentGasses[g], multiplier)) >= 1)
                return true;
            return false;
        }

        /// <summary>
        /// Normalize a gas amount to between 0 and 15 for packing.
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public int normalizeGasAmount(float amount, float multiplier = 1)
        {
            amount = amount * multiplier;
            if (amount > 150)
                amount = 150;
            return (int)(amount / 10);
        }

        public float TotalGas
        {
            get
            {
                float total = 0;
                foreach (var gas in gasses)
                {
                    total += gas.Value;
                }

                return total;
            }
        }
    }
}
