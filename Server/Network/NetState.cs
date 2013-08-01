using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Server.Accounting;
using Server.Diagnostics;
using Server.Gumps;
using Server.HuePickers;
using Server.Items;
using Server.Menus;

namespace Server.Network
{
    public interface IPacketEncoder
    {
        void EncodeOutgoingPacket(NetState to, ref byte[] buffer, ref int length);

        void DecodeIncomingPacket(NetState from, ref byte[] buffer, ref int length);
    }

    public delegate void NetStateCreatedCallback(NetState ns);

    public class NetState : IComparable<NetState>
    {
        private Socket m_Socket;
        private readonly IPAddress m_Address;
        private ByteQueue m_Buffer;
        private byte[] m_RecvBuffer;
        private readonly SendQueue m_SendQueue;
        private bool m_Seeded;
        private bool m_Running;

        #if NewAsyncSockets
		private SocketAsyncEventArgs m_ReceiveEventArgs, m_SendEventArgs;
        #else
        private AsyncCallback m_OnReceive, m_OnSend;
        #endif

        private readonly MessagePump m_MessagePump;
        private ServerInfo[] m_ServerInfo;
        private IAccount m_Account;
        private Mobile m_Mobile;
        private CityInfo[] m_CityInfo;
        private List<Gump> m_Gumps;
        private List<HuePicker> m_HuePickers;
        private List<IMenu> m_Menus;
        private readonly List<SecureTrade> m_Trades;
        private int m_Sequence;
        private bool m_CompressionEnabled;
        private readonly string m_ToString;
        private ClientVersion m_Version;
        private bool m_SentFirstPacket;
        private bool m_BlockAllPackets;

        private readonly DateTime m_ConnectedOn;

        public DateTime ConnectedOn
        {
            get
            {
                return this.m_ConnectedOn;
            }
        }

        public TimeSpan ConnectedFor
        {
            get
            {
                return (DateTime.Now - this.m_ConnectedOn);
            }
        }

        internal int m_Seed;
        internal int m_AuthID;

        public IPAddress Address
        {
            get
            {
                return this.m_Address;
            }
        }

        private ClientFlags m_Flags;

        private static bool m_Paused;

        [Flags]
        private enum AsyncState
        {
            Pending = 0x01,
            Paused = 0x02
        }

        private AsyncState m_AsyncState;
        private readonly object m_AsyncLock = new object();

        private IPacketEncoder m_Encoder = null;

        public IPacketEncoder PacketEncoder
        {
            get
            {
                return this.m_Encoder;
            }
            set
            {
                this.m_Encoder = value;
            }
        }

        private static NetStateCreatedCallback m_CreatedCallback;

        public static NetStateCreatedCallback CreatedCallback
        {
            get
            {
                return m_CreatedCallback;
            }
            set
            {
                m_CreatedCallback = value;
            }
        }

        public bool SentFirstPacket
        {
            get
            {
                return this.m_SentFirstPacket;
            }
            set
            {
                this.m_SentFirstPacket = value;
            }
        }

        public bool BlockAllPackets
        {
            get
            {
                return this.m_BlockAllPackets;
            }
            set
            {
                this.m_BlockAllPackets = value;
            }
        }

        public ClientFlags Flags
        {
            get
            {
                return this.m_Flags;
            }
            set
            {
                this.m_Flags = value;
            }
        }

        public ClientVersion Version
        {
            get
            {
                return this.m_Version;
            }
            set
            {
                this.m_Version = value;

                if (value >= m_Version70160)
                {
                    this._ProtocolChanges = ProtocolChanges.Version70160;
                }
                else if (value >= m_Version70130)
                {
                    this._ProtocolChanges = ProtocolChanges.Version70130;
                }
                else if (value >= m_Version7090)
                {
                    this._ProtocolChanges = ProtocolChanges.Version7090;
                }
                else if (value >= m_Version7000)
                {
                    this._ProtocolChanges = ProtocolChanges.Version7000;
                }
                else if (value >= m_Version60142)
                {
                    this._ProtocolChanges = ProtocolChanges.Version60142;
                }
                else if (value >= m_Version6017)
                {
                    this._ProtocolChanges = ProtocolChanges.Version6017;
                }
                else if (value >= m_Version6000)
                {
                    this._ProtocolChanges = ProtocolChanges.Version6000;
                }
                else if (value >= m_Version502b)
                {
                    this._ProtocolChanges = ProtocolChanges.Version502b;
                }
                else if (value >= m_Version500a)
                {
                    this._ProtocolChanges = ProtocolChanges.Version500a;
                }
                else if (value >= m_Version407a)
                {
                    this._ProtocolChanges = ProtocolChanges.Version407a;
                }
                else if (value >= m_Version400a)
                {
                    this._ProtocolChanges = ProtocolChanges.Version400a;
                }
            }
        }

        private static readonly ClientVersion m_Version400a = new ClientVersion("4.0.0a");
        private static readonly ClientVersion m_Version407a = new ClientVersion("4.0.7a");
        private static readonly ClientVersion m_Version500a = new ClientVersion("5.0.0a");
        private static readonly ClientVersion m_Version502b = new ClientVersion("5.0.2b");
        private static readonly ClientVersion m_Version6000 = new ClientVersion("6.0.0.0");
        private static readonly ClientVersion m_Version6017 = new ClientVersion("6.0.1.7");
        private static readonly ClientVersion m_Version60142 = new ClientVersion("6.0.14.2");
        private static readonly ClientVersion m_Version7000 = new ClientVersion("7.0.0.0");
        private static readonly ClientVersion m_Version7090 = new ClientVersion("7.0.9.0");
        private static readonly ClientVersion m_Version70130 = new ClientVersion("7.0.13.0");
        private static readonly ClientVersion m_Version70160 = new ClientVersion("7.0.16.0");

        private ProtocolChanges _ProtocolChanges;

        private enum ProtocolChanges
        {
            NewSpellbook = 0x00000001,
            DamagePacket = 0x00000002,
            Unpack = 0x00000004,
            BuffIcon = 0x00000008,
            NewHaven = 0x00000010,
            ContainerGridLines = 0x00000020,
            ExtendedSupportedFeatures = 0x00000040,
            StygianAbyss = 0x00000080,
            HighSeas = 0x00000100,
            NewCharacterList = 0x00000200,
            NewCharacterCreation = 0x00000400,

            Version400a = NewSpellbook,
            Version407a = Version400a | DamagePacket,
            Version500a = Version407a | Unpack,
            Version502b = Version500a | BuffIcon,
            Version6000 = Version502b | NewHaven,
            Version6017 = Version6000 | ContainerGridLines,
            Version60142 = Version6017 | ExtendedSupportedFeatures,
            Version7000 = Version60142 | StygianAbyss,
            Version7090 = Version7000 | HighSeas,
            Version70130 = Version7090 | NewCharacterList,
            Version70160 = Version70130 | NewCharacterCreation
        }

        public bool NewSpellbook
        {
            get
            {
                return ((this._ProtocolChanges & ProtocolChanges.NewSpellbook) != 0);
            }
        }
        public bool DamagePacket
        {
            get
            {
                return ((this._ProtocolChanges & ProtocolChanges.DamagePacket) != 0);
            }
        }
        public bool Unpack
        {
            get
            {
                return ((this._ProtocolChanges & ProtocolChanges.Unpack) != 0);
            }
        }
        public bool BuffIcon
        {
            get
            {
                return ((this._ProtocolChanges & ProtocolChanges.BuffIcon) != 0);
            }
        }
        public bool NewHaven
        {
            get
            {
                return ((this._ProtocolChanges & ProtocolChanges.NewHaven) != 0);
            }
        }
        public bool ContainerGridLines
        {
            get
            {
                return ((this._ProtocolChanges & ProtocolChanges.ContainerGridLines) != 0);
            }
        }
        public bool ExtendedSupportedFeatures
        {
            get
            {
                return ((this._ProtocolChanges & ProtocolChanges.ExtendedSupportedFeatures) != 0);
            }
        }
        public bool StygianAbyss
        {
            get
            {
                return ((this._ProtocolChanges & ProtocolChanges.StygianAbyss) != 0);
            }
        }
        public bool HighSeas
        {
            get
            {
                return ((this._ProtocolChanges & ProtocolChanges.HighSeas) != 0);
            }
        }
        public bool NewCharacterList
        {
            get
            {
                return ((this._ProtocolChanges & ProtocolChanges.NewCharacterList) != 0);
            }
        }
        public bool NewCharacterCreation
        {
            get
            {
                return ((this._ProtocolChanges & ProtocolChanges.NewCharacterCreation) != 0);
            }
        }

        public bool IsUOTDClient
        {
            get
            {
                return ((this.m_Flags & ClientFlags.UOTD) != 0 || (this.m_Version != null && this.m_Version.Type == ClientType.UOTD));
            }
        }

        public bool IsSAClient
        {
            get
            {
                return (this.m_Version != null && this.m_Version.Type == ClientType.SA);
            }
        }

        public List<SecureTrade> Trades
        {
            get
            {
                return this.m_Trades;
            }
        }

        public void ValidateAllTrades()
        {
            for (int i = this.m_Trades.Count - 1; i >= 0; --i)
            {
                if (i >= this.m_Trades.Count)
                {
                    continue;
                }

                SecureTrade trade = this.m_Trades[i];

                if (trade.From.Mobile.Deleted || trade.To.Mobile.Deleted || !trade.From.Mobile.Alive || !trade.To.Mobile.Alive || !trade.From.Mobile.InRange(trade.To.Mobile, 2) || trade.From.Mobile.Map != trade.To.Mobile.Map)
                {
                    trade.Cancel();
                }
            }
        }

        public void CancelAllTrades()
        {
            for (int i = this.m_Trades.Count - 1; i >= 0; --i)
            {
                if (i < this.m_Trades.Count)
                {
                    this.m_Trades[i].Cancel();
                }
            }
        }

        public void RemoveTrade(SecureTrade trade)
        {
            this.m_Trades.Remove(trade);
        }

        public SecureTrade FindTrade(Mobile m)
        {
            for (int i = 0; i < this.m_Trades.Count; ++i)
            {
                SecureTrade trade = this.m_Trades[i];

                if (trade.From.Mobile == m || trade.To.Mobile == m)
                {
                    return trade;
                }
            }

            return null;
        }

        public SecureTradeContainer FindTradeContainer(Mobile m)
        {
            for (int i = 0; i < this.m_Trades.Count; ++i)
            {
                SecureTrade trade = this.m_Trades[i];

                SecureTradeInfo from = trade.From;
                SecureTradeInfo to = trade.To;

                if (from.Mobile == this.m_Mobile && to.Mobile == m)
                {
                    return from.Container;
                }
                else if (from.Mobile == m && to.Mobile == this.m_Mobile)
                {
                    return to.Container;
                }
            }

            return null;
        }

        public SecureTradeContainer AddTrade(NetState state)
        {
            SecureTrade newTrade = new SecureTrade(this.m_Mobile, state.m_Mobile);

            this.m_Trades.Add(newTrade);
            state.m_Trades.Add(newTrade);

            return newTrade.From.Container;
        }

        public bool CompressionEnabled
        {
            get
            {
                return this.m_CompressionEnabled;
            }
            set
            {
                this.m_CompressionEnabled = value;
            }
        }

        public int Sequence
        {
            get
            {
                return this.m_Sequence;
            }
            set
            {
                this.m_Sequence = value;
            }
        }

        public IEnumerable<Gump> Gumps
        {
            get
            {
                return this.m_Gumps;
            }
        }

        public IEnumerable<HuePicker> HuePickers
        {
            get
            {
                return this.m_HuePickers;
            }
        }

        public IEnumerable<IMenu> Menus
        {
            get
            {
                return this.m_Menus;
            }
        }

        private static int m_GumpCap = 512, m_HuePickerCap = 512, m_MenuCap = 512;

        public static int GumpCap
        {
            get
            {
                return m_GumpCap;
            }
            set
            {
                m_GumpCap = value;
            }
        }

        public static int HuePickerCap
        {
            get
            {
                return m_HuePickerCap;
            }
            set
            {
                m_HuePickerCap = value;
            }
        }

        public static int MenuCap
        {
            get
            {
                return m_MenuCap;
            }
            set
            {
                m_MenuCap = value;
            }
        }

        public void WriteConsole(string text)
        {
            Console.WriteLine("Client: {0}: {1}", this, text);
        }

        public void WriteConsole(string format, params object[] args)
        {
            this.WriteConsole(String.Format(format, args));
        }

        public void AddMenu(IMenu menu)
        {
            if (this.m_Menus == null)
            {
                this.m_Menus = new List<IMenu>();
            }

            if (this.m_Menus.Count < m_MenuCap)
            {
                this.m_Menus.Add(menu);
            }
            else
            {
                Utility.PushColor(ConsoleColor.DarkRed);
                this.WriteConsole("Exceeded menu cap, disconnecting...");
                Utility.PopColor();
                this.Dispose();
            }
        }

        public void RemoveMenu(IMenu menu)
        {
            if (this.m_Menus != null)
            {
                this.m_Menus.Remove(menu);
            }
        }

        public void RemoveMenu(int index)
        {
            if (this.m_Menus != null)
            {
                this.m_Menus.RemoveAt(index);
            }
        }

        public void ClearMenus()
        {
            if (this.m_Menus != null)
            {
                this.m_Menus.Clear();
            }
        }

        public void AddHuePicker(HuePicker huePicker)
        {
            if (this.m_HuePickers == null)
            {
                this.m_HuePickers = new List<HuePicker>();
            }

            if (this.m_HuePickers.Count < m_HuePickerCap)
            {
                this.m_HuePickers.Add(huePicker);
            }
            else
            {
                Utility.PushColor(ConsoleColor.DarkRed);
                this.WriteConsole("Exceeded hue picker cap, disconnecting...");
                Utility.PopColor();
                this.Dispose();
            }
        }

        public void RemoveHuePicker(HuePicker huePicker)
        {
            if (this.m_HuePickers != null)
            {
                this.m_HuePickers.Remove(huePicker);
            }
        }

        public void RemoveHuePicker(int index)
        {
            if (this.m_HuePickers != null)
            {
                this.m_HuePickers.RemoveAt(index);
            }
        }

        public void ClearHuePickers()
        {
            if (this.m_HuePickers != null)
            {
                this.m_HuePickers.Clear();
            }
        }

        public void AddGump(Gump gump)
        {
            if (this.m_Gumps == null)
            {
                this.m_Gumps = new List<Gump>();
            }

            if (this.m_Gumps.Count < m_GumpCap)
            {
                this.m_Gumps.Add(gump);
            }
            else
            {
                Utility.PushColor(ConsoleColor.DarkRed);
                this.WriteConsole("Exceeded gump cap, disconnecting...");
                Utility.PopColor();
                this.Dispose();
            }
        }

        public void RemoveGump(Gump gump)
        {
            if (this.m_Gumps != null)
            {
                this.m_Gumps.Remove(gump);
            }
        }

        public void RemoveGump(int index)
        {
            if (this.m_Gumps != null)
            {
                this.m_Gumps.RemoveAt(index);
            }
        }

        public void ClearGumps()
        {
            if (this.m_Gumps != null)
            {
                this.m_Gumps.Clear();
            }
        }

        public void LaunchBrowser(string url)
        {
            this.Send(new MessageLocalized(Serial.MinusOne, -1, MessageType.Label, 0x35, 3, 501231, "", ""));
            this.Send(new LaunchBrowser(url));
        }

        public CityInfo[] CityInfo
        {
            get
            {
                return this.m_CityInfo;
            }
            set
            {
                this.m_CityInfo = value;
            }
        }

        public Mobile Mobile
        {
            get
            {
                return this.m_Mobile;
            }
            set
            {
                this.m_Mobile = value;
            }
        }

        public ServerInfo[] ServerInfo
        {
            get
            {
                return this.m_ServerInfo;
            }
            set
            {
                this.m_ServerInfo = value;
            }
        }

        public IAccount Account
        {
            get
            {
                return this.m_Account;
            }
            set
            {
                this.m_Account = value;
            }
        }

        public override string ToString()
        {
            return this.m_ToString;
        }

        private static readonly List<NetState> m_Instances = new List<NetState>();

        public static List<NetState> Instances
        {
            get
            {
                return m_Instances;
            }
        }

        private static readonly BufferPool m_ReceiveBufferPool = new BufferPool("Receive", 2048, 2048);

        public NetState(Socket socket, MessagePump messagePump)
        {
            this.m_Socket = socket;
            this.m_Buffer = new ByteQueue();
            this.m_Seeded = false;
            this.m_Running = false;
            this.m_RecvBuffer = m_ReceiveBufferPool.AcquireBuffer();
            this.m_MessagePump = messagePump;
            this.m_Gumps = new List<Gump>();
            this.m_HuePickers = new List<HuePicker>();
            this.m_Menus = new List<IMenu>();
            this.m_Trades = new List<SecureTrade>();

            this.m_SendQueue = new SendQueue();

            this.m_NextCheckActivity = DateTime.Now + TimeSpan.FromMinutes(0.5);

            m_Instances.Add(this);

            try
            {
                this.m_Address = Utility.Intern(((IPEndPoint)this.m_Socket.RemoteEndPoint).Address);
                this.m_ToString = this.m_Address.ToString();
            }
            catch (Exception ex)
            {
                TraceException(ex);
                this.m_Address = IPAddress.None;
                this.m_ToString = "(error)";
            }

            this.m_ConnectedOn = DateTime.Now;

            if (m_CreatedCallback != null)
            {
                m_CreatedCallback(this);
            }
        }

        public virtual void Send(Packet p)
        {
            if (this.m_Socket == null || this.m_BlockAllPackets)
            {
                p.OnSend();
                return;
            }

            PacketSendProfile prof = PacketSendProfile.Acquire(p.GetType());

            int length;
            byte[] buffer = p.Compile(this.m_CompressionEnabled, out length);

            if (buffer != null)
            {
                if (buffer.Length <= 0 || length <= 0)
                {
                    p.OnSend();
                    return;
                }

                if (prof != null)
                {
                    prof.Start();
                }

                if (this.m_Encoder != null)
                {
                    this.m_Encoder.EncodeOutgoingPacket(this, ref buffer, ref length);
                }

                try
                {
                    SendQueue.Gram gram;

                    lock (this.m_SendQueue)
                    {
                        gram = this.m_SendQueue.Enqueue(buffer, length);
                    }

                    if (gram != null)
                    {
                        #if NewAsyncSockets
						m_SendEventArgs.SetBuffer( gram.Buffer, 0, gram.Length );
						Send_Start();
                        #else
                        try
                        {
                            this.m_Socket.BeginSend(gram.Buffer, 0, gram.Length, SocketFlags.None, this.m_OnSend, this.m_Socket);
                        }
                        catch (Exception ex)
                        {
                            TraceException(ex);
                            this.Dispose(false);
                        }
                        #endif
                    }
                }
                catch (CapacityExceededException)
                {
                    Utility.PushColor(ConsoleColor.DarkRed);
                    Console.WriteLine("Client: {0}: Too much data pending, disconnecting...", this);
                    Utility.PopColor();
                    this.Dispose(false);
                }

                p.OnSend();

                if (prof != null)
                {
                    prof.Finish(length);
                }
            }
            else
            {
                Utility.PushColor(ConsoleColor.DarkRed);
                Console.WriteLine("Client: {0}: null buffer send, disconnecting...", this);
                Utility.PopColor();

                using (StreamWriter op = new StreamWriter("null_send.log", true))
                {
                    op.WriteLine("{0} Client: {1}: null buffer send, disconnecting...", DateTime.Now, this);
                    op.WriteLine(new System.Diagnostics.StackTrace());
                }
                this.Dispose();
            }
        }

        #if NewAsyncSockets
		public void Start() {
			m_ReceiveEventArgs = new SocketAsyncEventArgs();
			m_ReceiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>( Receive_Completion );
			m_ReceiveEventArgs.SetBuffer( m_RecvBuffer, 0, m_RecvBuffer.Length );

			m_SendEventArgs = new SocketAsyncEventArgs();
			m_SendEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>( Send_Completion );

			m_Running = true;

			if ( m_Socket == null || m_Paused ) {
				return;
			}

			Receive_Start();
		}

		private void Receive_Start()
		{
			try {
				bool result = false;

				do {
					lock ( m_AsyncLock ) {
						if ( ( m_AsyncState & ( AsyncState.Pending | AsyncState.Paused ) ) == 0 ) {
							m_AsyncState |= AsyncState.Pending;
							result = !m_Socket.ReceiveAsync( m_ReceiveEventArgs );

							if ( result )
								Receive_Process( m_ReceiveEventArgs );
						}
					}
				} while ( result );
			} catch ( Exception ex ) {
				TraceException( ex );
				Dispose( false );
			}
		}

		private void Receive_Completion( object sender, SocketAsyncEventArgs e )
		{
			Receive_Process( e );

			if ( !m_Disposing )
				Receive_Start();
		}

		private void Receive_Process( SocketAsyncEventArgs e )
		{
			int byteCount = e.BytesTransferred;

			if ( e.SocketError != SocketError.Success || byteCount <= 0 ) {
				Dispose( false );
				return;
			}

			m_NextCheckActivity = DateTime.Now + TimeSpan.FromMinutes( 1.2 );

			byte[] buffer = m_RecvBuffer;

			if ( m_Encoder != null )
				m_Encoder.DecodeIncomingPacket( this, ref buffer, ref byteCount );

			lock ( m_Buffer )
				m_Buffer.Enqueue( buffer, 0, byteCount );

			m_MessagePump.OnReceive( this );

			lock ( m_AsyncLock ) {
				m_AsyncState &= ~AsyncState.Pending;
			}
		}

		private void Send_Start()
		{
			try {
				bool result = false;

				do {
					result = !m_Socket.SendAsync( m_SendEventArgs );

					if ( result )
						Send_Process( m_SendEventArgs );
				} while ( result ); 
			} catch ( Exception ex ) {
				TraceException( ex );
				Dispose( false );
			}
		}

		private void Send_Completion( object sender, SocketAsyncEventArgs e )
		{
			Send_Process( e );

			if ( m_Disposing )
				return;

			if ( m_CoalesceSleep >= 0 ) {
				Thread.Sleep( m_CoalesceSleep );
			}

			SendQueue.Gram gram;

			lock ( m_SendQueue ) {
				gram = m_SendQueue.Dequeue();
			}

			if ( gram != null ) {
				m_SendEventArgs.SetBuffer( gram.Buffer, 0, gram.Length );
				Send_Start();
			}
		}

		private void Send_Process( SocketAsyncEventArgs e )
		{
			int bytes = e.BytesTransferred;

			if ( e.SocketError != SocketError.Success || bytes <= 0 ) {
				Dispose( false );
				return;
			}

			m_NextCheckActivity = DateTime.Now + TimeSpan.FromMinutes( 1.2 );
		}

		public static void Pause() {
			m_Paused = true;

			for ( int i = 0; i < m_Instances.Count; ++i ) {
				NetState ns = m_Instances[i];

				lock ( ns.m_AsyncLock ) {
					ns.m_AsyncState |= AsyncState.Paused;
				}
			}
		}

		public static void Resume() {
			m_Paused = false;

			for ( int i = 0; i < m_Instances.Count; ++i ) {
				NetState ns = m_Instances[i];

				if ( ns.m_Socket == null ) {
					continue;
				}

				lock ( ns.m_AsyncLock ) {
					ns.m_AsyncState &= ~AsyncState.Paused;

					if ( ( ns.m_AsyncState & AsyncState.Pending ) == 0 )
						ns.Receive_Start();
				}
			}
		}

		public bool Flush() {
			if ( m_Socket == null || !m_SendQueue.IsFlushReady ) {
				return false;
			}

			SendQueue.Gram gram;

			lock ( m_SendQueue ) {
				gram = m_SendQueue.CheckFlushReady();
			}

			if ( gram != null ) {
				m_SendEventArgs.SetBuffer( gram.Buffer, 0, gram.Length );
				Send_Start();
			}

			return false;
		}

        #else

        public void Start()
        {
            this.m_OnReceive = new AsyncCallback(OnReceive);
            this.m_OnSend = new AsyncCallback(OnSend);

            this.m_Running = true;

            if (this.m_Socket == null || m_Paused)
            {
                return;
            }

            try
            {
                lock (this.m_AsyncLock)
                {
                    if ((this.m_AsyncState & (AsyncState.Pending | AsyncState.Paused)) == 0)
                    {
                        this.InternalBeginReceive();
                    }
                }
            }
            catch (Exception ex)
            {
                TraceException(ex);
                this.Dispose(false);
            }
        }

        private void InternalBeginReceive()
        {
            this.m_AsyncState |= AsyncState.Pending;

            this.m_Socket.BeginReceive(this.m_RecvBuffer, 0, this.m_RecvBuffer.Length, SocketFlags.None, this.m_OnReceive, this.m_Socket);
        }

        private void OnReceive(IAsyncResult asyncResult)
        {
            Socket s = (Socket)asyncResult.AsyncState;

            try
            {
                int byteCount = s.EndReceive(asyncResult);

                if (byteCount > 0)
                {
                    this.m_NextCheckActivity = DateTime.Now + TimeSpan.FromMinutes(1.2);

                    byte[] buffer = this.m_RecvBuffer;

                    if (this.m_Encoder != null)
                        this.m_Encoder.DecodeIncomingPacket(this, ref buffer, ref byteCount);

                    lock (this.m_Buffer)
                        this.m_Buffer.Enqueue(buffer, 0, byteCount);

                    this.m_MessagePump.OnReceive(this);

                    lock (this.m_AsyncLock)
                    {
                        this.m_AsyncState &= ~AsyncState.Pending;

                        if ((this.m_AsyncState & AsyncState.Paused) == 0)
                        {
                            try
                            {
                                this.InternalBeginReceive();
                            }
                            catch (Exception ex)
                            {
                                TraceException(ex);
                                this.Dispose(false);
                            }
                        }
                    }
                }
                else
                {
                    this.Dispose(false);
                }
            }
            catch
            {
                this.Dispose(false);
            }
        }

        private void OnSend(IAsyncResult asyncResult)
        {
            Socket s = (Socket)asyncResult.AsyncState;

            try
            {
                int bytes = s.EndSend(asyncResult);

                if (bytes <= 0)
                {
                    this.Dispose(false);
                    return;
                }

                this.m_NextCheckActivity = DateTime.Now + TimeSpan.FromMinutes(1.2);

                if (m_CoalesceSleep >= 0)
                {
                    Thread.Sleep(m_CoalesceSleep);
                }

                SendQueue.Gram gram;

                lock (this.m_SendQueue)
                {
                    gram = this.m_SendQueue.Dequeue();
                }

                if (gram != null)
                {
                    try
                    {
                        s.BeginSend(gram.Buffer, 0, gram.Length, SocketFlags.None, this.m_OnSend, s);
                    }
                    catch (Exception ex)
                    {
                        TraceException(ex);
                        this.Dispose(false);
                    }
                }
            }
            catch (Exception)
            {
                this.Dispose(false);
            }
        }

        public static void Pause()
        {
            m_Paused = true;

            for (int i = 0; i < m_Instances.Count; ++i)
            {
                NetState ns = m_Instances[i];

                lock (ns.m_AsyncLock)
                {
                    ns.m_AsyncState |= AsyncState.Paused;
                }
            }
        }

        public static void Resume()
        {
            m_Paused = false;

            for (int i = 0; i < m_Instances.Count; ++i)
            {
                NetState ns = m_Instances[i];

                if (ns.m_Socket == null)
                {
                    continue;
                }

                lock (ns.m_AsyncLock)
                {
                    ns.m_AsyncState &= ~AsyncState.Paused;

                    try
                    {
                        if ((ns.m_AsyncState & AsyncState.Pending) == 0)
                            ns.InternalBeginReceive();
                    }
                    catch (Exception ex)
                    {
                        TraceException(ex);
                        ns.Dispose(false);
                    }
                }
            }
        }

        public bool Flush()
        {
            if (this.m_Socket == null || !this.m_SendQueue.IsFlushReady)
            {
                return false;
            }

            SendQueue.Gram gram;

            lock (this.m_SendQueue)
            {
                gram = this.m_SendQueue.CheckFlushReady();
            }

            if (gram != null)
            {
                try
                {
                    this.m_Socket.BeginSend(gram.Buffer, 0, gram.Length, SocketFlags.None, this.m_OnSend, this.m_Socket);
                    return true;
                }
                catch (Exception ex)
                {
                    TraceException(ex);
                    this.Dispose(false);
                }
            }

            return false;
        }

        #endif

        public PacketHandler GetHandler(int packetID)
        {
            if (this.ContainerGridLines)
                return PacketHandlers.Get6017Handler(packetID);
            else
                return PacketHandlers.GetHandler(packetID);
        }

        public static void FlushAll()
        {
            for (int i = 0; i < m_Instances.Count; ++i)
            {
                NetState ns = m_Instances[i];

                ns.Flush();
            }
        }

        private static int m_CoalesceSleep = -1;

        public static int CoalesceSleep
        {
            get
            {
                return m_CoalesceSleep;
            }
            set
            {
                m_CoalesceSleep = value;
            }
        }

        private DateTime m_NextCheckActivity;

        public bool CheckAlive()
        {
            if (this.m_Socket == null)
                return false;

            if (DateTime.Now < this.m_NextCheckActivity)
            {
                return true;
            }

            Utility.PushColor(ConsoleColor.DarkRed);
            Console.WriteLine("Client: {0}: Disconnecting due to inactivity...", this);
            Utility.PopColor();

            this.Dispose();
            return false;
        }

        public static void TraceException(Exception ex)
        {
            try
            {
                using (StreamWriter op = new StreamWriter("network-errors.log", true))
                {
                    op.WriteLine("# {0}", DateTime.Now);

                    op.WriteLine(ex);

                    op.WriteLine();
                    op.WriteLine();
                }
            }
            catch
            {
            }

            try
            {
                Console.WriteLine(ex);
            }
            catch
            {
            }
        }

        private bool m_Disposing;

        public void Dispose()
        {
            this.Dispose(true);
        }

        public virtual void Dispose(bool flush)
        {
            if (this.m_Socket == null || this.m_Disposing)
            {
                return;
            }

            this.m_Disposing = true;

            if (flush)
                flush = this.Flush();

            try
            {
                this.m_Socket.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException ex)
            {
                TraceException(ex);
            }

            try
            {
                this.m_Socket.Close();
            }
            catch (SocketException ex)
            {
                TraceException(ex);
            }

            if (this.m_RecvBuffer != null)
                m_ReceiveBufferPool.ReleaseBuffer(this.m_RecvBuffer);

            this.m_Socket = null;

            this.m_Buffer = null;
            this.m_RecvBuffer = null;

            #if NewAsyncSockets
			m_ReceiveEventArgs = null;
			m_SendEventArgs = null;
            #else
            this.m_OnReceive = null;
            this.m_OnSend = null;
            #endif

            this.m_Running = false;

            m_Disposed.Enqueue(this);

            if (/*!flush &&*/ !this.m_SendQueue.IsEmpty)
            {
                lock (this.m_SendQueue)
                    this.m_SendQueue.Clear();
            }
        }

        public static void Initialize()
        {
            Timer.DelayCall(TimeSpan.FromMinutes(1.0), TimeSpan.FromMinutes(1.5), new TimerCallback(CheckAllAlive));
        }

        public static void CheckAllAlive()
        {
            try
            {
                for (int i = 0; i < m_Instances.Count; ++i)
                {
                    m_Instances[i].CheckAlive();
                }
            }
            catch (Exception ex)
            {
                TraceException(ex);
            }
        }

        private static readonly Queue m_Disposed = Queue.Synchronized(new Queue());

        public static void ProcessDisposedQueue()
        {
            int breakout = 0;

            while (breakout < 200 && m_Disposed.Count > 0)
            {
                ++breakout;

                NetState ns = (NetState)m_Disposed.Dequeue();

                Mobile m = ns.m_Mobile;
                IAccount a = ns.m_Account;

                if (m != null)
                {
                    m.NetState = null;
                    ns.m_Mobile = null;
                }

                ns.m_Gumps.Clear();
                ns.m_Menus.Clear();
                ns.m_HuePickers.Clear();
                ns.m_Account = null;
                ns.m_ServerInfo = null;
                ns.m_CityInfo = null;

                m_Instances.Remove(ns);

                if (a != null)
                {
                    Utility.PushColor(ConsoleColor.DarkRed);
                    ns.WriteConsole("Disconnected. [{0} Online] [{1}]", m_Instances.Count, a);
                    Utility.PopColor();
                }
                else
                {
                    Utility.PushColor(ConsoleColor.DarkRed);
                    ns.WriteConsole("Disconnected. [{0} Online]", m_Instances.Count);
                    Utility.PopColor();
                }
            }
        }

        public bool Running
        {
            get
            {
                return this.m_Running;
            }
        }

        public bool Seeded
        {
            get
            {
                return this.m_Seeded;
            }
            set
            {
                this.m_Seeded = value;
            }
        }

        public Socket Socket
        {
            get
            {
                return this.m_Socket;
            }
        }

        public ByteQueue Buffer
        {
            get
            {
                return this.m_Buffer;
            }
        }

        public ExpansionInfo ExpansionInfo
        {
            get
            {
                for (int i = ExpansionInfo.Table.Length - 1; i >= 0; i--)
                {
                    ExpansionInfo info = ExpansionInfo.Table[i];

                    if ((info.RequiredClient != null && this.Version >= info.RequiredClient) || ((this.Flags & info.ClientFlags) != 0))
                    {
                        return info;
                    }
                }

                return ExpansionInfo.GetInfo(Expansion.None);
            }
        }

        public Expansion Expansion
        {
            get
            {
                return (Expansion)this.ExpansionInfo.ID;
            }
        }

        public bool SupportsExpansion(ExpansionInfo info, bool checkCoreExpansion)
        {
            if (info == null || (checkCoreExpansion && (int)Core.Expansion < info.ID))
                return false;

            if (info.RequiredClient != null)
                return (this.Version >= info.RequiredClient);

            return ((this.Flags & info.ClientFlags) != 0);
        }

        public bool SupportsExpansion(Expansion ex, bool checkCoreExpansion)
        {
            return this.SupportsExpansion(ExpansionInfo.GetInfo(ex), checkCoreExpansion);
        }

        public bool SupportsExpansion(Expansion ex)
        {
            return this.SupportsExpansion(ex, true);
        }

        public bool SupportsExpansion(ExpansionInfo info)
        {
            return this.SupportsExpansion(info, true);
        }

        public int CompareTo(NetState other)
        {
            if (other == null)
                return 1;

            return this.m_ToString.CompareTo(other.m_ToString);
        }
    }
}