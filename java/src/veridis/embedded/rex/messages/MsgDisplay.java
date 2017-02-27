package veridis.embedded.rex.messages;

import java.io.ByteArrayOutputStream;
import java.io.DataInputStream;
import java.io.IOException;

import veridis.embedded.rex.RexProtocol;
import veridis.embedded.rex.Util;
import veridis.embedded.rex.RexProtocol.MessageHandler;

public class MsgDisplay {
	private static final int COMMAND_DISPLAY_INITIALIZE       = 0x1e;
	private static final int COMMAND_DISPLAY_CLEAR            = 0x1f;
	private static final int COMMAND_DISPLAY_SET_ENTRY_MODE   = 0x21;
	private static final int COMMAND_DISPLAY_SET_CURSOR       = 0x23;
	private static final int COMMAND_DISPLAY_MOVE             = 0x24;
	private static final int COMMAND_DISPLAY_WRITE            = 0x28;
	
	public static interface DisplayListener {
		public abstract void displayClear(RexProtocol comm);
		public abstract void displayInit(RexProtocol comm, int width, int height, int busWidth, boolean font5x10);	
		public abstract void displaySetEntryMode(RexProtocol comm, boolean moveMessage, boolean toRight);
		public abstract void displaySetCursor(RexProtocol comm, boolean displayOn, boolean cursorOn, boolean blinking);
		public abstract void displayWrite(RexProtocol comm, int line, int col, int unknown, String str);
		public abstract void displayMove(RexProtocol comm, boolean moveMessage, int offset);
	}
	
	
	///////////////////////////////////////////////////////////////////////////////////////////////	
	public static class Clear extends MessageHandler {
		private DisplayListener listener;
		public Clear(DisplayListener listener) {
			super(COMMAND_DISPLAY_CLEAR);
			this.listener = listener;
		}
		@Override
		public void handle(DataInputStream in, int inLength, RexProtocol comm) throws IOException {
			listener.displayClear(comm);
		}
		public static void send(RexProtocol comm) throws IOException {
			comm.sendCommand(COMMAND_DISPLAY_CLEAR);
		}
	}

	
	///////////////////////////////////////////////////////////////////////////////////////////////	
	public static class Initialize extends MessageHandler {
		private DisplayListener listener;
		public Initialize(DisplayListener listener) {
			super(COMMAND_DISPLAY_INITIALIZE);
			this.listener = listener;
		}
		@Override
		public void handle(DataInputStream in, int inLength, RexProtocol comm) throws IOException {
			int width = Util.readInt(in);
			int height = Util.readInt(in);
			int busWidth = Util.readInt(in);//4 ou 8
			int font = Util.readInt(in); //5x7=0, 5x10=1
			
			listener.displayInit(comm, width, height, busWidth, font!=0);
		}
		public static void send(RexProtocol comm, int width, int height, boolean font5x10) throws IOException {
			ByteArrayOutputStream cmdOut = new ByteArrayOutputStream();
			Util.writeInt(cmdOut, width);
			Util.writeInt(cmdOut, height);
			Util.writeInt(cmdOut, 8);
			Util.writeInt(cmdOut, font5x10?1:0);
			comm.sendCommand(COMMAND_DISPLAY_INITIALIZE, cmdOut.toByteArray());
		}
	}
	
	
	///////////////////////////////////////////////////////////////////////////////////////////////	
	public static class SetEntryMode extends MessageHandler {
		public static final int DISPLAY_ENTRY_MODE_CURSOR_LEFT = 4;
		public static final int DISPLAY_ENTRY_MODE_MESSAGE_LEFT = 5;
		public static final int DISPLAY_ENTRY_MODE_CURSOR_RIGHT = 6;
		public static final int DISPLAY_ENTRY_MODE_MESSAGE_RIGHT = 7;
		
		private DisplayListener listener;
		public SetEntryMode(DisplayListener listener) {
			super(COMMAND_DISPLAY_SET_ENTRY_MODE);
			this.listener = listener;
		}
		@Override
		public void handle(DataInputStream in, int inLength, RexProtocol comm) throws IOException {
			int mode = Util.readInt(in);
			listener.displaySetEntryMode(comm, 
					mode == DISPLAY_ENTRY_MODE_MESSAGE_LEFT  || mode == DISPLAY_ENTRY_MODE_MESSAGE_RIGHT, 
					mode == DISPLAY_ENTRY_MODE_MESSAGE_RIGHT || mode == DISPLAY_ENTRY_MODE_CURSOR_RIGHT);
		}
		public static void send(RexProtocol comm, boolean moveMessage, boolean toRight) throws IOException {
			ByteArrayOutputStream cmdOut = new ByteArrayOutputStream();
			if (moveMessage) {
				if (toRight) {
					Util.writeInt(cmdOut, DISPLAY_ENTRY_MODE_MESSAGE_RIGHT);
				} else {
					Util.writeInt(cmdOut, DISPLAY_ENTRY_MODE_MESSAGE_LEFT);
				}
			} else {
				if (toRight) {
					Util.writeInt(cmdOut, DISPLAY_ENTRY_MODE_CURSOR_RIGHT);
				} else { 
					Util.writeInt(cmdOut, DISPLAY_ENTRY_MODE_CURSOR_LEFT);
				}
			}
			comm.sendCommand(COMMAND_DISPLAY_SET_ENTRY_MODE, cmdOut.toByteArray());			
		}
	}
	
	
	///////////////////////////////////////////////////////////////////////////////////////////////	
	public static class SetCursor extends MessageHandler {
		private DisplayListener listener;
		public SetCursor(DisplayListener listener) {
			super(COMMAND_DISPLAY_SET_CURSOR);
			this.listener = listener;
		}
		@Override
		public void handle(DataInputStream in, int inLength, RexProtocol comm) throws IOException {
			int cur = Util.readInt(in); //off=12, offblinking=13, on=14, cmOnBlinking=15
			listener.displaySetCursor(comm, (cur&4)!=0, (cur&2)!=0, (cur&1)!=0);
		}
		public static void send(RexProtocol comm, boolean displayOn, boolean cursorOn, boolean cursorBlinking) throws IOException {
			int val=0;
			if (displayOn     ) val |=4;
			if (cursorOn      ) val |=2;
			if (cursorBlinking) val |=1;
			
			ByteArrayOutputStream cmdOut = new ByteArrayOutputStream();
			Util.writeInt(cmdOut, val);
			comm.sendCommand(COMMAND_DISPLAY_SET_CURSOR, cmdOut.toByteArray());
		}
	}
	
	
	///////////////////////////////////////////////////////////////////////////////////////////////	
	public static class Write extends MessageHandler {
		private DisplayListener listener;
		public Write(DisplayListener listener) {
			super(COMMAND_DISPLAY_WRITE);
			this.listener = listener;
		}
		@Override
		public void handle(DataInputStream in, int inLength, RexProtocol comm) throws IOException {
			int line = Util.readInt(in);
			int col  = Util.readInt(in);
			int cri  = Util.readInt(in);
			byte[] strBytes = new byte[inLength - 12];
			in.readFully(strBytes);
			
			listener.displayWrite(comm, line, col, cri, Util.StringFromBytes(strBytes));
		}
		public static void send(RexProtocol comm, String message) throws IOException {
			send(comm, message, -1, -1);
		}
		public static void send(RexProtocol comm, String message, int line, int col) throws IOException {
			byte[] msgBytes = Util.StringToBytes(message);

			ByteArrayOutputStream cmdOut = new ByteArrayOutputStream();
			Util.writeInt(cmdOut, line);
			Util.writeInt(cmdOut, col);
			Util.writeInt(cmdOut, 0); //Cri?
			
			comm.sendCommand(COMMAND_DISPLAY_WRITE, cmdOut.toByteArray(), msgBytes);
		}
	}
	
	
	///////////////////////////////////////////////////////////////////////////////////////////////	
	public static class Move extends MessageHandler {
		private DisplayListener listener;
		public Move(DisplayListener listener) {
			super(COMMAND_DISPLAY_MOVE);
			this.listener = listener;
		}
		@Override
		public void handle(DataInputStream in, int inLength, RexProtocol comm) throws IOException {
			int a = Util.readInt(in);    //a=0x18  => moveMessage, offset Negativo
			//a=0x1c  => moveMessage, offset Positivo
			//a=0x10  => moveCursor, offset Negativo
			//a=0x14  => moveCursor, offset Positivo		
			int offset = Util.readInt(in);

			if (a == 0x18 || a == 0x10 )
				offset = -offset;

			boolean moveMessage = (a == 0x18 || a == 0x1C );

			listener.displayMove(comm, moveMessage, offset);
		}
		public static void send(RexProtocol comm) throws IOException {
			throw new UnsupportedOperationException(); //FIXME
		}
	}
}
