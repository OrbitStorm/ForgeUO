/***************************************************************************
*                               SendQueue.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: SendQueue.cs 80 2006-08-27 20:41:31Z krrios $
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
    public class SendQueue
    {
        private static int m_CoalesceBufferSize = 512;
        private static BufferPool m_UnusedBuffers = new BufferPool("Coalesced", 2048, m_CoalesceBufferSize);
        private const int PendingCap = 96 * 1024;
        private readonly Queue<Gram> _pending;
        private Gram _buffered;
        public SendQueue()
        {
            this._pending = new Queue<Gram>();
        }

        public static int CoalesceBufferSize
        {
            get
            {
                return m_CoalesceBufferSize;
            }
            set
            {
                if (m_CoalesceBufferSize == value)
                    return;

                if (m_UnusedBuffers != null)
                    m_UnusedBuffers.Free();

                m_CoalesceBufferSize = value;
                m_UnusedBuffers = new BufferPool("Coalesced", 2048, m_CoalesceBufferSize);
            }
        }
        public bool IsFlushReady
        {
            get
            {
                return (this._pending.Count == 0 && this._buffered != null);
            }
        }
        public bool IsEmpty
        {
            get
            {
                return (this._pending.Count == 0 && this._buffered == null);
            }
        }
        public static byte[] AcquireBuffer()
        {
            return m_UnusedBuffers.AcquireBuffer();
        }

        public static void ReleaseBuffer(byte[] buffer)
        {
            if (buffer != null && buffer.Length == m_CoalesceBufferSize)
            {
                m_UnusedBuffers.ReleaseBuffer(buffer);
            }
        }

        public Gram CheckFlushReady()
        {
            Gram gram = null;

            if (this._pending.Count == 0 && this._buffered != null)
            {
                gram = this._buffered;

                this._pending.Enqueue(this._buffered);
                this._buffered = null;
            }

            return gram;
        }

        public Gram Dequeue()
        {
            Gram gram = null;

            if (this._pending.Count > 0)
            {
                this._pending.Dequeue().Release();

                if (this._pending.Count > 0)
                {
                    gram = this._pending.Peek();
                }
            }

            return gram;
        }

        public Gram Enqueue(byte[] buffer, int length)
        {
            return this.Enqueue(buffer, 0, length);
        }

        public Gram Enqueue(byte[] buffer, int offset, int length)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            else if (!(offset >= 0 && offset < buffer.Length))
            {
                throw new ArgumentOutOfRangeException("offset", offset, "Offset must be greater than or equal to zero and less than the size of the buffer.");
            }
            else if (length < 0 || length > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("length", length, "Length cannot be less than zero or greater than the size of the buffer.");
            }
            else if ((buffer.Length - offset) < length)
            {
                throw new ArgumentException("Offset and length do not point to a valid segment within the buffer.");
            }

            int existingBytes = (this._pending.Count * m_CoalesceBufferSize) + (this._buffered == null ? 0 : this._buffered.Length);

            if ((existingBytes + length) > PendingCap)
            {
                throw new CapacityExceededException();
            }

            Gram gram = null;

            while (length > 0)
            {
                if (this._buffered == null)
                { // nothing yet buffered
                    this._buffered = Gram.Acquire();
                }

                int bytesWritten = this._buffered.Write(buffer, offset, length);

                offset += bytesWritten;
                length -= bytesWritten;

                if (this._buffered.IsFull)
                {
                    if (this._pending.Count == 0)
                    {
                        gram = this._buffered;
                    }

                    this._pending.Enqueue(this._buffered);
                    this._buffered = null;
                }
            }

            return gram;
        }

        public void Clear()
        {
            if (this._buffered != null)
            {
                this._buffered.Release();
                this._buffered = null;
            }

            while (this._pending.Count > 0)
            {
                this._pending.Dequeue().Release();
            }
        }

        public class Gram
        {
            private static readonly Stack<Gram> _pool = new Stack<Gram>();
            private byte[] _buffer;
            private int _length;
            private Gram()
            {
            }

            public byte[] Buffer
            {
                get
                {
                    return this._buffer;
                }
            }
            public int Length
            {
                get
                {
                    return this._length;
                }
            }
            public int Available
            {
                get
                {
                    return (this._buffer.Length - this._length);
                }
            }
            public bool IsFull
            {
                get
                {
                    return (this._length == this._buffer.Length);
                }
            }
            public static Gram Acquire()
            {
                lock (_pool)
                {
                    Gram gram;

                    if (_pool.Count > 0)
                    {
                        gram = _pool.Pop();
                    }
                    else
                    {
                        gram = new Gram();
                    }

                    gram._buffer = AcquireBuffer();
                    gram._length = 0;

                    return gram;
                }
            }

            public int Write(byte[] buffer, int offset, int length)
            {
                int write = Math.Min(length, this.Available);

                System.Buffer.BlockCopy(buffer, offset, this._buffer, this._length, write);

                this._length += write;

                return write;
            }

            public void Release()
            {
                lock (_pool)
                {
                    _pool.Push(this);
                    ReleaseBuffer(this._buffer);
                }
            }
        }
    }

    public sealed class CapacityExceededException : Exception
    {
        public CapacityExceededException()
            : base("Too much data pending.")
        {
        }
    }
}