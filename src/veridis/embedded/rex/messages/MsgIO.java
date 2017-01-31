package veridis.embedded.rex.messages;

import java.io.ByteArrayOutputStream;
import java.io.DataInputStream;
import java.io.IOException;

import veridis.embedded.rex.RexProtocol;
import veridis.embedded.rex.Util;
import veridis.embedded.rex.RexProtocol.MessageHandler;

public class MsgIO {
	public static final int COMMAND_IO                 = 0x12;
	public static final int COMMAND_INPUT_CHANGED      = 0x3c;
	public static final int COMMAND_IMAGE_ACQUIRED     = 0x46;
	public static final int COMMAND_KEY_TYPED          = 0x50;

	
	public static interface DigitalOutputListener {	
		public abstract void toggleDigitalOutput(RexProtocol comm, int portType, int portNum, int timeOn, int timeOff, int repeats);
	}
	
	public static interface InputListener {
		public abstract void inputChanged(RexProtocol comm, int port, boolean isOn);
		public abstract void keyTyped    (RexProtocol comm, int keyCode);
	}
	
	public static interface ImageCaptureListener {
		public abstract void imageCaptured(RexProtocol comm, int width, int height, int resX, int resY, byte[] imgBuf, String sensorName);
	}
	
	
	///////////////////////////////////////////////////////////////////////////////////////////////	
	public static class DigitalOutput extends MessageHandler {
		public static final int IO_TYPE_RELAY = 1;
		public static final int IO_TYPE_LED = 2;
		public static final int IO_TYPE_BACKLIGHT = 3;
		public static final int IO_TYPE_BUZZER = 4;

		public static String IO_NAME(int type, int port) {
			switch (type) {
				case IO_TYPE_RELAY:      return "Relay #"+port;
				case IO_TYPE_LED:        return "Led #"+port;
				case IO_TYPE_BACKLIGHT:  return "Backlight #"+port;
				case IO_TYPE_BUZZER:     return "Buzzer #"+port;
				default:                 return "Unknown port #" + type + ":" + port;
			}
		}
		
		private DigitalOutputListener listener;
		public DigitalOutput(DigitalOutputListener listener) {
			super(COMMAND_IO);
			this.listener = listener;
		}
		@Override
		public void handle(DataInputStream in, int inLength, RexProtocol comm) throws IOException {
			int tipo = Util.readInt(in); //Rel�=1, Led=2, Backlight=3, Buzina=4
			int qual = Util.readInt(in);
			int timeOn = Util.readInt(in);
			int timeOff = Util.readInt(in);
			int repeats = Util.readInt(in);
			listener.toggleDigitalOutput(comm, tipo, qual, timeOn, timeOff, repeats);
		}
		public static void send(RexProtocol comm, int tipo, int qual, int timeOn, int timeOff, int repeats) throws IOException {
			ByteArrayOutputStream cmdOut = new ByteArrayOutputStream();
			Util.writeInt(cmdOut, tipo);
			Util.writeInt(cmdOut, qual);
			Util.writeInt(cmdOut, timeOn);
			Util.writeInt(cmdOut, timeOff);
			Util.writeInt(cmdOut, repeats);		
			comm.sendCommand(COMMAND_IO, cmdOut.toByteArray());
		}
	}
		
	
	///////////////////////////////////////////////////////////////////////////////////////////////	
	public static class InputChange extends MessageHandler {
		private InputListener listener;
		public InputChange(InputListener listener) {
			super(COMMAND_INPUT_CHANGED);
			this.listener = listener;
		}
		@Override
		public void handle(DataInputStream in, int inLength, RexProtocol comm) throws IOException {
			int     port = Util.readInt(in);
			boolean isOn = Util.readInt(in) != 0;
			listener.inputChanged(comm, port, isOn);
		}
		public static void send(RexProtocol comm, int port, boolean isOn) throws IOException {
			ByteArrayOutputStream cmdOut = new ByteArrayOutputStream();
			Util.writeInt(cmdOut, port);
			Util.writeInt(cmdOut, isOn?1:0);		
			comm.sendCommand(COMMAND_INPUT_CHANGED, cmdOut.toByteArray());
		}
	}
	
	
	///////////////////////////////////////////////////////////////////////////////////////////////	
	public static class KeyTyped extends MessageHandler {
		private InputListener listener;
		public KeyTyped(InputListener listener) {
			super(COMMAND_KEY_TYPED);
			this.listener = listener;
		}
		@Override
		public void handle(DataInputStream in, int inLength, RexProtocol comm) throws IOException {
			int keyCode = Util.readInt(in);
			listener.keyTyped(comm, keyCode);
		}
		public static void send(RexProtocol comm, int keyCode) throws IOException {
			ByteArrayOutputStream cmdOut = new ByteArrayOutputStream();
			Util.writeInt(cmdOut, keyCode);		
			comm.sendCommand(COMMAND_KEY_TYPED, cmdOut.toByteArray());
		}
	}
	
	
	///////////////////////////////////////////////////////////////////////////////////////////////	
	public static class ImageCapture extends MessageHandler {
		private ImageCaptureListener listener;
		public ImageCapture(ImageCaptureListener listener) {
			super(COMMAND_IMAGE_ACQUIRED);
			this.listener = listener;
		}
		@Override
		public void handle(DataInputStream in, int inLength, RexProtocol comm) throws IOException {
			int width  = Util.readInt(in);
			int height = Util.readInt(in);
			int resX   = Util.readInt(in);
			int resY   = Util.readInt(in);
			byte[] imgBuf = new byte[width*height];
			in.readFully(imgBuf);
			byte[] nameBytes = new byte[inLength - 16 - width*height];
			in.readFully(nameBytes);
			String name = Util.StringFromBytes(nameBytes);
			listener.imageCaptured(comm, width, height, resX, resY, imgBuf, name);
		}
		public static void send(RexProtocol comm, String reader, int width, int height, int resX, int resY, byte[] buffer) throws IOException {
			ByteArrayOutputStream cmdOut = new ByteArrayOutputStream();
			Util.writeInt(cmdOut, width);
			Util.writeInt(cmdOut, height);
			Util.writeInt(cmdOut, resX);
			Util.writeInt(cmdOut, resY);	
			
			comm.sendCommand(COMMAND_IMAGE_ACQUIRED, cmdOut.toByteArray(), buffer, Util.StringToBytes(reader));
		}
	}
}
