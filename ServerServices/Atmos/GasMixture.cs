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
        private float temperature = 293.15f; // Normal room temp in K
        private float nextTemperature = 0;
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

            temperature += nextTemperature;
            nextTemperature = 0;
            if (temperature < 0)    // This shouldn't happen unless someone fucks with the temperature directly
                temperature = 0;

        }

        public void SetNextTemperature(float temp)
        {
            nextTemperature += temp;
        }

        public void AddNextGas(float amount, GasType gas)
        {
            nextGasses[gas] += amount;

            if (nextGasses[gas] < 0)    // This shouldn't happen unless someone calls this directly but lets just make sure
                nextGasses[gas] = 0;
        }

        public void Diffuse(GasMixture a, float factor = 8)
        {
            foreach (var gas in a.gasses)
            {
                var amount = (gas.Value - gasses[gas.Key]) / factor;
                AddNextGas(amount, gas.Key);
                a.AddNextGas(-amount, gas.Key);
            }

            ShareTemp(a, factor);
        }

        public void ShareTemp(GasMixture a, float factor = 8)
        {
            float HCCell = HeatCapacity * TotalMass;
            float HCa = a.HeatCapacity * a.TotalMass;
            float energyFlow = a.Temperature - Temperature;

            if (energyFlow > 0.0f)
            {
                energyFlow *= HCa;
            }
            else
            {
                energyFlow *= HCCell;
            }

            energyFlow *= (1 / factor);
            SetNextTemperature((energyFlow / HCCell));
            a.SetNextTemperature(-(energyFlow / HCa));
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

        public float HeatCapacity
        {
            get
            {
                float SHC = 0.0f;
                foreach (GasType g in gasses.Keys)
                {
                    SHC += (gasses[g] * IoCManager.Resolve<IAtmosManager>().GetGasProperties(g).SpecificHeatCapacity);
                }
                
                return SHC;
            }
        }

        public float TotalMass
        {
            get
            {
                float mass = 0.0f;

                foreach (GasType g in gasses.Keys)
                {
                    mass += (gasses[g] * IoCManager.Resolve<IAtmosManager>().GetGasProperties(g).MolecularMass);
                }

                return mass;
            }
        }


        
    }
}
