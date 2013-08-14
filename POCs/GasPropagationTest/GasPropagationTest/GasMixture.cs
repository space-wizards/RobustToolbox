using System;
using System.Collections.Generic;

namespace GasPropagationTest {
    public enum GasType
    {
        Oxygen = 0, // MUST BE 1 FOR NETWORKING
        Toxin = 1,
        Nitrogen = 2,
        CO2 = 3,
        WVapor = 4
    }
    
    public class GasMixture
    {
        private bool burning;
        private bool exposed; // Have we been exposed to a source of ignition?

        public Dictionary<GasType, float> gasses; // Type, moles
        public Dictionary<GasType, float> lastSentGasses;
        public Dictionary<GasType, float> nextGasses;
        private float nextTemperature;
        private float temperature = 293.15f; // Normal room temp in K
        private float volume = 2.0f; // in m^3
        private AtmosManager _atmosManager;

        public GasMixture(AtmosManager atmosManager)
        {
            _atmosManager = atmosManager;
            InitGasses();
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
            get { return temperature; }
        }

        // P = nRT / V
        public float Pressure
        {
            get { return ((TotalGas*8.314f*Temperature)/Volume); }
        }

        public float Volume
        {
            get { return volume; }
            set { volume = value; }
        }

        public float HeatCapacity
        {
            get
            {
                float SHC = 0.0f;
                foreach (GasType g in gasses.Keys)
                {
                    SHC += (gasses[g]*_atmosManager.GetGasProperties(g).SpecificHeatCapacity);
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
                    mass += (gasses[g] * _atmosManager.GetGasProperties(g).MolecularMass);
                }

                return mass;
            }
        }

        public bool Burning
        {
            get { return burning; }
        }

        private void InitGasses()
        {
            gasses = new Dictionary<GasType, float>();
            nextGasses = new Dictionary<GasType, float>();
            lastSentGasses = new Dictionary<GasType, float>();

            Array gasTypes = Enum.GetValues(typeof (GasType));
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
            if (temperature < 0) // This shouldn't happen unless someone fucks with the temperature directly
                temperature = 0;
            exposed = false;
        }

        public void SetNextTemperature(float temp)
        {
            nextTemperature += temp;
        }

        public void AddNextGas(float amount, GasType gas)
        {
            nextGasses[gas] += amount;

            if (nextGasses[gas] < 0) // This shouldn't happen unless someone calls this directly but lets just make sure
                nextGasses[gas] = 0;
        }

        public float Diffuse(GasMixture a, float factor = 9)
        {
            float totalAmount = 0.0f;
            foreach (var gas in a.gasses)
            {
                float amount = (gas.Value - gasses[gas.Key])/factor;
                AddNextGas(amount, gas.Key);
                a.AddNextGas(-amount, gas.Key);
                totalAmount += amount;
            }

            ShareTemp(a, factor);

            return Math.Abs(totalAmount);
        }

        public void ShareTemp(GasMixture a, float factor = 9)
        {
            float HCCell = HeatCapacity*TotalMass;
            float HCa = a.HeatCapacity*a.TotalMass;
            float energyFlow = a.Temperature - Temperature;

            if (energyFlow > 0.0f)
            {
                energyFlow *= HCa;
            }
            else
            {
                energyFlow *= HCCell;
            }

            energyFlow *= (1/factor);
            SetNextTemperature((energyFlow/HCCell));
            a.SetNextTemperature(-(energyFlow/HCa));
        }

        public void Expose()
        {
            exposed = true;
        }

        public void Burn()
        {
            if (!Burning) // If we're not burning lets see if we can start due to autoignition
            {
                foreach (GasType g in gasses.Keys)
                {
                    float ait = _atmosManager.GetGasProperties(g).AutoignitionTemperature;
                    if (ait > 0.0f && temperature > ait)
                        // If our temperature is high enough to autoignite then we're burning now
                    {
                        burning = true;
                        continue;
                    }
                }
            }

            float energy_released = 0.0f;

            if (Burning || exposed) // We're going to try burning some of our gasses
            {
                float cAmount = 0.0f;
                float oAmount = 0.0f;
                foreach (GasType g in nextGasses.Keys)
                {
                    if (_atmosManager.GetGasProperties(g).Combustable)
                    {
                        cAmount += gasses[g];
                    }
                    if (_atmosManager.GetGasProperties(g).Oxidant)
                    {
                        oAmount += gasses[g];
                    }
                }

                if (oAmount > 0.0001f && cAmount > 0.0001f && Pressure > 10)
                {
                    float ratio = Math.Min(1f, oAmount/cAmount);
                    // This is how much of each gas we can burn as that's how much oxidant we have free
                    ratio /= 3; // Lets not just go mental and burn everything in one go because that's dumb
                    float amount = 0.0f;

                    foreach (GasType g in gasses.Keys)
                    {
                        amount = gasses[g]*ratio;
                        if (_atmosManager.GetGasProperties(g).Combustable)
                        {
                            AddNextGas(-amount, g);
                            AddNextGas(amount, GasType.CO2);
                            energy_released += (_atmosManager.GetGasProperties(g).SpecificHeatCapacity * 2000 * amount);
                            // This is COMPLETE bullshit non science but whatever
                        }
                        if (_atmosManager.GetGasProperties(g).Oxidant)
                        {
                            AddNextGas(-amount, g);
                            AddNextGas(amount, GasType.CO2);
                            energy_released += (_atmosManager.GetGasProperties(g).SpecificHeatCapacity * 2000 * amount);
                            // This is COMPLETE bullshit non science but whatever
                        }
                    }
                }
            }

            if (energy_released > 0)
            {
                SetNextTemperature(energy_released *= HeatCapacity);
                // This is COMPLETE bullshit non science but whatever
                burning = true;
            }
            else // Nothing burnt here so we're not on fire
            {
                burning = false;
            }
        }

        public float MassOf(GasType gas)
        {
            return (_atmosManager.GetGasProperties(gas).MolecularMass * gasses[gas]);
        }
    }
}