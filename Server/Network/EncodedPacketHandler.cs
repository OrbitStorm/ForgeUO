/***************************************************************************
*                          EncodedPacketHandler.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: EncodedPacketHandler.cs 4 2006-06-15 04:28:39Z mark $
*
***************************************************************************/








/***************************************************************************
*
*   This program is free software; you can redistribute it and/or modify
*   it under the terms of the GNU General Public License as published by
*   the Free Software Foundation; either version 2 of the License, or
*   (at your option) any later version.
*
***************************************************************************/
using System;

namespace Server.Network
{
    public delegate void OnEncodedPacketReceive(NetState state, IEntity ent, EncodedReader pvSrc);

    public class EncodedPacketHandler
    {
        private readonly int m_PacketID;
        private readonly bool m_Ingame;
        private readonly OnEncodedPacketReceive m_OnReceive;
        public EncodedPacketHandler(int packetID, bool ingame, OnEncodedPacketReceive onReceive)
        {
            this.m_PacketID = packetID;
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
        public OnEncodedPacketReceive OnReceive
        {
            get
            {
                return this.m_OnReceive;
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