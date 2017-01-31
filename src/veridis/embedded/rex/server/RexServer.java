package veridis.embedded.rex.server;

import java.io.IOException;
import java.net.InetAddress;
import java.net.InetSocketAddress;
import java.net.InterfaceAddress;
import java.net.NetworkInterface;
import java.util.Arrays;
import java.util.Enumeration;

import veridis.embedded.rex.RexEndpoint;
import veridis.embedded.rex.RexProtocol;
import veridis.embedded.rex.Util;
import veridis.embedded.rex.RexProtocol.TCP;
import veridis.embedded.rex.messages.MsgBasic;
import veridis.embedded.rex.messages.MsgDisplay;
import veridis.embedded.rex.messages.MsgIO;
import veridis.embedded.rex.messages.MsgRS232;
import veridis.embedded.rex.messages.MsgBasic.DiscoveryListener;
import veridis.embedded.rex.messages.MsgIO.ImageCaptureListener;
import veridis.embedded.rex.messages.MsgIO.InputListener;
import veridis.embedded.rex.messages.MsgRS232.RS232Listener;

public class RexServer extends RexEndpoint implements DiscoveryListener, ImageCaptureListener, InputListener, RS232Listener {
	int TCP_PORT;
	public RexServer(int port) throws IOException {
		this.TCP_PORT = port;
		addMessageHandler(new MsgBasic.Discovery(this));
		addMessageHandler(new MsgIO.ImageCapture(this));
		addMessageHandler(new MsgIO.InputChange(this));
		addMessageHandler(new MsgIO.KeyTyped(this));
		addMessageHandler(new MsgRS232.Write(this));
		new RexServerDiscovery(this, port);
	}
	
	public void connectionStarted(RexProtocol comm) throws IOException, InterruptedException {
		String id = MsgBasic.IdRequest.sendAndWait(comm);
		System.out.println("Rex ID is " + id);
		
		MsgDisplay.Write.send(comm, "Hi ");
		MsgDisplay.Write.send(comm, " there");
		MsgDisplay.Write.send(comm, "Foo", 1, 1);
	
		for (int i=0; i<5; i++) {
			MsgRS232.Open.send(comm, i, 9600, MsgRS232.RS232_PARITY_NONE, 8, 1, MsgRS232.RS232_FLOW_CONTROL_NONE);
		}
		
		for (int i=0; i<4; i++) {
			int[][] IOs = {
				{MsgIO.DigitalOutput.IO_TYPE_BUZZER, 0},
				{MsgIO.DigitalOutput.IO_TYPE_RELAY, 0},
				{MsgIO.DigitalOutput.IO_TYPE_RELAY, 1},
				{MsgIO.DigitalOutput.IO_TYPE_RELAY, 2},
				{MsgIO.DigitalOutput.IO_TYPE_BACKLIGHT, 0},
			};
			for (int[] io : IOs) {
				MsgIO.DigitalOutput.send(comm, io[0], io[1], 200, 200, 3);
				Thread.sleep(1500);
			}
			for (int r=0; r<4; r++) {
				MsgRS232.Write.send(comm, r, Util.StringToBytes("Hello From serial port #"+r+"\r\n"));
			}
		}
	}
	public void connectionFinished(TCP conn) {
		System.out.println("Rex disconnected: " + conn.socket);
	}
	
	public String getID() {
		return "SERVER";
	}
	
	@Override
	public void discoveryReceived(RexProtocol comm, String name, byte[] mac, byte[] ip, byte[] mask, byte[] gateway) {
		//System.out.println(RexDevice.EVENT_PREFIX + "Discovery from " + name);
		
		try {
			Enumeration<NetworkInterface> nics = NetworkInterface.getNetworkInterfaces();
			while (nics.hasMoreElements()) {
				NetworkInterface nic = nics.nextElement();
				if (nic.isLoopback() || !nic.isUp()) continue;
				for (InterfaceAddress addr : nic.getInterfaceAddresses()) {
					InetAddress inetaddr = addr.getAddress();
					if (inetaddr.getAddress() == null) continue;
					if (inetaddr.getAddress().length != 4) continue;
					try {
						((RexProtocol.UDP)comm).setDestinationAddress(new InetSocketAddress(InetAddress.getByAddress(ip), RexProtocol.UDP.PORT_CONNECTION_REQUEST));
						MsgBasic.ConnectionRequest.send(comm, 0, new InetSocketAddress(inetaddr, TCP_PORT));
					} catch (IOException e){
						e.printStackTrace();
					}
				}
			}
		} catch (IOException e){
			e.printStackTrace();
		}
	}

	@Override
	public void imageCaptured(RexProtocol comm, int width, int height, int resX, int resY, byte[] imgBuf, String sensorName) {
		System.out.println(Util.EVENT_PREFIX + "Got image from " + sensorName);
	}

	@Override
	public void inputChanged(RexProtocol comm, int port, boolean isOn) {
		System.out.println(Util.EVENT_PREFIX + "Input #"+port+" set to " + isOn);
	}

	@Override
	public void keyTyped(RexProtocol comm, int keyCode) {
		String keyName = (char)keyCode + "";
		if (keyCode == 13) keyName = "Enter";
		if (keyCode == 27) keyName = "Esc";
		System.out.println(Util.EVENT_PREFIX + "Key #"+keyCode+"("+keyName+") typed");
	}
	
	@Override
	public void rs232Close(RexProtocol conn, int portNumber) {
		throw new UnsupportedOperationException("rs232Close");
	}

	@Override
	public void rs232Open(RexProtocol conn, int portNumber, int baud, int parity, int bits, int stopBits, int flowControl) throws IOException {
		throw new UnsupportedOperationException("rs232Open");
	}

	@Override
	public void rs232Read(RexProtocol conn, int portNumber, int bufferLength) throws IOException {
		throw new UnsupportedOperationException("rs232Read");		
	}

	@Override
	public void rs232SetMode(RexProtocol conn, int portNumber, boolean synchroneous, boolean binary, int packSize) {
		throw new UnsupportedOperationException("rs232SetMode");
	}

	@Override
	public void rs232Write(RexProtocol conn, int portNumber, byte[] buffer) throws IOException {
		System.out.println("RS232 #" + portNumber + ": \"" + Util.StringFromBytes(Arrays.copyOfRange(buffer, 0, buffer.length-1)) + "\"" + Arrays.toString(buffer));
	}


	public static void main(String[] args) throws IOException, InterruptedException {
		new RexServer(9876);
	}
}
