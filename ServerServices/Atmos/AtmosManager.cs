using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using BKSystem.IO;
using Lidgren.Network;
using SS13.IoC;
using SS13_Shared;
using ServerInterfaces.Atmos;
using ServerInterfaces.Map;
using ServerInterfaces.Network;
using ServerInterfaces.Tiles;
using ServerServices.Log;
using ServerServices.Tiles;

namespace ServerServices.Atmos
{
    public class AtmosManager : IAtmosManager
    {
        private IGasProperties[] gasProperties;
        //private readonly Dictionary<GasType, IGasProperties> gasProperties;
        private DateTime lastAtmosDisplayPush;
        private float elapsedSinceLastFrame;
        private const float fps = 15;

        public int NumGasTypes {
            get { return Enum.GetNames(typeof(GasType)).Length; }}

        public AtmosManager()
        {
            //gasProperties = new Dictionary<GasType, IGasProperties>();
            gasProperties = new IGasProperties[NumGasTypes];
            gasProperties[(int)GasType.Oxygen] = new Oxygen();
            gasProperties[(int)GasType.CO2] = new CO2();
            gasProperties[(int)GasType.Nitrogen] = new Nitrogen();
            gasProperties[(int)GasType.Toxin] = new Toxin();
            gasProperties[(int)GasType.WVapor] = new WVapor();
        }

        #region IAtmosManager Members

        public void InitializeGasCells()
        {
            var m = IoCManager.Resolve<IMapManager>();
            foreach (ITile t in m.GetITilesIn(m.GetWorldArea()))
            {
                t.GasCell = new GasCell((Tile)t);
                if (t.StartWithAtmos)
                    t.GasCell.InitSTP();
            }

            foreach(Tile t in m.GetITilesIn(new RectangleF(0, 0, m.GetMapWidth() * m.GetTileSpacing(), m.GetMapHeight() * m.GetTileSpacing())))
            {
                t.gasCell = new GasCell(t);
                if(t.StartWithAtmos)
                {
                    t.gasCell.InitSTP();
                    t.gasCell.SetNeighbours(m);
                }

            }
        }

        public void Update(float frametime)
        {
            lock (this)
            {
                elapsedSinceLastFrame += frametime;
                if (elapsedSinceLastFrame < (1/fps))
                    return;
                elapsedSinceLastFrame = 0;
                var m = IoCManager.Resolve<IMapManager>();

                foreach (Tile t in m.GetITilesIn(m.GetWorldArea()))
                {
                    t.gasCell.CalculateNextGasAmount(m);
                }

                foreach (Tile t in m.GetITilesIn(m.GetWorldArea()))
                {
                    if (t.TileState == TileState.Dead && t.GasPermeable)
                    {
                        m.DestroyTile(t.WorldPosition);
                    }
                    t.GasCell.Update();
                }

                CheckNetworkUpdate();
            }
        }

        public IGasProperties GetGasProperties(GasType g)
        {
            return gasProperties[(int)g];
        }

        public void TotalAtmosReport()
        {
            /*var m = IoCManager.Resolve<IMapManager>();

            float totalGas = 0.0f;
            for (int x = 0; x < m.GetMapWidth(); x++)
            {
                for (int y = 0; y < m.GetMapHeight(); y++)
                {
                    Tile t = (Tile)m.GetTileFromIndex(x, y);
                    if (t == null)
                        continue;
                    totalGas += t.GasCell.TotalGas;
                }
            }

            LogManager.Log("Report: " + totalGas);*/
        }

        #endregion

        #region Networking

        public void SendAtmosStateTo(NetConnection client)
        {
            var m = IoCManager.Resolve<IMapManager>();

            int numberOfGasTypes = Enum.GetValues(typeof (GasType)).Length;
            var records = new BitStream(m.GetMapHeight()*m.GetMapWidth()*numberOfGasTypes);
            int displayBitsWritten = 0;

            for (int x = 0; x < m.GetMapWidth(); x++)
            {
                for (int y = 0; y < m.GetMapHeight(); y++)
                {
                    Tile t = (Tile)m.GetITileAt(new Vector2(x * m.GetTileSpacing(), y * m.GetTileSpacing()));
                    if (t == null)
                        continue;
                    displayBitsWritten = t.GasCell.PackDisplayBytes(records, true);
                }
            }
            NetOutgoingMessage msg = CreateAtmosUpdatePacket(records);
            if (msg == null)
                return;
            IoCManager.Resolve<ISS13NetServer>().SendMessage(msg, client, NetDeliveryMethod.ReliableUnordered);
        }

        private void CheckNetworkUpdate()
        {
            var m = IoCManager.Resolve<IMapManager>();

            if ((DateTime.Now - lastAtmosDisplayPush).TotalMilliseconds > 1000)
            {
                bool sendUpdate = false;
                int numberOfGasTypes = Enum.GetValues(typeof (GasType)).Length;
                var records = new BitStream(m.GetMapHeight()*m.GetMapWidth()*numberOfGasTypes);
                for (int x = 0; x < m.GetMapWidth(); x++)
                {
                    for (int y = 0; y < m.GetMapHeight(); y++)
                    {
                        Tile t = (Tile)m.GetITileAt(new Vector2(x * m.GetTileSpacing(), y * m.GetTileSpacing()));
                        if (t == null)
                            continue;
                        int displayBitsWritten = t.GasCell.PackDisplayBytes(records);
                        if (displayBitsWritten > numberOfGasTypes)
                            sendUpdate = true;
                    }
                }
                if (sendUpdate)
                {
                    SendAtmosUpdatePacket(records);
                }
                lastAtmosDisplayPush = DateTime.Now;
            }
        }

        private void SendAtmosUpdatePacket(BitStream records)
        {
            NetOutgoingMessage msg = CreateAtmosUpdatePacket(records);
            if (msg == null)
                return;
            IoCManager.Resolve<ISS13NetServer>().SendToAll(msg, NetDeliveryMethod.Unreliable);
        }

        private NetOutgoingMessage CreateAtmosUpdatePacket(BitStream records)
        {
            byte[] recs = Compress(records);
            if (recs == null)
                return null;
            NetOutgoingMessage msg = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
            msg.Write((byte) NetMessage.AtmosDisplayUpdate);
            msg.Write((int) records.Length);
            msg.Write(recs.Length);
            msg.Write(recs);
            return msg;
        }

        private byte[] Compress(BitStream rawStream)
        {
            rawStream.Position = 0;
            var raw = new byte[rawStream.Length8];
            for (int i = 0; i < rawStream.Length8; i++)
            {
                rawStream.Read(out raw[i]);
            }
            var byteLength = (int) rawStream.Length8;
            if (byteLength == 0)
            {
                return null;
            }

            using (var memory = new MemoryStream())
            {
                using (var gzip = new GZipStream(memory, CompressionMode.Compress, true))
                {
                    gzip.Write(raw, 0, raw.Length);
                }
                return memory.ToArray();
            }
        }

        private struct AtmosRecord
        {
            private readonly byte display;
            private readonly int x;
            private readonly int y;

            public AtmosRecord(int _x, int _y, byte _display)
            {
                x = _x;
                y = _y;
                display = _display;
            }

            public void pack(NetOutgoingMessage message)
            {
                message.Write(x);
                message.Write(y);
                message.Write(display);
            }
        }

        #endregion
    }

    #region Gas Definitions

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