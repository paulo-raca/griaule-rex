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

		public RexClient(int tcpPort = 0) {
			this.RawListeners[COMMAND_DISCOVERY] += RawDiscoveryReceived;
			udpClient = new UdpClient (PORT_DISCOVERY);
			tcpServer = new TcpListener (IPAddress.Parse("0.0.0.0"), tcpPort);
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

		public void RequestConnection(IPAddress rexAddress) {
			foreach (NetworkInterface netInterface in NetworkInterface.GetAllNetworkInterfaces()) {
				if (netInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
				{
					continue;
				}

				foreach (UnicastIPAddressInformation addr in netInterface.GetIPProperties().UnicastAddresses)
				{
					IPAddress serverAddress = addr.Address;
					if (serverAddress.AddressFamily == AddressFamily.InterNetwork) {
						MemoryStream stream = new MemoryStream (12);

						// Unknown
						stream.WriteInt (0); 

						//IP Address (Reversed)
						stream.WriteByte(serverAddress.GetAddressBytes()[3]);
						stream.WriteByte(serverAddress.GetAddressBytes()[2]);
						stream.WriteByte(serverAddress.GetAddressBytes()[1]);
						stream.WriteByte(serverAddress.GetAddressBytes()[0]);

						stream.WriteInt (((IPEndPoint)tcpServer.LocalEndpoint).Port); 

						SendMessage(COMMAND_CONNECTION_REQUEST, stream.GetBuffer(), new IPEndPoint(rexAddress, PORT_CONNECTION_REQUEST));
					}
				}
			}
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
	}
}
