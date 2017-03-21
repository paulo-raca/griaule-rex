using System;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace GriauleRexDotNet {
	
	public class RexClient : RexProtocol, IDisposable {
		
		private const int  PORT_DISCOVERY          = 2800;
		private const int  PORT_CONNECTION_REQUEST = 1025;

		private UdpClient udpClient;
		private TcpListener tcpServer;

		public delegate void RexDiscoveryEventHandler(String name, PhysicalAddress mac, IPAddress ip, IPAddress mask, IPAddress gateway);
		public event RexDiscoveryEventHandler Discovered;

		public delegate void RexConnectionEventHandler(RexDevice rex);
		public event RexConnectionEventHandler Connected;
		public event RexConnectionEventHandler Disconnected;

		public RexClient(int localPort = 0) 
			: this (IPAddress.Any, localPort) 
		{ }

		public RexClient(IPAddress localAddress, int localPort = 0)
			: this (new IPEndPoint(localAddress, localPort))
		{ }

		public RexClient(IPEndPoint localEndpoint) {
			this.RawListeners[COMMAND_DISCOVERY] += RawDiscoveryReceived;
			udpClient = new UdpClient (new IPEndPoint(localEndpoint.Address, PORT_DISCOVERY));
			tcpServer = new TcpListener (localEndpoint);
			ReceiveDiscoveryLoopAsync();
			ReceiveConnectionsLoopAsync();
		}

		public void Dispose() {
			udpClient.Close ();
			tcpServer.Stop ();
			//TODO: await ReceiveDiscoveryLoop and ReceiveConnectionsLoop
		}

		private void RawDiscoveryReceived(byte[] payload) {
			MemoryStream stream = new MemoryStream (payload);

			String name = STRING_ENCODING.GetString(stream.ReadFully(12));
			PhysicalAddress mac = new PhysicalAddress(Util.parseHex(STRING_ENCODING.GetString(stream.ReadFully(12))));
			IPAddress ip = new IPAddress(stream.ReadFully(4));
			IPAddress mask = new IPAddress(stream.ReadFully(4));
			IPAddress gateway = new IPAddress(stream.ReadFully(4));

			if (Discovered != null) {
				Discovered (name, mac, ip, mask, gateway);
			}
		}

		protected void SendMessage (int cmd, byte[] payload, IPEndPoint destination) {
			MemoryStream stream = new MemoryStream (payload.Length + 12);
			stream.WriteInt (PROTOCOL_PREFIX);
			stream.WriteInt (cmd);
			stream.WriteInt (payload.Length);
			stream.WriteFully (payload);

			udpClient.Send (stream.GetBuffer(), (int)stream.Length, destination);
		}

		public void RequestConnection(IPAddress rexAddress, int rexPort = PORT_CONNECTION_REQUEST) {
			RequestConnection(new IPEndPoint(rexAddress, rexPort));
		}

		public void RequestConnection(IPEndPoint rexUdpEndpoint, IPEndPoint localTcpEndpoint = null) {
			if (localTcpEndpoint == null) {
				localTcpEndpoint = GetLocalEndPointFor (rexUdpEndpoint.Address, ((IPEndPoint)tcpServer.LocalEndpoint).Port);
			}
			// Console.WriteLine ("RequestConnection: " + rexEndpoint + " to " + localEndpoint );

			MemoryStream stream = new MemoryStream (12);

			// Unknown
			stream.WriteInt (0); 

			//IP Address (Reversed)
			stream.WriteByte(localTcpEndpoint.Address.GetAddressBytes()[3]);
			stream.WriteByte(localTcpEndpoint.Address.GetAddressBytes()[2]);
			stream.WriteByte(localTcpEndpoint.Address.GetAddressBytes()[1]);
			stream.WriteByte(localTcpEndpoint.Address.GetAddressBytes()[0]);

			stream.WriteInt (localTcpEndpoint.Port);

			SendMessage(COMMAND_CONNECTION_REQUEST, stream.GetBuffer(), rexUdpEndpoint);
		}

		private async Task ReceiveDiscoveryLoopAsync() {
			while (true) {
				UdpReceiveResult package;
				try {
					package = await udpClient.ReceiveAsync ();
				} catch (ObjectDisposedException) {
					//Console.WriteLine ("break listenRequests()");
					break;
				}
				parseRawMessage (package.Buffer);
			}
		}

		private async Task ReceiveConnectionsLoopAsync() {
			tcpServer.Start ();
			while (true) {
				TcpClient socket;
				try {
					socket = await tcpServer.AcceptTcpClientAsync();
				} catch (ObjectDisposedException) {
					//Console.WriteLine ("break listenConnections()");
					break;
				}
				RexDevice rex = new RexDevice (socket.GetStream());
				rex.Connected += () => {
					if (Connected != null) Connected(rex);
				};
				rex.Disconnected += () => {
					if (Disconnected != null) Disconnected(rex);
				};
				rex.InitializeAsync ();
			}
		}


		private static IPEndPoint GetLocalEndPointFor(IPAddress remote, int localPort) {
			// http://stackoverflow.com/a/14141114/995480
			using (Socket s = new Socket(remote.AddressFamily, SocketType.Dgram, ProtocolType.IP)) {
				// Just picked a random port, you could make this application
				// specific if you want, but I don't think it really matters
				s.Connect(new IPEndPoint(remote, localPort));

				return new IPEndPoint(((IPEndPoint)s.LocalEndPoint).Address, localPort);
			}
		}
	}
}
