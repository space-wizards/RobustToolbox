using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BKSystem.IO;
using SS13_Shared;
using ServerInterfaces.Atmos;
using ServerServices.Tiles;
using ServerInterfaces.Map;

namespace ServerServices.Atmos
{

    public class GasCell : IGasCell
    {
        public Tile attachedTile;
        public int arrX;
        public int arrY;
        bool calculated = true;
        public Vector2 gasVel;
        public Vector2 NextGasVel;
        public Dictionary<GasType, float> lastSentGasses;
        private float lastVelSent = 0;
        Random rand;

        private GasMixture gasMixture;
        
        //Constants
        private const float SourceDamping = .5f;
        private const float RecieverDamping = .75f;
        public const float quarterpi = (float)(Math.PI / 4);
        private float FlowConstant = .1f;

        public GasCell(int _x, int _y, Tile _attachedTile)
        {
            arrX = _x;
            arrY = _y;
            SetGasDisplay();
            gasVel = new Vector2(0, 0);
            NextGasVel = new Vector2(0, 0);
            lastSentGasses = new Dictionary<GasType, float>();
            gasMixture = new GasMixture();
            rand = new Random();
            attachedTile = _attachedTile;
        }


        public void InitSTP()
        {
            AddGas(20, GasType.Oxygen);
            AddGas(80, GasType.Nitrogen);
        }

        public void Update()
        {
            if (attachedTile.GasSink)
            {
                gasMixture.SetNextGasAmount(0);
                foreach (var g in gasMixture.gasses)
                {
                    gasMixture.nextGasses[g.Key] = 0;
                }
            }

            gasVel = NextGasVel;
            NextGasVel = new Vector2(0, 0);

            // Copy next gas values into gas values
            gasMixture.Update();

            CalculateTotals();
            SetGasDisplay();

            calculated = false;
        }

        public void AttachToTile(Tile t)
        {
            attachedTile = t;
        }

        public void SetGasDisplay()
        {
            if (gasMixture == null)
                return;

            float gas = (float)gasMixture.GasAmount;
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
            gasMixture.AddGas(amount, gas);
        }

        public void CalculateTotals()
        {
            gasMixture.CalculateTotals();
        }

        private void Diffuse(GasMixture a)
        {
            gasMixture.Diffuse(a);
        }

        public void CalculateNextGasAmount(IMapManager m)
        {
            if (!attachedTile.GasPermeable)
                return;
            float DAmount;
            float Flow = 0;
            

            GasCell neighbor;
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) // If we're on this cell
                        continue;
                    if (arrX + i < 0 || arrX + i >= m.GetMapWidth() || arrY + j < 0 || arrY + j >= m.GetMapHeight()) // If out of bounds
                        continue;

                    neighbor = (GasCell)m.GetTileAt(arrX+i,arrY+j).GasCell;
                    if (neighbor.Calculated) // if neighbor's already been calculated, skip it
                        continue;

                    if (Math.Abs(i) + Math.Abs(j) == 2) //If its a corner
                    {
                        if (!m.GetTileAt(arrX + i, arrY).GasPermeable && !m.GetTileAt(arrX, arrY + i).GasPermeable) // And it is a corner separated from us by 2 blocking walls
                            continue; //Don't process it. These cells are not connected.
                    }

                    DAmount = gasMixture.GasAmount - neighbor.gasMixture.GasAmount;
                    if (DAmount == 0 || Math.Abs(DAmount) < 0.1)
                    {
                        Diffuse(neighbor.gasMixture);
                        return;
                    }

                    ///Calculate initial flow
                    Flow = FlowConstant * DAmount;
                    Flow = Clamp(Flow, gasMixture.GasAmount / 8, neighbor.gasMixture.GasAmount / 8);

                    //Velocity application code
                    Vector2 Dir = new Vector2(i, j);
                    float proportion = 0;
                    float componentangle;
                    //Process velocity flow to neighbor
                    if (gasVel.Magnitude > 0.01) //If the gas velocity vector is within 45 degrees of the direction of the neighbor
                    {
                        componentangle = Dir.Angle(gasVel);
                        if (Math.Abs(componentangle) < quarterpi)
                        {
                            proportion = Math.Abs((1 / quarterpi) * (quarterpi - componentangle)); // Get the proper proportion of the vel vector
                            float velflow = proportion * gasVel.Magnitude; // Calculate flow due to gas velocity
                            if (velflow > gasMixture.GasAmount / 2.5f)
                                velflow = gasMixture.GasAmount / 2.5f;
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
                            if (velflow > neighbor.gasMixture.GasAmount / 2.5f)
                                velflow = neighbor.gasMixture.GasAmount / 2.5f;
                            Flow -= velflow;
                        }
                    }

                    //Wall destruction
                    if (!neighbor.attachedTile.GasPermeable)
                    {
                        if (Flow > 500 && neighbor.attachedTile.GetType() == typeof(Wall))
                        {
                            neighbor.attachedTile.GasPermeable = false; // Incident flow is > 750 so the wall is destroyed
                            neighbor.attachedTile.TileState = TileState.Dead;
                            Flow = Flow * .75f; //Dying wall takes out some of the flow.
                        }
                        else
                            continue;
                    }

                    //Transfer gasses
                    gasMixture.SetNextGasAmount(gasMixture.GasAmount - Flow);
                    neighbor.gasMixture.SetNextGasAmount(gasMixture.GasAmount + Flow);

                    if (Flow > 0) // Flow is to neighbor
                    {
                        foreach (var g in gasMixture.gasses)
                        {
                            gasMixture.nextGasses[g.Key] -= Flow * (g.Value / gasMixture.GasAmount);
                            neighbor.gasMixture.nextGasses[g.Key] += Flow * (g.Value / gasMixture.GasAmount);
                        }
                    }
                    if (Flow < 0) // Flow is from neighbor
                    {
                        foreach (var g in neighbor.gasMixture.gasses)
                        {
                            gasMixture.nextGasses[g.Key] -= Flow * (g.Value / neighbor.gasMixture.GasAmount);
                            neighbor.gasMixture.nextGasses[g.Key] += Flow * (g.Value / neighbor.gasMixture.GasAmount);
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
                        if (gasMixture.gasses[GasType.Toxin] > 10 && (checkUpdateThreshold(GasType.Toxin) || all))
                        {
                            amount = (byte)normalizeGasAmount(gasMixture.gasses[GasType.Toxin]);
                            gasChanges[i] = amount;
                            changedTypes = (changedTypes | (1 << i));
                            lastSentGasses[GasType.Toxin] = gasMixture.gasses[GasType.Toxin];
                            bitCount += 4;
                        }
                        else
                        {
                            lastSentGasses[GasType.Toxin] = 0;
                        }
                        break;
                    case GasType.WVapor:
                        if (gasMixture.gasses[GasType.WVapor] > 10 && (checkUpdateThreshold(GasType.WVapor) || all))
                        {
                            amount = (byte)normalizeGasAmount(gasMixture.gasses[GasType.WVapor]);
                            gasChanges[i] = amount;
                            changedTypes = (changedTypes | (1 << i));
                            lastSentGasses[GasType.WVapor] = gasMixture.gasses[GasType.WVapor];
                            bitCount += 4;
                        }
                        else
                        {
                            lastSentGasses[GasType.WVapor] = 0;
                        }
                        break;
                    case GasType.HighVel:
                        if ((normalizeGasAmount(gasVel.Magnitude, 20) != normalizeGasAmount(lastVelSent, 20)))
                        {
                            amount = (byte)normalizeGasAmount(gasVel.Magnitude, 20);
                            gasChanges[i] = amount;
                            changedTypes = (changedTypes | (1 << i));
                            lastVelSent = gasVel.Magnitude;
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
            if(Math.Abs(normalizeGasAmount(gasMixture.gasses[g], multiplier) - normalizeGasAmount(lastSentGasses[g], multiplier)) >= 1)
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

        public float GasAmount(GasType type)
        {
            return gasMixture.gasses[type];
        }

        public float TotalGas
        {
            get
            {
                return gasMixture.TotalGas;
            }
        }

        public GasMixture GasMixture
        {
            get
            {
                return gasMixture;
            }
        }

        public Vector2 GasVel
        {
            get
            {
                return gasVel;
            }
        }

        public bool Calculated
        {
            get
            {
                return calculated;
            }
        }
    }
}
