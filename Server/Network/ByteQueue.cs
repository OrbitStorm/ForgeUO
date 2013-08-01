/***************************************************************************
*                               ByteQueue.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: ByteQueue.cs 4 2006-06-15 04:28:39Z mark $
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
    public class ByteQueue
    {
        private int m_Head;
        private int m_Tail;
        private int m_Size;
        private byte[] m_Buffer;
        public ByteQueue()
        {
            this.m_Buffer = new byte[2048];
        }

        public int Length
        {
            get
            {
                return this.m_Size;
            }
        }
        public void Clear()
        {
            this.m_Head = 0;
            this.m_Tail = 0;
            this.m_Size = 0;
        }

        public byte GetPacketID()
        {
            if (this.m_Size >= 1)
                return this.m_Buffer[this.m_Head];

            return 0xFF;
        }

        public int GetPacketLength()
        {
            if (this.m_Size >= 3)
                return (this.m_Buffer[(this.m_Head + 1) % this.m_Buffer.Length] << 8) | this.m_Buffer[(this.m_Head + 2) % this.m_Buffer.Length];

            return 0;
        }

        public int Dequeue(byte[] buffer, int offset, int size)
        {
            if (size > this.m_Size)
                size = this.m_Size;

            if (size == 0)
                return 0;

            if (this.m_Head < this.m_Tail)
            {
                Buffer.BlockCopy(this.m_Buffer, this.m_Head, buffer, offset, size);
            }
            else
            {
                int rightLength = (this.m_Buffer.Length - this.m_Head);

                if (rightLength >= size)
                {
                    Buffer.BlockCopy(this.m_Buffer, this.m_Head, buffer, offset, size);
                }
                else
                {
                    Buffer.BlockCopy(this.m_Buffer, this.m_Head, buffer, offset, rightLength);
                    Buffer.BlockCopy(this.m_Buffer, 0, buffer, offset + rightLength, size - rightLength);
                }
            }

            this.m_Head = (this.m_Head + size) % this.m_Buffer.Length;
            this.m_Size -= size;

            if (this.m_Size == 0)
            {
                this.m_Head = 0;
                this.m_Tail = 0;
            }

            return size;
        }

        public void Enqueue(byte[] buffer, int offset, int size)
        {
            if ((this.m_Size + size) > this.m_Buffer.Length)
                this.SetCapacity((this.m_Size + size + 2047) & ~2047);

            if (this.m_Head < this.m_Tail)
            {
                int rightLength = (this.m_Buffer.Length - this.m_Tail);

                if (rightLength >= size)
                {
                    Buffer.BlockCopy(buffer, offset, this.m_Buffer, this.m_Tail, size);
                }
                else
                {
                    Buffer.BlockCopy(buffer, offset, this.m_Buffer, this.m_Tail, rightLength);
                    Buffer.BlockCopy(buffer, offset + rightLength, this.m_Buffer, 0, size - rightLength);
                }
            }
            else
            {
                Buffer.BlockCopy(buffer, offset, this.m_Buffer, this.m_Tail, size);
            }

            this.m_Tail = (this.m_Tail + size) % this.m_Buffer.Length;
            this.m_Size += size;
        }

        private void SetCapacity(int capacity) 
        {
            byte[] newBuffer = new byte[capacity];

            if (this.m_Size > 0)
            {
                if (this.m_Head < this.m_Tail)
                {
                    Buffer.BlockCopy(this.m_Buffer, this.m_Head, newBuffer, 0, this.m_Size);
                }
                else
                {
                    Buffer.BlockCopy(this.m_Buffer, this.m_Head, newBuffer, 0, this.m_Buffer.Length - this.m_Head);
                    Buffer.BlockCopy(this.m_Buffer, 0, newBuffer, this.m_Buffer.Length - this.m_Head, this.m_Tail);
                }
            }

            this.m_Head = 0;
            this.m_Tail = this.m_Size;
            this.m_Buffer = newBuffer;
        }
    }
}