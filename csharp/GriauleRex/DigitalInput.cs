using System;

namespace GriauleRex {

	public class DigitalInput {

		public readonly RexDevice Rex;
		public readonly int Index;
		private bool _value;

		public event Action<bool> InputChanged;

		internal DigitalInput(RexDevice rex, int index) {
			this.Rex = rex;
			this.Index = index;
			this.Value = false;
		}

		public override String ToString() {
			return "[Digital Input #" + Index + " = " + Value + " at " + this.Rex + "]";
		}

		public bool Value {
			get {
				return _value;
			}
			internal set {
				_value = value;
				if (InputChanged != null) {
					InputChanged (value);
				}
			}
		}
	}
}
