using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BKSystem.IO;
using SS13_Shared;
using SS13.IoC;
using ServerInterfaces.Atmos;

namespace ServerServices.Atmos
{
    public class GasMixture
    {

        private float nextGasAmount;
        private float gasAmount;
        private float temperature = 2.0f; // ~Space temp in K
        private float volume = 2.0f; // in m^3

        public Dictionary<GasType, float> gasses; // Type, moles
        public Dictionary<GasType, float> nextGasses;
        public Dictionary<GasType, float> lastSentGasses;

        public GasMixture()
        {
            InitGasses();
        }

        private void InitGasses()
        {
            gasses = new Dictionary<GasType, float>();
            nextGasses = new Dictionary<GasType, float>();
            lastSentGasses = new Dictionary<GasType, float>();

            var gasTypes = Enum.GetValues(typeof(GasType));
            foreach (GasType g in gasTypes)
            {
                gasses.Add(g, 0);
                nextGasses.Add(g, 0);
                lastSentGasses.Add(g, 0);
            }
        }

        public void Update()
        {
            foreach (var ng in nextGasses)
            {
                gasses[ng.Key] = ng.Value;
            }
        }

        public void SetNextGasAmount(float amount)
        {
            nextGasAmount = amount;
        }

        public float GasAmount
        {
            get
            {
                return gasAmount;
            }
        }

        public void AddGas(float amount, GasType gas)
        {
            gasses[gas] += amount;
            nextGasses[gas] += amount;
            CalculateTotals();
        }

        public void RemoveGas(float amount, GasType gas )
        {
            gasses[gas] -= amount;
            nextGasses[gas] -= amount;
            CalculateTotals();
        }

        public void AddNextGas(float amount, GasType gas)
        {
            nextGasses[gas] += amount;
        }

        public void RemoveNextGas(float amount, GasType gas)
        {
            nextGasses[gas] -= amount;
        }

        public void CalculateTotals()
        {
            gasAmount = 0;
            foreach (float g in gasses.Values.ToArray())
            {
                gasAmount += g;
            }

            nextGasAmount = 0;
            foreach (float n in nextGasses.Values.ToArray())
            {
                nextGasAmount += n;
            }
        }

        public void Diffuse(GasMixture a)
        {
            foreach (var gas in a.gasses)
            {
                if (gas.Value > gasses[gas.Key])
                {
                    var amount = (gas.Value - gasses[gas.Key]) / 8;
                    a.RemoveNextGas(amount, gas.Key);
                    AddNextGas(amount, gas.Key);
                }
                else if (gasses[gas.Key] > gas.Value)
                {
                    var amount = (gasses[gas.Key] - gas.Value) / 8;
                    a.AddNextGas(amount, gas.Key);
                    RemoveNextGas(amount, gas.Key);
                }
            }
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

        public float Temperature
        {
            get
            {
                return temperature;
            }

            set
            {
                temperature = value;
            }
        }

        // P = nRT / V
        public float Pressure
        {
            get
            {
                return ((TotalGas * 8.314f * Temperature) / Volume);
            }
        }

        public float Volume
        {
            get
            {
                return volume;
            }
            set
            {
                volume = value;
            }
        }

        public float MassOf(GasType gas)
        {
            return (IoCManager.Resolve<IAtmosManager>().GetGasProperties(gas).MolecularMass * gasses[gas]);
        }


        
    }
}
