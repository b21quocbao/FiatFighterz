﻿using System;
using System.Collections.Generic;
using System.Text;
using Utils.NET.IO;

namespace Utils.NET.Net.Udp.Packets
{
    public class UdpConnected : UdpPacket
    {
        public override UdpPacketType Type => UdpPacketType.Connected;

        /// <summary>
        /// Salt generated by server/client
        /// </summary>
        public ulong salt;

        /// <summary>
        /// The port that the connected should read from
        /// </summary>
        public ushort port;

        public UdpConnected() { }

        public UdpConnected(ulong salt, ushort port)
        {
            this.salt = salt;
            this.port = port;
        }

        protected override void Read(BitReader r)
        {
            salt = r.ReadUInt64();
            port = r.ReadUInt16();
        }

        protected override void Write(BitWriter w)
        {
            w.Write(salt);
            w.Write(port);
        }
    }
}
