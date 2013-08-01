using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace Server.Network
{
    public class Listener : IDisposable
    {
        private Socket m_Listener;

        private readonly Queue<Socket> m_Accepted;
        private readonly object m_AcceptedSyncRoot;

        #if NewAsyncSockets
		private SocketAsyncEventArgs m_EventArgs;
        #else
        private readonly AsyncCallback m_OnAccept;
        #endif

        private static readonly Socket[] m_EmptySockets = new Socket[0];

        private static IPEndPoint[] m_EndPoints;

        public static IPEndPoint[] EndPoints
        {
            get
            {
                return m_EndPoints;
            }
            set
            {
                m_EndPoints = value;
            }
        }

        public Listener(IPEndPoint ipep)
        {
            this.m_Accepted = new Queue<Socket>();
            this.m_AcceptedSyncRoot = ((ICollection)this.m_Accepted).SyncRoot;

            this.m_Listener = this.Bind(ipep);

            if (this.m_Listener == null)
                return;

            this.DisplayListener();

            #if NewAsyncSockets
			m_EventArgs = new SocketAsyncEventArgs();
			m_EventArgs.Completed += new EventHandler<SocketAsyncEventArgs>( Accept_Completion );
			Accept_Start();
            #else
            this.m_OnAccept = new AsyncCallback(OnAccept);
            try
            {
                IAsyncResult res = this.m_Listener.BeginAccept(this.m_OnAccept, this.m_Listener);
            }
            catch (SocketException ex)
            {
                NetState.TraceException(ex);
            }
            catch (ObjectDisposedException)
            {
            }
            #endif
        }

        private Socket Bind(IPEndPoint ipep)
        {
            Socket s = new Socket(ipep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                s.LingerState.Enabled = false;
                #if !MONO
                s.ExclusiveAddressUse = false;
                #endif
                s.Bind(ipep);
                s.Listen(8);

                return s;
            }
            catch (Exception e)
            {
                if (e is SocketException)
                {
                    SocketException se = (SocketException)e;

                    if (se.ErrorCode == 10048)
                    { // WSAEADDRINUSE
                        Utility.PushColor(ConsoleColor.Red);
                        Console.WriteLine("Listener Failed: {0}:{1} (In Use)", ipep.Address, ipep.Port);
                        Utility.PopColor();
                    }
                    else if (se.ErrorCode == 10049)
                    { // WSAEADDRNOTAVAIL
                        Utility.PushColor(ConsoleColor.Red);
                        Console.WriteLine("Listener Failed: {0}:{1} (Unavailable)", ipep.Address, ipep.Port);
                        Utility.PopColor();
                    }
                    else
                    {
                        Utility.PushColor(ConsoleColor.Red);
                        Console.WriteLine("Listener Exception:");
                        Console.WriteLine(e);
                        Utility.PopColor();
                    }
                }

                return null;
            }
        }

        private void DisplayListener()
        {
            IPEndPoint ipep = this.m_Listener.LocalEndPoint as IPEndPoint;

            if (ipep == null)
                return;

            if (ipep.Address.Equals(IPAddress.Any) || ipep.Address.Equals(IPAddress.IPv6Any))
            {
                NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface adapter in adapters)
                {
                    IPInterfaceProperties properties = adapter.GetIPProperties();
                    foreach (IPAddressInformation unicast in properties.UnicastAddresses)
                    {
                        if (ipep.AddressFamily == unicast.Address.AddressFamily)
                        {
                            Utility.PushColor(ConsoleColor.Green);
                            Console.WriteLine("Listening: {0}:{1}", unicast.Address, ipep.Port);
                            Utility.PopColor();
                        }
                    }
                }
                /*
                try {
                Console.WriteLine( "Listening: {0}:{1}", IPAddress.Loopback, ipep.Port );
                IPHostEntry iphe = Dns.GetHostEntry( Dns.GetHostName() );
                IPAddress[] ip = iphe.AddressList;
                for ( int i = 0; i < ip.Length; ++i )
                Console.WriteLine( "Listening: {0}:{1}", ip[i], ipep.Port );
                }
                catch { }
                */
            }
            else
            {
                Utility.PushColor(ConsoleColor.Green);
                Console.WriteLine("Listening: {0}:{1}", ipep.Address, ipep.Port);
                Utility.PopColor();
            }

            Utility.PushColor(ConsoleColor.DarkGreen);
            Console.WriteLine(@"----------------------------------------------------------------------");
            Utility.PopColor();
        }

        #if NewAsyncSockets
		private void Accept_Start()
		{
			bool result = false;

			do {
				try {
					result = !m_Listener.AcceptAsync( m_EventArgs );
				} catch ( SocketException ex ) {
					NetState.TraceException( ex );
					break;
				} catch ( ObjectDisposedException ) {
					break;
				}

				if ( result )
					Accept_Process( m_EventArgs );
			} while ( result );
		}

		private void Accept_Completion( object sender, SocketAsyncEventArgs e )
		{
			Accept_Process( e );

			Accept_Start();
		}

		private void Accept_Process( SocketAsyncEventArgs e )
		{
			if ( e.SocketError == SocketError.Success && VerifySocket( e.AcceptSocket ) ) {
				Enqueue( e.AcceptSocket );
			} else {
				Release( e.AcceptSocket );
			}

			e.AcceptSocket = null;
		}

        #else

        private void OnAccept(IAsyncResult asyncResult)
        {
            Socket listener = (Socket)asyncResult.AsyncState;

            Socket accepted = null;

            try
            {
                accepted = listener.EndAccept(asyncResult);
            }
            catch (SocketException ex)
            {
                NetState.TraceException(ex);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            if (accepted != null)
            {
                if (this.VerifySocket(accepted))
                {
                    this.Enqueue(accepted);
                }
                else
                {
                    this.Release(accepted);
                }
            }

            try
            {
                listener.BeginAccept(this.m_OnAccept, listener);
            }
            catch (SocketException ex)
            {
                NetState.TraceException(ex);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        #endif

        private bool VerifySocket(Socket socket)
        {
            try
            {
                SocketConnectEventArgs args = new SocketConnectEventArgs(socket);

                EventSink.InvokeSocketConnect(args);

                return args.AllowConnection;
            }
            catch (Exception ex)
            {
                NetState.TraceException(ex);

                return false;
            }
        }

        private void Enqueue(Socket socket)
        {
            lock (this.m_AcceptedSyncRoot)
            {
                this.m_Accepted.Enqueue(socket);
            }

            Core.Set();
        }

        private void Release(Socket socket)
        {
            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException ex)
            {
                NetState.TraceException(ex);
            }

            try
            {
                socket.Close();
            }
            catch (SocketException ex)
            {
                NetState.TraceException(ex);
            }
        }

        public Socket[] Slice()
        {
            Socket[] array;

            lock (this.m_AcceptedSyncRoot)
            {
                if (this.m_Accepted.Count == 0)
                    return m_EmptySockets;

                array = this.m_Accepted.ToArray();
                this.m_Accepted.Clear();
            }

            return array;
        }

        public void Dispose()
        {
            Socket socket = Interlocked.Exchange<Socket>(ref this.m_Listener, null);

            if (socket != null)
            {
                socket.Close();
            }
        }
    }
}