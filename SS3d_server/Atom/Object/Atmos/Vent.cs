using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3d_server.Atom.Object.Atmos
{
    class Vent : Object
    {
        Dictionary<GasType, float> normalGasses;
        public Vent()
            : base()
        {
            name = "Vent";
            normalGasses = new Dictionary<GasType, float>();
            var gastypes = Enum.GetValues(typeof(GasType));
            foreach (GasType g in gastypes)
            {
                normalGasses.Add(g, 0);
            }
            normalGasses[GasType.Oxygen] = 20;
            normalGasses[GasType.Nitrogen] = 80;
        }

        public override void Update(float framePeriod)
        {
            base.Update(framePeriod);

            float maxAdd = 50 * framePeriod;

            updateRequired = true;

            var nearestTile = GetNearestTile();
            if (nearestTile.tileType == TileType.Floor)
            {
                var g = nearestTile.gasCell;
                //Check for normal oxygen/nitrogen mix

                foreach (var gas in normalGasses)
                {
                    if (g.nextGasses[gas.Key] != gas.Value)
                    {
                        var gasdiff = gas.Value - g.nextGasses[gas.Key];
                        var gastoAdd = Math.Sign(gasdiff) * Math.Min(maxAdd, Math.Abs(gasdiff));
                        g.nextGasses[gas.Key] += gastoAdd;
                        Console.Write("Added " + gastoAdd.ToString() + " units of " + gas.Key.ToString() + "\n");
                    }
                }
            }
        }
    }
}
