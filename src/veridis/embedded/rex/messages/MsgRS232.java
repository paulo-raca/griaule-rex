package veridis.embedded.rex.messages;

import java.io.ByteArrayOutputStream;
import java.io.DataInputStream;
import java.io.IOException;

import veridis.embedded.rex.RexProtocol;
import veridis.embedded.rex.Util;
import veridis.embedded.rex.RexProtocol.MessageHandler;

public class MsgRS232 {
	public static final int COMMAND_RS232_OPEN     = 0x32;
	public static final int COMMAND_RS232_SET_MODE = 0x33;
	public static final int COMMAND_RS232_READ     = 0x34;
	public static final int COMMAND_RS232_WRITE    = 0x35;
	public static final int COMMAND_RS232_CLOSE    = 0x36;
	
	public static final int RS232_PARITY_NONE = 0;
	public static final int RS232_PARITY_ODD = 1;
	public static final int RS232_PARITY_EVEN = 2;
	
	public static final int RS232_FLOW_CONTROL_NONE = 0;
	public static final int RS232_FLOW_CONTROL_SOFTWARE = 1;
	public static final int RS232_FLOW_CONTROL_HARDWARE = 2;
	
	public static interface RS232Listener {
		public abstract void rs232Open(RexProtocol conn, int portNumber, int baud, int parity, int bits, int stopBits, int flowControl) throws IOException;
	
		public abstract void rs232Close(RexProtocol conn, int portNumber);
	
		public abstract void rs232Write(RexProtocol conn, int portNumber, byte[] buffer) throws IOException;
	
		public abstract void rs232SetMode(RexProtocol conn, int portNumber, boolean synchroneous, boolean binary, int packSize);
	
		public abstract void rs232Read(RexProtocol conn, int portNumber, int bufferLength) throws IOException;
	}
	
	
	///////////////////////////////////////////////////////////////////////////////////////////////	
	public static class Open extends MessageHandler {
		private RS232Listener listener;
		public Open(RS232Listener listener) {
			super(COMMAND_RS232_OPEN);
			this.listener = listener;
		}
		@Override
		public void handle(DataInputStream in, int inLength, RexProtocol comm) throws IOException {
			int portNumber = Util.readInt(in);
			int baud = Util.readInt(in);
			int parity = Util.readInt(in); //None=0, Odd=1, Even2
			int bits = Util.readInt(in);
			int stopBits = Util.readInt(in);
			int flowControl = Util.readInt(in); //None=0, Soft=1, Hard=2
					
			listener.rs232Open(comm, portNumber, baud, parity, bits, stopBits, flowControl);
		}
		public static void send(RexProtocol comm, int portNumber, int baud, int parity, int bits, int stopBits, int flowControl) throws IOException {
			ByteArrayOutputStream cmdOut = new ByteArrayOutputStream();
			Util.writeInt(cmdOut, portNumber);
			Util.writeInt(cmdOut, baud);
			Util.writeInt(cmdOut, parity);
			Util.writeInt(cmdOut, bits);
			Util.writeInt(cmdOut, stopBits);
			Util.writeInt(cmdOut, flowControl);
			comm.sendCommand(COMMAND_RS232_OPEN, cmdOut.toByteArray());
		}
	}
	
	
	///////////////////////////////////////////////////////////////////////////////////////////////	
	public static class Close extends MessageHandler {
		private RS232Listener listener;
		public Close(RS232Listener listener) {
			super(COMMAND_RS232_CLOSE);
			this.listener = listener;
		}
		@Override
		public void handle(DataInputStream in, int inLength, RexProtocol comm) throws IOException {
			int portNumber = Util.readInt(in);
			listener.rs232Close(comm, portNumber);
		}
		public static void send(RexProtocol comm, int portNumber) throws IOException {
			ByteArrayOutputStream cmdOut = new ByteArrayOutputStream();
			Util.writeInt(cmdOut, portNumber);
			comm.sendCommand(COMMAND_RS232_CLOSE, cmdOut.toByteArray()); 
		}
	}
	
	///////////////////////////////////////////////////////////////////////////////////////////////	
	public static class SetMode extends MessageHandler {
		private RS232Listener listener;
		public SetMode(RS232Listener listener) {
			super(COMMAND_RS232_SET_MODE);
			this.listener = listener;
		}
		@Override
		public void handle(DataInputStream in, int inLength, RexProtocol comm) throws IOException {
			int portNumber        = Util.readInt(in);
			boolean synchroneous  = Util.readInt(in) != 0; //Async=0, Sync = 1, 
			boolean binaryMode    = Util.readInt(in) != 0; //Ascii=0, Bin=1
			int packSize          = Util.readInt(in);
			
			listener.rs232SetMode(comm, portNumber, !synchroneous, binaryMode, packSize);
		}
		public static void send(RexProtocol comm, int portNumber, boolean asynchroneous, boolean binaryMode, int packSize) throws IOException {
			ByteArrayOutputStream cmdOut = new ByteArrayOutputStream();
			Util.writeInt(cmdOut, portNumber);
			Util.writeInt(cmdOut, !asynchroneous ? 1 : 0);
			Util.writeInt(cmdOut, binaryMode ? 1 : 0);
			Util.writeInt(cmdOut, packSize);
			comm.sendCommand(COMMAND_RS232_SET_MODE, cmdOut.toByteArray()); 
		}
	}
	
	
	///////////////////////////////////////////////////////////////////////////////////////////////	
	public static class Read extends MessageHandler {
		private RS232Listener listener;
		public Read(RS232Listener listener) {
			super(COMMAND_RS232_READ);
			this.listener = listener;
		}
		@Override
		public void handle(DataInputStream in, int inLength, RexProtocol comm) throws IOException {
			int portNumber = Util.readInt(in);
			int bufferLength = Util.readInt(in);
			listener.rs232Read(comm, portNumber, bufferLength);
		}
		public static void send(RexProtocol comm, int portNumber, int bufferLength) throws IOException {
			ByteArrayOutputStream cmdOut = new ByteArrayOutputStream();
			Util.writeInt(cmdOut, portNumber);
			Util.writeInt(cmdOut, bufferLength);
			comm.sendCommand(COMMAND_RS232_READ, cmdOut.toByteArray()); 
		}
	}
	
	
	///////////////////////////////////////////////////////////////////////////////////////////////	
	public static class Write extends MessageHandler {
		private RS232Listener listener;
		public Write(RS232Listener listener) {
			super(COMMAND_RS232_WRITE);
			this.listener = listener;
		}
		@Override
		public void handle(DataInputStream in, int inLength, RexProtocol comm) throws IOException {
			int portNumber = Util.readInt(in);
			byte[] buffer = new byte[inLength-4];
			in.readFully(buffer);
			listener.rs232Write(comm, portNumber, buffer);
		}
		public static void send(RexProtocol comm, int port, byte[] buffer) throws IOException {
			ByteArrayOutputStream cmdOut = new ByteArrayOutputStream();
			Util.writeInt(cmdOut, port);
			comm.sendCommand(COMMAND_RS232_WRITE, cmdOut.toByteArray(), buffer); 
		}
	}
}
