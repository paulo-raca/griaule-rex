package veridis.embedded.rex.messages;

import java.io.ByteArrayOutputStream;
import java.io.DataInputStream;
import java.io.IOException;
import java.net.InetAddress;
import java.net.InetSocketAddress;
import java.util.concurrent.Semaphore;

import veridis.embedded.rex.RexProtocol;
import veridis.embedded.rex.Util;
import veridis.embedded.rex.RexProtocol.MessageHandler;

public class MsgBasic {
	public static final int COMMAND_DISCOVERY          = 0x01;
	public static final int COMMAND_CONNECTION_REQUEST = 0x02;
	public static final int COMMAND_FEATURES_REQUEST   = 0x0a;
	public static final int COMMAND_FEATURES_RESPONSE  = 0x0b;
	public static final int COMMAND_RESET              = 0x0c;
	public static final int COMMAND_ID_REQUEST         = 0x0d;
	public static final int COMMAND_ID_RESPONSE        = 0x0e;

	//Enviada pelo dispositivo e recebida pelo servidor
	public static interface DiscoveryListener {
		public abstract void discoveryReceived(RexProtocol comm, String name, byte[] mac, byte[] ip, byte[] mask, byte[] gateway);
	}
	//Enviada pelo servidor e recebida pela dispositivo
	public static interface ConnectionRequestListener {
		public abstract void connectionRequested(RexProtocol comm, final InetSocketAddress addr, final int unknown);
	}
	//Enviada pelo servidor e recebida pela dispositivo
	public static interface FeaturesRequestListener {
		public abstract void featuresRequested(RexProtocol comm);
		public abstract void idRequested(RexProtocol comm);
	}
	//Enviada pelo cliente e recebida pelo servidor
	public static interface FeaturesResponseListener {
		public abstract void featuresReceived(RexProtocol comm, int relays, int rs232, int inputs, int leds, int readers, boolean hasKeyboard, boolean hasDisplay, boolean hasBuzzer, boolean hasMP3, int versionRev, int versionSub, int versionMin, int versionMaj);
		public abstract void idReceived(RexProtocol comm, String id);
	}
	//Enviada pelo servidor e recebida pela dispositivo
	public static interface ResetListener {
		public abstract void resetRequested(RexProtocol comm);
	}
	
	
	
	///////////////////////////////////////////////////////////////////////////////////////////////
	public static class Discovery extends MessageHandler {
		private DiscoveryListener listener;
		public Discovery(DiscoveryListener listener) {
			super(COMMAND_DISCOVERY);
			this.listener = listener;
		}
		@Override
		public void handle(DataInputStream in, int inLength, RexProtocol comm) throws IOException {
			byte[] _name = new byte[12];
			in.readFully(_name);
			String name = Util.StringFromBytes(_name);
			
			byte[] _macChars = new byte[12];
			in.readFully(_macChars);
			String macChars = Util.StringFromBytes(_macChars);
			byte[] mac = new byte[6];
			for (int i=0; i<6; i++) {
				mac[i] = (byte)Integer.parseInt(macChars.substring(2*i, 2*i+2), 16);
			}
			
			byte[] ip = new byte[4];
			in.readFully(ip);
			byte[] mask = new byte[4];
			in.readFully(mask);
			byte[] gateway = new byte[4];
			in.readFully(gateway);

			listener.discoveryReceived(comm, name, mac, ip, mask, gateway);
		}
		public static void send(RexProtocol comm, String name, byte[] mac, byte[] ip, byte[] mask, byte[] gateway) throws IOException {
			ByteArrayOutputStream cmdOut = new ByteArrayOutputStream();
			cmdOut.write(Util.formatID(name));
			cmdOut.write(Util.macChars(mac));
			cmdOut.write(ip);
			cmdOut.write(mask);
			cmdOut.write(gateway);
			
			comm.sendCommand(COMMAND_DISCOVERY, cmdOut.toByteArray());
		}
	}
	
	
	///////////////////////////////////////////////////////////////////////////////////////////////
	public static class ConnectionRequest extends MessageHandler {
		private ConnectionRequestListener listener;
		public ConnectionRequest(ConnectionRequestListener listener) {
			super(COMMAND_CONNECTION_REQUEST);
			this.listener = listener;
		}
		@Override
		public void handle(DataInputStream in, int inLength, RexProtocol comm) throws IOException {
			int blah = Util.readInt(in); //FIXME O que é esse argumento?
			
			byte[] ip = new byte[4];
			ip[3] = (byte)Util.readByte(in);
			ip[2] = (byte)Util.readByte(in);
			ip[1] = (byte)Util.readByte(in);
			ip[0] = (byte)Util.readByte(in);

			int port = Util.readInt(in);
			
			InetSocketAddress sockAddr = new InetSocketAddress(InetAddress.getByAddress(ip), port);
			listener.connectionRequested(comm, sockAddr, blah);
		}
		public static void send(RexProtocol comm, int blah, InetSocketAddress addr) throws IOException {
			ByteArrayOutputStream cmdOut = new ByteArrayOutputStream();
			Util.writeInt(cmdOut, blah);
			byte[] ip = addr.getAddress().getAddress();
			cmdOut.write(ip[3]);
			cmdOut.write(ip[2]);
			cmdOut.write(ip[1]);
			cmdOut.write(ip[0]);
			Util.writeInt(cmdOut, addr.getPort());
			comm.sendCommand(COMMAND_CONNECTION_REQUEST, cmdOut.toByteArray());
		}
	}
	
	
	///////////////////////////////////////////////////////////////////////////////////////////////
	public static class FeaturesRequest extends MessageHandler {
		private FeaturesRequestListener listener;
		public FeaturesRequest(FeaturesRequestListener listener) {
			super(COMMAND_FEATURES_REQUEST);
			this.listener = listener;
		}
		@Override
		public void handle(DataInputStream in, int inLength, RexProtocol comm) throws IOException {
			listener.featuresRequested(comm);
		}
		public static void send(RexProtocol comm) throws IOException {
			comm.sendCommand(COMMAND_FEATURES_REQUEST);
		}
	}
	
	
	///////////////////////////////////////////////////////////////////////////////////////////////
	public static class FeaturesResponse extends MessageHandler {
		private FeaturesResponseListener listener;
		public FeaturesResponse(FeaturesResponseListener listener) {
			super(COMMAND_FEATURES_RESPONSE);
			this.listener = listener;
		}
		@Override
		public void handle(DataInputStream in, int inLength, RexProtocol comm) throws IOException {
			int relays          = Util.readInt(in);
			int rs232           = Util.readInt(in);
			int inputs          = Util.readInt(in);
			int leds            = Util.readInt(in);
			int readers         = Util.readInt(in);
			boolean hasKeyboard = Util.readInt(in) != 0;
			boolean hasDisplay  = Util.readInt(in) != 0;
			boolean hasBuzzer   = Util.readInt(in) != 0;
			boolean hasMP3      = Util.readInt(in) != 0;
			int versionRev      = Util.readByte(in);
			int versionSub      = Util.readByte(in);
			int versionMin      = Util.readByte(in);
			int versionMaj      = Util.readByte(in);
			listener.featuresReceived(comm, relays, rs232, inputs, leds, readers, hasKeyboard, hasDisplay, hasBuzzer, hasMP3, versionRev, versionSub, versionMin, versionMaj);			
		}
		public static void send(RexProtocol comm, int relays, int rs232, int inputs, int leds, int readers, boolean hasKeyboard, boolean hasDisplay, boolean hasBuzzer) throws IOException {
			ByteArrayOutputStream cmdOut = new ByteArrayOutputStream();
			Util.writeInt(cmdOut, relays);
			Util.writeInt(cmdOut, rs232);
			Util.writeInt(cmdOut, inputs);
			Util.writeInt(cmdOut, leds);
			Util.writeInt(cmdOut, readers);
			Util.writeInt(cmdOut, hasKeyboard?1:0);
			Util.writeInt(cmdOut, hasDisplay?1:0);
			Util.writeInt(cmdOut, hasBuzzer?1:0); 
			Util.writeInt(cmdOut, 0); //MP3 -> Não tem			
			cmdOut.write(RexProtocol.VERSION_REV);
			cmdOut.write(RexProtocol.VERSION_SUB);
			cmdOut.write(RexProtocol.VERSION_MINOR);
			cmdOut.write(RexProtocol.VERSION_MAJOR);
			comm.sendCommand(COMMAND_FEATURES_RESPONSE, cmdOut.toByteArray());
		}
	}
	
	///////////////////////////////////////////////////////////////////////////////////////////////
	public static class IdRequest extends MessageHandler {
		private FeaturesRequestListener listener;
		public IdRequest(FeaturesRequestListener listener) {
			super(COMMAND_ID_REQUEST);
			this.listener = listener;
		}
		@Override
		public void handle(DataInputStream in, int inLength, RexProtocol comm) throws IOException {
			listener.idRequested(comm);
		}
		public static void send(RexProtocol comm) throws IOException {
			comm.sendCommand(COMMAND_ID_REQUEST);
		}
		public static String sendAndWait(RexProtocol comm) throws IOException, InterruptedException {
			final Semaphore sem = new Semaphore(0);
			final String[] ptrResult = new String[1];
			final IdResponse response = new IdResponse(new FeaturesResponseListener() {
				@Override
				public void idReceived(RexProtocol comm, String id) {
					ptrResult[0] = id;
					sem.release();
				}
				@Override
				public void featuresReceived(RexProtocol comm, int relays, int rs232, int inputs, int leds, int readers, boolean hasKeyboard, boolean hasDisplay, boolean hasBuzzer, boolean hasMP3, int versionRev, int versionSub, int versionMin, int versionMaj) {}
			});
			comm.rex.addMessageHandler(response);
			send(comm);
			sem.acquire();
			comm.rex.removeMessageHandler(response);

			return ptrResult[0];
		}
	}
	
	
	///////////////////////////////////////////////////////////////////////////////////////////////
	public static class IdResponse extends MessageHandler {
		private FeaturesResponseListener listener;
		public IdResponse(FeaturesResponseListener listener) {
			super(COMMAND_ID_RESPONSE);
			this.listener = listener;
		}
		@Override
		public void handle(DataInputStream in, int inLength, RexProtocol comm) throws IOException {
			byte[] buf = new byte[inLength];
			in.readFully(buf);
			listener.idReceived(comm, Util.StringFromBytes(buf));
		}
		public static void send(RexProtocol comm, String ID) throws IOException {
			comm.sendCommand(COMMAND_ID_RESPONSE, Util.formatID(ID));
		}
	}
	
	
	///////////////////////////////////////////////////////////////////////////////////////////////	
	public static class ResetRequest extends MessageHandler {
		private ResetListener listener;
		public ResetRequest(ResetListener listener) {
			super(COMMAND_RESET);
			this.listener = listener;
		}
		@Override
		public void handle(DataInputStream in, int inLength, RexProtocol comm) throws IOException {
			listener.resetRequested(comm);
		}
		public static void send(RexProtocol comm) throws IOException {
			comm.sendCommand(COMMAND_RESET);
		}
	}
}
