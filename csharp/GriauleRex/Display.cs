using System;
using System.IO;

namespace GriauleRex {

	public class Display {

		public readonly RexDevice Rex;
		public readonly DigitalOutput Backlight;

		internal Display(RexDevice rex) {
			this.Rex = rex;
			this.Backlight = new DigitalOutput(rex, DigitalOutput.DigitalOutputType.DisplayBacklight, 0);
		}

		public override String ToString() {
			return "[Display at " + this.Rex + "]";
		}

		public void Clear() {
			this.Rex.SendMessage (RexDevice.COMMAND_DISPLAY_CLEAR, new byte[0]);
		}

		public void Initialize(int width=16, int height=2, bool font5x10=false) {
			MemoryStream stream = new MemoryStream (16);
			stream.WriteInt (width);
			stream.WriteInt (height);
			stream.WriteInt (8); // Bus width, ignored
			stream.WriteInt (font5x10 ? 1 : 0);
			this.Rex.SendMessage (RexDevice.COMMAND_DISPLAY_INITIALIZE, stream.GetBuffer());
		}

		public void SetEntryMode(bool moveMessage=false, bool toRight=false) {
			MemoryStream stream = new MemoryStream (4);
			stream.WriteInt ( 
				4
				| (toRight ? 2 : 0) 
				| (moveMessage ? 1 : 0) 
			);
			this.Rex.SendMessage (RexDevice.COMMAND_DISPLAY_SET_ENTRY_MODE, stream.GetBuffer());
		}

		public void SetCursor(bool displayOn=true, bool cursonOn=false, bool cursorBlinking=false) {
			MemoryStream stream = new MemoryStream (4);
			stream.WriteInt (
				(displayOn ? 4 : 0)
				| (cursonOn ? 2 : 0)
				| (cursorBlinking ? 1 : 0)
			);
			this.Rex.SendMessage (RexDevice.COMMAND_DISPLAY_SET_CURSOR, stream.GetBuffer());
		}

		public void Write(String text) {
			WriteAt (text, -1, -1);
		}

		public void WriteAt(String text, int row, int col) {
			byte[] textBytes = RexProtocol.STRING_ENCODING.GetBytes (text);
			MemoryStream stream = new MemoryStream (12 + textBytes.Length);
			stream.WriteInt (row);
			stream.WriteInt (col);
			stream.WriteInt (0); //?
			stream.WriteFully(textBytes);
			this.Rex.SendMessage (RexDevice.COMMAND_DISPLAY_WRITE, stream.GetBuffer());
		}
	}
}
