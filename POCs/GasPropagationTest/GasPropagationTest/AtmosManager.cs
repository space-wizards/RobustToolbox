using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Windows.Controls;
using System.Windows.Shapes;
using Brushes = System.Windows.Media.Brushes;

namespace GasPropagationTest
{
    public class AtmosManager
    {
        private readonly Dictionary<GasType, IGasProperties> gasProperties;
        private Size _mapSize;
        public GasCell[,] cellArray;
        private Canvas _canvas;

        public AtmosManager(Size mapSize, Canvas canvas)
        {
            _mapSize = mapSize;
            _canvas = canvas;
            cellArray = new GasCell[_mapSize.Width, _mapSize.Height];
            gasProperties = new Dictionary<GasType, IGasProperties>();
            gasProperties.Add(GasType.Oxygen, new Oxygen());
            gasProperties.Add(GasType.CO2, new CO2());
            gasProperties.Add(GasType.Nitrogen, new Nitrogen());
            gasProperties.Add(GasType.Toxin, new Toxin());
            gasProperties.Add(GasType.WVapor, new WVapor());
        }

        #region IAtmosManager Members

        public void InitializeGasCells()
        {
            for (int x = 0; x < _mapSize.Width; x++)
            {
                for (int y = 0; y < _mapSize.Height; y++)
                {
                    var e = new Ellipse();
                    e.Width = 15;
                    e.Height = 15;
                    e.Fill = Brushes.Blue;
                    _canvas.Children.Add(e);
                    Canvas.SetLeft(e, (double)x * 15);
                    Canvas.SetTop(e, (double)y * 15);
                    cellArray[x, y] = new GasCell(e, x * 15, y * 15, cellArray, this);
                    cellArray[x, y].arrX = x;
                    cellArray[x, y].arrY = y;
                    cellArray[x, y].InitSTP();
                    
                }
            }
        }

        public void Calculate()
        {
            for (int x = 0; x < _mapSize.Width; x++)
            {
                for (int y = 0; y < _mapSize.Height; y++)
                {
                    cellArray[x,y].CalculateNextGasAmount();
                }
            }

        }

        public void Update()
        {
            for (int x = 0; x < _mapSize.Width; x++)
            {
                for (int y = 0; y < _mapSize.Height; y++)
                {
                    cellArray[x,y].Update(true);
                }
            }
        }

        public IGasProperties GetGasProperties(GasType g)
        {
            foreach (GasType type in gasProperties.Keys)
            {
                if (type == g)
                {
                    return gasProperties[g];
                }
            }

            return null;
        }

        #endregion


    }

    #region Gas Definitions

    public interface IGasProperties
    {
        string Name { get; }
        float SpecificHeatCapacity { get; }
        float MolecularMass { get; }
        GasType Type { get; }
        bool Combustable { get; }
        bool Oxidant { get; }
        float AutoignitionTemperature { get; }
    }
    public class Oxygen : IGasProperties
    {
        private const string name = "Oxygen";
        private const float shc = 0.919f;
        private const float mm = 32.0f;
        private const GasType type = GasType.Oxygen;
        private const bool combustable = false;
        private const bool oxidant = true;
        private const float ait = 0.0f; // Means it wont autoignite

        #region IGasProperties Members

        public string Name
        {
            get { return name; }
        }

        public float SpecificHeatCapacity
        {
            get { return shc; }
        }

        public GasType Type
        {
            get { return type; }
        }

        public float MolecularMass
        {
            get { return mm; }
        }

        public bool Combustable
        {
            get { return combustable; }
        }

        public bool Oxidant
        {
            get { return oxidant; }
        }

        public float AutoignitionTemperature
        {
            get { return ait; }
        }

        #endregion
    }

    public class CO2 : IGasProperties
    {
        private const string name = "CO2";
        private const float shc = 0.844f;
        private const float mm = 44.01f;
        private const GasType type = GasType.CO2;
        private const bool combustable = false;
        private const bool oxidant = false;
        private const float ait = 0.0f; // Means it wont autoignite

        #region IGasProperties Members

        public string Name
        {
            get { return name; }
        }

        public float SpecificHeatCapacity
        {
            get { return shc; }
        }

        public GasType Type
        {
            get { return type; }
        }

        public float MolecularMass
        {
            get { return mm; }
        }

        public bool Combustable
        {
            get { return combustable; }
        }

        public bool Oxidant
        {
            get { return oxidant; }
        }

        public float AutoignitionTemperature
        {
            get { return ait; }
        }

        #endregion
    }

    public class Nitrogen : IGasProperties
    {
        private const string name = "Nitrogen";
        private const float shc = 1.04f;
        private const float mm = 28.01f;
        private const GasType type = GasType.Nitrogen;
        private const bool combustable = false;
        private const bool oxidant = false;
        private const float ait = 0.0f; // Means it wont autoignite

        #region IGasProperties Members

        public string Name
        {
            get { return name; }
        }

        public float SpecificHeatCapacity
        {
            get { return shc; }
        }

        public GasType Type
        {
            get { return type; }
        }

        public float MolecularMass
        {
            get { return mm; }
        }

        public bool Combustable
        {
            get { return combustable; }
        }

        public bool Oxidant
        {
            get { return oxidant; }
        }

        public float AutoignitionTemperature
        {
            get { return ait; }
        }

        #endregion
    }

    public class Toxin : IGasProperties
    {
        private const string name = "Toxin";
        private const float shc = 4.00f; // Made up
        private const float mm = 20.0f; // Made up
        private const GasType type = GasType.Toxin;
        private const bool combustable = true;
        private const bool oxidant = false;
        private const float ait = 1000.0f;

        #region IGasProperties Members

        public string Name
        {
            get { return name; }
        }

        public float SpecificHeatCapacity
        {
            get { return shc; }
        }

        public GasType Type
        {
            get { return type; }
        }

        public float MolecularMass
        {
            get { return mm; }
        }

        public bool Combustable
        {
            get { return combustable; }
        }

        public bool Oxidant
        {
            get { return oxidant; }
        }

        public float AutoignitionTemperature
        {
            get { return ait; }
        }

        #endregion
    }

    public class WVapor : IGasProperties
    {
        private const string name = "Water Vapour";
        private const float shc = 1.93f;
        private const float mm = 16.0f;
        private const GasType type = GasType.WVapor;
        private const bool combustable = false;
        private const bool oxidant = false;
        private const float ait = 0.0f; // Means it wont autoignite

        #region IGasProperties Members

        public string Name
        {
            get { return name; }
        }

        public float SpecificHeatCapacity
        {
            get { return shc; }
        }

        public GasType Type
        {
            get { return type; }
        }

        public float MolecularMass
        {
            get { return mm; }
        }

        public bool Combustable
        {
            get { return combustable; }
        }

        public bool Oxidant
        {
            get { return oxidant; }
        }

        public float AutoignitionTemperature
        {
            get { return ait; }
        }

        #endregion
    }

    #endregion
}