using System;
using System.IO;

namespace GriauleRex {

	public class Features {

		public readonly RexDevice Rex;
		public readonly int NumDigitalInputs;
		public readonly int NumRelays;
		public readonly int NumLeds;
		public readonly int NumSerialPorts;
		public readonly int NumFingerprintScanners;
		public readonly bool HasKeyboard;
		public readonly bool HasDisplay;
		public readonly bool HasBuzzer;
		public readonly bool HasMP3;
		public readonly Version Version;

		internal Features(RexDevice rex, byte[] payload) {
			this.Rex = rex;

			Stream stream = new MemoryStream(payload);
			this.NumRelays = stream.ReadInt();
			this.NumSerialPorts = stream.ReadInt();
			this.NumDigitalInputs = stream.ReadInt();
			this.NumLeds = stream.ReadInt();
			this.NumFingerprintScanners = stream.ReadInt();
			this.HasKeyboard = stream.ReadInt() > 0;
			this.HasDisplay = stream.ReadInt() > 0;
			this.HasBuzzer = stream.ReadInt() > 0;
			this.HasMP3 = stream.ReadInt() > 0;

			byte[] versionInts = stream.ReadFully(4);
			this.Version = new Version(versionInts[3], versionInts[2], versionInts[1], versionInts[0]);
		}

		public override String ToString() {
			return "[Rex Features: Digital Inputs=" + NumDigitalInputs + ", Relays=" + NumRelays + ", Leds=" + NumLeds + ", Serial Ports=" + NumSerialPorts + ", Fingerprint Scanners=" + NumFingerprintScanners + ", Keyboard=" + HasKeyboard + ", Display=" + HasDisplay + ", Buzzer=" + HasBuzzer + ", MP3=" + HasMP3 + "]";
		}
	}
}
