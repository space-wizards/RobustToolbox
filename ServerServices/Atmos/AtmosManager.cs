using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using Lidgren.Network;
using ServerInterfaces;
using ServerInterfaces.GameObject;
using ServerInterfaces.Network;
using SS13.IoC;
using SS13_Shared;
using ServerInterfaces.Atmos;
using ServerInterfaces.Map;
using BKSystem.IO;
using ServerServices.Log;
using ServerServices.Tiles;

namespace ServerServices.Atmos
{


    public class AtmosManager : IAtmosManager
    {
        DateTime lastAtmosDisplayPush;

        private Dictionary<GasType, IGasProperties> gasProperties;

        public AtmosManager()
        {
            gasProperties = new Dictionary<GasType, IGasProperties>();
            gasProperties.Add(GasType.Oxygen, new Oxygen());
            gasProperties.Add(GasType.CO2, new CO2());
            gasProperties.Add(GasType.Nitrogen, new Nitrogen());
            gasProperties.Add(GasType.Toxin, new Toxin());
            gasProperties.Add(GasType.WVapor, new WVapor());
        }

        public void InitializeGasCells()
        {
            for (int x = 0; x < IoCManager.Resolve<IMapManager>().GetMapWidth(); x++)
            {
                for (int y = 0; y < IoCManager.Resolve<IMapManager>().GetMapHeight(); y++)
                {
                    var t = IoCManager.Resolve<IMapManager>().GetTileAt(x, y);
                    t.GasCell = new GasCell(x, y, (Tile)t);
                    if (t.StartWithAtmos)
                    {
                        t.GasCell.InitSTP();
                    }
                }
            }
        }

        public void Update()
        {
            var m = IoCManager.Resolve<IMapManager>();

            for (int x = 0; x < m.GetMapWidth(); x++)
            {
                for (int y = 0; y < m.GetMapHeight(); y++)
                {
                    m.GetTileAt(x, y).GasCell.CalculateNextGasAmount(m);
                }
            }


            for (int x = 0; x < m.GetMapWidth(); x++)
            {
                for (int y = 0; y < m.GetMapHeight(); y++)
                {
                    if (m.GetTileAt(x, y).TileState == TileState.Dead && m.GetTileAt(x, y).GasPermeable)
                    {
                        m.DestroyTile(new Point(x, y));
                    }

                    m.GetTileAt(x, y).GasCell.Update();
                }
            }

            CheckNetworkUpdate();
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

        public void TotalAtmosReport()
        {
            var m = IoCManager.Resolve<IMapManager>();

            float totalGas = 0.0f;
            for (int x = 0; x < m.GetMapWidth(); x++)
            {
                for (int y = 0; y < m.GetMapHeight(); y++)
                {
                    totalGas += m.GetTileAt(x, y).GasCell.TotalGas;
                }
            }

            Log.LogManager.Log("Report: " + totalGas);
        }

        #region Networking

        private void CheckNetworkUpdate()
        {
            var m = IoCManager.Resolve<IMapManager>();

            if ((DateTime.Now - lastAtmosDisplayPush).TotalMilliseconds > 1000)
            {
                bool sendUpdate = false;
                int numberOfGasTypes = Enum.GetValues(typeof(GasType)).Length;
                var records = new BitStream(m.GetMapHeight() * m.GetMapWidth() * numberOfGasTypes);
                for (var x = 0; x < m.GetMapWidth(); x++)
                    for (var y = 0; y < m.GetMapHeight(); y++)
                    {
                        int displayBitsWritten = m.GetTileAt(x, y).GasCell.PackDisplayBytes(records);
                        if (displayBitsWritten > numberOfGasTypes)
                            sendUpdate = true;
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
            var msg = CreateAtmosUpdatePacket(records);
            if (msg == null)
                return;
            IoCManager.Resolve<ISS13NetServer>().SendToAll(msg, NetDeliveryMethod.Unreliable);
        }

        private NetOutgoingMessage CreateAtmosUpdatePacket(BitStream records)
        {
            var recs = Compress(records);
            if (recs == null)
                return null;
            var msg = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
            msg.Write((byte)NetMessage.AtmosDisplayUpdate);
            msg.Write((int)records.Length);
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
            var byteLength = (int)rawStream.Length8;
            if (byteLength == 0)
            {
                return null;
            }

            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true))
                {
                    gzip.Write(raw, 0, raw.Length);
                }
                return memory.ToArray();
            }
        }

        public void SendAtmosStateTo(NetConnection client)
        {
            var m = IoCManager.Resolve<IMapManager>();

            int numberOfGasTypes = Enum.GetValues(typeof(GasType)).Length;
            var records = new BitStream(m.GetMapHeight() * m.GetMapWidth() * numberOfGasTypes);
            int displayBitsWritten = 0;
            for (int x = 0; x < m.GetMapWidth(); x++)
                for (int y = 0; y < m.GetMapHeight(); y++)
                {
                    displayBitsWritten = m.GetTileAt(x, y).GasCell.PackDisplayBytes(records, true);
                }
            var msg = CreateAtmosUpdatePacket(records);
            if (msg == null)
                return;
            IoCManager.Resolve<ISS13NetServer>().SendMessage(msg, client, NetDeliveryMethod.ReliableUnordered);

        }

        private struct AtmosRecord
        {
            int x;
            int y;
            byte display;

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

        public string Name { get { return name; } }
        public float SpecificHeatCapacity { get { return shc; } }
        public GasType Type { get { return type; } }
        public float MolecularMass { get { return mm; } }
    }

    public class CO2 : IGasProperties
    {
        private const string name = "CO2";
        private const float shc = 0.844f;
        private const float mm = 44.01f;
        private const GasType type = GasType.CO2;

        public string Name { get { return name; } }
        public float SpecificHeatCapacity { get { return shc; } }
        public GasType Type { get { return type; } }
        public float MolecularMass { get { return mm; } }
    }

    public class Nitrogen : IGasProperties
    {
        private const string name = "Nitrogen";
        private const float shc = 1.04f;
        private const float mm = 28.01f;
        private const GasType type = GasType.Nitrogen;

        public string Name { get { return name; } }
        public float SpecificHeatCapacity { get { return shc; } }
        public GasType Type { get { return type; } }
        public float MolecularMass { get { return mm; } }
    }

    public class Toxin : IGasProperties
    {
        private const string name = "Toxin";
        private const float shc = 4.00f;    // Made up
        private const float mm = 20.0f; // Made up
        private const GasType type = GasType.Toxin;

        public string Name { get { return name; } }
        public float SpecificHeatCapacity { get { return shc; } }
        public GasType Type { get { return type; } }
        public float MolecularMass { get { return mm; } }
    }

    public class WVapor : IGasProperties
    {
        private const string name = "Water Vapour";
        private const float shc = 1.93f;
        private const float mm = 16.0f;
        private const GasType type = GasType.WVapor;


        public string Name { get { return name; } }
        public float SpecificHeatCapacity { get { return shc; } }
        public GasType Type { get { return type; } }
        public float MolecularMass { get { return mm; } }
    }

    #endregion
}
