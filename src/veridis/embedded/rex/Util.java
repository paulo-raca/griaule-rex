package veridis.embedded.rex;
import java.io.DataInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.io.UnsupportedEncodingException;


public class Util {
	public static final String EVENT_PREFIX = "=> ";
	public static final String CMD_PREFIX   = "$ ";
	public static final String ERR_PREFIX   = ">>> ERR >>> ";	
	
	/**Charset usado para transmitir Strings nas mensagens: ISO-8859-1*/
	//public static final Charset ISO_LATIN1 = Charset.availableCharsets().get("ISO-8859-1");
	public static final String StringFromBytes(byte[] data) {
		try {
			return new String(data, "ISO-8859-1");
		} catch (UnsupportedEncodingException e) {
			throw new RuntimeException(e);
		}
	}
	public static final byte[] StringToBytes(String s) {
		try {
			return s.getBytes("ISO-8859-1");
		} catch (UnsupportedEncodingException e) {
			throw new RuntimeException(e);
		}
	}
	
	public static void prettyDump(DataInputStream in, int len) throws IOException {
		byte[] b = new byte[len];
		in.readFully(b);
		prettyDump(b);
	} 
	public static void prettyDump(byte[] buf) {
		prettyDump(buf, buf.length);
	}
	public static void prettyDump(byte[] buf, int len) {
		for (int i=0; i<len; i++) {
			System.out.format("%1$02x ", buf[i]);
		}
		System.out.println();
		for (int i=0; i<len; i++) {
			System.out.print((char)buf[i] + "  ");
		}
		System.out.println();
	}
	public static byte[] formatID(String name)  {
		byte[] idRaw = StringToBytes(name);
		byte[] id = new byte[12];
		for (int i=0; i<id.length; i++) {
			if (i < idRaw.length)
				id[i] = idRaw[i];
			else
				id[i] = '_';
		}
		return id;
	}
	public static String macString(byte[] mac)  {
		return StringFromBytes(macChars(mac));
	}
	private static byte[] hexChars = StringToBytes("0123456789ABCDEF");
	public static byte[] macChars(byte[] mac)  {
		 byte[] macStr = new byte[12];
		 for (int i=0; i<6; i++) {
			 macStr[2*i  ] = hexChars[(mac[i] >> 4) & 0xF];
			 macStr[2*i+1] = hexChars[(mac[i] >> 0) & 0xF];
		 }
		 return macStr;
	}
	public static String macStringPretty(byte[] mac)  {
		 String macStr = "";
		 for (int i=0; i<6; i++) {
			 macStr+= (char)hexChars[(mac[i] >> 4) & 0xF];
			 macStr+= (char)hexChars[(mac[i] >> 0) & 0xF];
			 if (i<5) macStr+= ":";
		 }
		 return macStr;
	}
	
	public static int readByte(InputStream in) throws IOException {
		int b = in.read();
		if (b<0) throw new IOException("End of Stream");
		return b;
	}
	
	
	public static int readInt(InputStream in) throws IOException {
		return readByte(in) | (readByte(in)<<8) | (readByte(in)<<16) | (readByte(in)<<24);
	}
	public static void writeInt(OutputStream out, int v) throws IOException {
		out.write( (v>>  0) & 0xFF);
		out.write( (v>>  8) & 0xFF);
		out.write( (v>> 16) & 0xFF);
		out.write( (v>> 24) & 0xFF);
	}
}
