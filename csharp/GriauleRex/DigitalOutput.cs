using System;
using System.IO;

namespace GriauleRex {

	public class DigitalOutput {

		public enum DigitalOutputType {
			Relay = 1,
			Led = 2,
			DisplayBacklight = 3,
			Buzzer = 4
		}

		public readonly RexDevice Rex;
		public readonly DigitalOutputType Type;
		public readonly int Index;

		internal DigitalOutput(RexDevice rex, DigitalOutputType type, int index) {
			this.Rex = rex;
			this.Type = type;
			this.Index = index;
		}

		public override String ToString() {
			return "[" + Type + " #" + Index + " at " + this.Rex + "]";
		}

		public bool Value {
			set {
				if (value) {
					Toggle (1, 0, 0);
				} else {
					Toggle (0, 1, 0);
				}
			}
		}

		public void Hold(int timeOn) {
			Toggle (timeOn, 0, 1);
		}

		public void Toggle(int timeOn, int timeOff, int repeats=0) {
			MemoryStream stream = new MemoryStream (20);
			stream.WriteInt ((int)Type);
			stream.WriteInt (Index);
			stream.WriteInt (timeOn);
			stream.WriteInt (timeOff);
			stream.WriteInt (repeats);

			Rex.SendMessage (RexDevice.COMMAND_IO, stream.GetBuffer ());
		}
	}
}
