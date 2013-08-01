/***************************************************************************
*                               BufferPool.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: BufferPool.cs 4 2006-06-15 04:28:39Z mark $
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
using System.Collections.Generic;

namespace Server.Network
{
    public class BufferPool
    {
        private static List<BufferPool> m_Pools = new List<BufferPool>();
        private readonly string m_Name;
        private readonly int m_InitialCapacity;
        private readonly int m_BufferSize;
        private readonly Queue<byte[]> m_FreeBuffers;
        private int m_Misses;
        public BufferPool(string name, int initialCapacity, int bufferSize)
        {
            this.m_Name = name;

            this.m_InitialCapacity = initialCapacity;
            this.m_BufferSize = bufferSize;

            this.m_FreeBuffers = new Queue<byte[]>(initialCapacity);

            for (int i = 0; i < initialCapacity; ++i)
                this.m_FreeBuffers.Enqueue(new byte[bufferSize]);

            lock (m_Pools)
                m_Pools.Add(this);
        }

        public static List<BufferPool> Pools
        {
            get
            {
                return m_Pools;
            }
            set
            {
                m_Pools = value;
            }
        }
        public void GetInfo(out string name, out int freeCount, out int initialCapacity, out int currentCapacity, out int bufferSize, out int misses)
        {
            lock (this)
            {
                name = this.m_Name;
                freeCount = this.m_FreeBuffers.Count;
                initialCapacity = this.m_InitialCapacity;
                currentCapacity = this.m_InitialCapacity * (1 + this.m_Misses);
                bufferSize = this.m_BufferSize;
                misses = this.m_Misses;
            }
        }

        public byte[] AcquireBuffer()
        {
            lock (this)
            {
                if (this.m_FreeBuffers.Count > 0)
                    return this.m_FreeBuffers.Dequeue();

                ++this.m_Misses;

                for (int i = 0; i < this.m_InitialCapacity; ++i)
                    this.m_FreeBuffers.Enqueue(new byte[this.m_BufferSize]);

                return this.m_FreeBuffers.Dequeue();
            }
        }

        public void ReleaseBuffer(byte[] buffer)
        {
            if (buffer == null)
                return;

            lock (this)
                this.m_FreeBuffers.Enqueue(buffer);
        }

        public void Free()
        {
            lock (m_Pools)
                m_Pools.Remove(this);
        }
    }
}