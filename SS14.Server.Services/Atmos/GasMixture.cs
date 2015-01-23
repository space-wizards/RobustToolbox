using SS14.Server.Interfaces.Atmos;
using SS14.Shared;
using SS14.Shared.IoC;
using System;

namespace SS14.Server.Services.Atmos
{
    public class GasMixture
    {
        private bool burning;
        private bool exposed; // Have we been exposed to a source of ignition?

        public float[] gasses;
        public float[] lastSentGasses;
        public float[] nextGasses;
        private float nextTemperature;
        private float temperature = 293.15f; // Normal room temp in K
        private float volume = 2.0f; // in m^3
        private IAtmosManager _atmosManager;

        public GasMixture()
        {
            _atmosManager = IoCManager.Resolve<IAtmosManager>();
            InitGasses();
        }

        public float TotalGas
        {
            get
            {
                float total = 0;
                foreach (var gas in gasses)
                {
                    total += gas;
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

        public float HeatCapacity { get; private set; }

        public float TotalMass { get; private set; }

        public bool Burning
        {
            get { return burning; }
        }

        private void InitGasses()
        {
            gasses = new float[_atmosManager.NumGasTypes];
            lastSentGasses = new float[_atmosManager.NumGasTypes];
            nextGasses = new float[_atmosManager.NumGasTypes];

            Array gasTypes = Enum.GetValues(typeof (GasType));
            foreach (GasType g in gasTypes)
            {
                gasses[(int)g] = 0;
                nextGasses[(int)g] = 0;
                lastSentGasses[(int)g] = 0;
            }
            UpdateHeatCapacity();
            UpdateTotalMass();
        }

        public void Update()
        {
            for (var i = 0; i < nextGasses.Length; i++)
            {
                gasses[i] = nextGasses[i];
            }
            temperature += nextTemperature;
            nextTemperature = 0;
            if (temperature < 0) // This shouldn't happen unless someone fucks with the temperature directly
                temperature = 0;
            exposed = false;
            UpdateHeatCapacity();
            UpdateTotalMass();
        }

        public void UpdateHeatCapacity()
        {
            float SHC = 0.0f;
            for (var i = 0; i < gasses.Length; i++)
            {
                SHC += (gasses[i] * _atmosManager.GetGasProperties((GasType)i).SpecificHeatCapacity);
            }
            
            HeatCapacity = SHC;
        }

        public void UpdateTotalMass()
        {
            float mass = 0.0f;

            for (var i = 0; i < gasses.Length; i++)
            {
                mass += (gasses[i] * _atmosManager.GetGasProperties((GasType)i).MolecularMass);
            }
            TotalMass = mass;
        }

        public void SetNextTemperature(float temp)
        {
            nextTemperature += temp;
        }

        public void AddNextGas(float amount, GasType gas)
        {
            nextGasses[(int)gas] += amount;

            if (nextGasses[(int)gas] < 0) // This shouldn't happen unless someone calls this directly but lets just make sure
                nextGasses[(int)gas] = 0;
        }

        public void Diffuse(GasMixture a, float factor = 8)
        {
            for (var i = 0; i < a.gasses.Length; i++)
            {
                float amount = (a.gasses[i] - gasses[i])/factor;
                AddNextGas(amount, (GasType)i);
                a.AddNextGas(-amount, (GasType)i);
            }

            ShareTemp(a, factor);
        }

        public void ShareTemp(GasMixture a, float factor = 8)
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
                for (var i = 0; i < gasses.Length; i++)
                {
                    float ait = _atmosManager.GetGasProperties((GasType)i).AutoignitionTemperature;
                    if (ait > 0.0f && temperature > ait)
                        // If our temperature is high enough to autoignite then we're burning now
                    {
                        burning = true;
                    }
                }
            }

            float energy_released = 0.0f;

            if (Burning || exposed) // We're going to try burning some of our gasses
            {
                float cAmount = 0.0f;
                float oAmount = 0.0f;
                for (var i = 0; i < nextGasses.Length; i++)
                {
                    if (_atmosManager.GetGasProperties((GasType)i).Combustable)
                    {
                        cAmount += gasses[i];
                    }
                    if (_atmosManager.GetGasProperties((GasType)i).Oxidant)
                    {
                        oAmount += gasses[i];
                    }
                }

                if (oAmount > 0.0001f && cAmount > 0.0001f && Pressure > 10)
                {
                    float ratio = Math.Min(1f, oAmount/cAmount);
                    // This is how much of each gas we can burn as that's how much oxidant we have free
                    ratio /= 3; // Lets not just go mental and burn everything in one go because that's dumb
                    float amount = 0.0f;

                    for (var i = 0; i < gasses.Length; i++)
                    {
                        amount = gasses[i]*ratio;
                        if (_atmosManager.GetGasProperties((GasType)i).Combustable)
                        {
                            AddNextGas(-amount, (GasType)i);
                            AddNextGas(amount, GasType.CO2);
                            energy_released += (_atmosManager.GetGasProperties((GasType)i).SpecificHeatCapacity * 2000 * amount);
                            // This is COMPLETE bullshit non science but whatever
                        }
                        if (_atmosManager.GetGasProperties((GasType)i).Oxidant)
                        {
                            AddNextGas(-amount, (GasType)i);
                            AddNextGas(amount, GasType.CO2);
                            energy_released += (_atmosManager.GetGasProperties((GasType)i).SpecificHeatCapacity * 2000 * amount);
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
            return (_atmosManager.GetGasProperties(gas).MolecularMass * gasses[(int)gas]);
        }
    }
}