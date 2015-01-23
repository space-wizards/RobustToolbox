using BKSystem.IO;
using SS14.Server.Interfaces.Atmos;
using SS14.Server.Interfaces.Map;
using SS14.Server.Services.Log;
using SS14.Server.Services.Tiles;
using SS14.Shared;
using System;
using System.Collections.Generic;

namespace SS14.Server.Services.Atmos
{
    public class GasCell : IGasCell
    {
        private const float SourceDamping = .25f;
        private const float RecieverDamping = .1f;
        public const float quarterpi = (float) (Math.PI/4);
        private const float VelocityMultiplier = .1f;
        private readonly GasMixture gasMixture;
        private Vector2 NextGasVel;
        public Tile attachedTile;
        private bool calculated = true;
        private Vector2 gasVel;
        public Dictionary<GasType, float> lastSentGasses;
        private float lastVelSent = 0;
        private Random rand;
        private Tile[,] neighbours;

        public GasCell(Tile _attachedTile)
        {
            gasVel = new Vector2(0, 0);
            NextGasVel = new Vector2(0, 0);
            lastSentGasses = new Dictionary<GasType, float>();
            gasMixture = new GasMixture();
            rand = new Random();
            attachedTile = _attachedTile;
        }

        public void SetNeighbours(IMapManager m)
        {
            neighbours = new Tile[3,3];
            for (int i = 0; i <= 2; i++)
            {
                for (int j = 0; j <= 2; j++)
                {
                    if (i == 1 && j == 1)
                    {
                        neighbours[i, j] = null;
                        continue;
                    }
                    neighbours[i, j] = (Tile)m.GetFloorAt(attachedTile.WorldPosition + new Vector2((i - 1) * m.GetTileSpacing(), (j - 1) * m.GetTileSpacing()));
                }
            }
        }

        public GasMixture GasMixture
        {
            get { return gasMixture; }
        }

        // Lets assume all gasses are ideal gasses so we can use PV = nRT
        // Air is 78% Nitrogen, 21% Oxygen, 1% CO2 (very roughly)
        // So using the ideal gas equation we can work out how many moles of each gas
        // are in "normal" air (~ 100kPa, 298.15K)

        // Roughly 0.08 moles of gas in 2m^3 (2 litres) of air (assuming each "tile" is 1x1x2 m)

        #region IGasCell Members

        public void InitSTP()
        {
            AddGas(0.0172f, GasType.Oxygen);
            AddGas(0.0627f, GasType.Nitrogen);
            AddGas(0.0010f, GasType.CO2);
        }

        public void Update()
        {
            if (attachedTile.GasSink)
            {
                gasMixture.SetNextTemperature(0);
                for (var i = 0; i < gasMixture.gasses.Length; i++)
                {
                    gasMixture.nextGasses[i] = 0;
                }
            }

            gasVel = NextGasVel;
            NextGasVel = new Vector2(0, 0);

            gasMixture.Burn();

            // Copy next gas values into gas values
            gasMixture.Update();

            calculated = false;
        }

        public void AddGas(float amount, GasType gas)
        {
            if (amount == 1.0f)
            {
                gasMixture.SetNextTemperature(5000f);
                LogManager.Log("Temp increase");
            }
            else
            {
                gasMixture.AddNextGas(amount, gas);
            }
        }

        public void CalculateNextGasAmount(IMapManager m)
        {
            if (calculated)
                return;

            if (!attachedTile.GasPermeable)
                return;

            float DAmount;

            GasCell neighbor;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) // If we're on this cell
                        continue;

                    Tile t = neighbours[i + 1, j + 1];
                    if (t == null || t.gasCell == null)
                        continue;

                    neighbor = t.gasCell;


                    if (Math.Abs(i) + Math.Abs(j) == 2) //If its a corner
                    {
                        if (!neighbours[i + 1, 1].GasPermeable && !neighbours[1, j + 1].GasPermeable)
                        {
                            // And it is a corner separated from us by 2 blocking walls
                            continue; //Don't process it. These cells are not connected.
                        }
                    }

                    if (neighbor.Calculated) // if neighbor's already been calculated, skip it
                        continue;

                    if (neighbor.attachedTile.GasPermeable)
                    {
                        if (gasMixture.Burning || neighbor.gasMixture.Burning)
                        {
                            neighbor.gasMixture.Expose();
                            gasMixture.Expose();
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



                            if (gasVel.Magnitude > 0.0f)
                            {
                                angle = (float)Math.Abs(neighDir.Angle(gasVel));
                                if (Math.Abs(angle) < quarterpi)
                                {
                                    pAmount += VelClamp((float)gasVel.Magnitude);//Clamp(GasMixture.Pressure / neighbor.GasMixture.Pressure);
                                    proportion += Math.Abs(((1f / quarterpi) * (quarterpi - angle)) * pAmount);// (float)GasVel.Magnitude / 100f);
                                }
                            }

                            if (neighbor.gasVel.Magnitude > 0.0f)
                            {
                                var Dir = neighDir * -1;
                                angle = (float)Math.Abs(Dir.Angle(neighbor.gasVel));
                                if (angle < quarterpi)
                                {
                                    pAmount += VelClamp((float)neighbor.gasVel.Magnitude); //Clamp(neighbor.GasMixture.Pressure / GasMixture.Pressure);
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
            int typesCount = Enum.GetValues(typeof (GasType)).Length;
            var gasChanges = new byte[typesCount];
            int bitCount = typesCount;
            for (int i = typesCount - 1; i >= 0; i--)
            {
                byte amount;
                var t = (GasType) i;
                switch (t)
                {
                    case GasType.Toxin:
                        if (gasMixture.gasses[(int)GasType.Toxin] > 0.00005f && (checkUpdateThreshold(GasType.Toxin) || all))
                        {
                            amount = (byte)normalizeGasAmount(gasMixture.gasses[(int)GasType.Toxin]);
                            gasChanges[i] = amount;
                            changedTypes = (changedTypes | (1 << i));
                            lastSentGasses[GasType.Toxin] = gasMixture.gasses[(int)GasType.Toxin];
                            bitCount += 4;
                        }
                        else
                        {
                            lastSentGasses[GasType.Toxin] = 0;
                        }
                        break;
                    case GasType.WVapor:
                        //if (gasMixture.gasses[GasType.WVapor] > 0.005f && (checkUpdateThreshold(GasType.WVapor) || all))
                        if (gasMixture.Burning)
                        {
                            amount = 15; // normalizeGasAmount(gasMixture.gasses[GasType.WVapor]);
                            gasChanges[i] = amount;
                            changedTypes = (changedTypes | (1 << i));
                            lastSentGasses[GasType.WVapor] = gasMixture.gasses[(int)GasType.WVapor];
                            bitCount += 4;
                        }
                        else
                        {
                            amount = 0; // normalizeGasAmount(gasMixture.gasses[GasType.WVapor]);
                            gasChanges[i] = amount;
                            changedTypes = (changedTypes | (1 << i));
                            lastSentGasses[GasType.WVapor] = gasMixture.gasses[(int)GasType.WVapor];
                            bitCount += 4;
                            //lastSentGasses[GasType.WVapor] = 0;
                        }
                        break;
                    default:
                        break;
                }
            }

            //Make a new bitstream with the number o bits we need.
            bits.Write(changedTypes, 0, typesCount); //write 8 bits for what gas types have changed...
            for (int i = typesCount - 1; i >= 0; i--)
            {
                int type = 1 << i;
                //Checks flags in the form of 00001011 -- each 1 is a gas type that needs sending... I know this is nuts but it works great!
                if ((changedTypes & type) == type)
                {
                    bits.Write(gasChanges[i], 0, 4);
                }
            }

            return bitCount;
        }

        public float GasAmount(GasType type)
        {
            return gasMixture.gasses[(int)type];
        }

        public float TotalGas
        {
            get { return gasMixture.TotalGas; }
        }

        public Vector2 GasVelocity
        {
            get { return gasVel*VelocityMultiplier; }
        }

        public bool Calculated
        {
            get { return calculated; }
        }

        public float Pressure
        {
            get { return gasMixture.Pressure; }
        }

        #endregion

        public void AttachToTile(Tile t)
        {
            attachedTile = t;
        }

        public void SetRadius(float r)
        {
            if (r > 7.5)
                r = 7.5f;
            if (r < 0)
                r = 0;
            //Defunct
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

        private bool checkUpdateThreshold(GasType g, float multiplier = 2000)
        {
            //If the delta since the last update was sent is greater than 2, send another update.
            if (
                Math.Abs(normalizeGasAmount(gasMixture.gasses[(int)g], multiplier) -
                         normalizeGasAmount(lastSentGasses[g], multiplier)) >= 1)
                return true;
            return false;
        }

        /// <summary>
        /// Normalize a gas amount to between 0 and 15 for packing.
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public int normalizeGasAmount(float amount, float multiplier = 2000)
        {
            amount = amount*multiplier;
            if (amount > 150)
                amount = 150;
            else if (amount < 2)
                amount = 0;
            return (int) (amount/10);
        }
    }
}