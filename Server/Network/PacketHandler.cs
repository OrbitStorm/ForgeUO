using System;

namespace Server.Network
{
    public delegate void OnPacketReceive(NetState state, PacketReader pvSrc);
    public delegate bool ThrottlePacketCallback(NetState state);

    public class PacketHandler
    {
        private readonly int m_PacketID;
        private readonly int m_Length;
        private readonly bool m_Ingame;
        private readonly OnPacketReceive m_OnReceive;
        private ThrottlePacketCallback m_ThrottleCallback;
        public PacketHandler(int packetID, int length, bool ingame, OnPacketReceive onReceive)
        {
            this.m_PacketID = packetID;
            this.m_Length = length;
            this.m_Ingame = ingame;
            this.m_OnReceive = onReceive;
        }

        public int PacketID
        {
            get
            {
                return this.m_PacketID;
            }
        }
        public int Length
        {
            get
            {
                return this.m_Length;
            }
        }
        public OnPacketReceive OnReceive
        {
            get
            {
                return this.m_OnReceive;
            }
        }
        public ThrottlePacketCallback ThrottleCallback
        {
            get
            {
                return this.m_ThrottleCallback;
            }
            set
            {
                this.m_ThrottleCallback = value;
            }
        }
        public bool Ingame
        {
            get
            {
                return this.m_Ingame;
            }
        }
    }
}