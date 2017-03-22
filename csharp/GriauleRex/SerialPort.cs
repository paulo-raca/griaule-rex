using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace GriauleRex {

	public class SerialPort {

		public readonly RexDevice Rex;
		public readonly int Index;
		private Stream _stream;

		public enum Parity {
			None = 0,
			Odd = 1,
			Even = 2
		};

		public enum FlowControl {
			None = 0,
			Software = 1,
			Hardware = 2
		};

		internal SerialPort(RexDevice rex, int index) {
			this.Rex = rex;
			this.Index = index;
			this._stream = null;
		}

		public Stream Open(int baud=9600, int bits=8, Parity parity=Parity.None, int stopBits=1, FlowControl flowControl=FlowControl.None) {
			if (_stream != null) {
				throw new IOException ("Serial port already open");
			} else {
				_stream = new SerialPortStream (this);

				MemoryStream streamMode = new MemoryStream (16);
				streamMode.WriteInt (Index);
				streamMode.WriteInt (1); //Asynchronous
				streamMode.WriteInt (1); //Binary
				streamMode.WriteInt (1); //1 byte package -- TODO, create a mode where callbacks return as much data as possible, without blocking
				Rex.SendMessage (RexDevice.COMMAND_RS232_SET_MODE, streamMode.GetBuffer ());

				MemoryStream streamOpen = new MemoryStream (24);
				streamOpen.WriteInt (Index);
				streamOpen.WriteInt (baud);
				streamOpen.WriteInt ((int)parity);
				streamOpen.WriteInt (bits);
				streamOpen.WriteInt (stopBits);
				streamOpen.WriteInt ((int)flowControl);
				Rex.SendMessage (RexDevice.COMMAND_RS232_OPEN, streamOpen.GetBuffer ());

				return _stream;
			}
		}

		public override String ToString() {
			return "[Serial Port #" + Index + " at " + this.Rex + "]";
		}



		private class SerialPortStream : Stream {

			public readonly SerialPort Serial;
			private bool _closed = false;
			internal Queue<byte> _inputBuffer = new Queue<byte> ();

			public override bool CanRead { get { return true; } }
			public override bool CanWrite { get { return true; } }
			public override bool CanSeek { get { return false; } }
			public override bool CanTimeout { get { return false; } }
			public override long Length { get { throw new NotSupportedException (); } }
			public override long Position { get { throw new NotSupportedException (); } set { throw new NotSupportedException (); } }
			public override int ReadTimeout { get { throw new InvalidOperationException (); } }
			public override int WriteTimeout { get { throw new InvalidOperationException (); } }

			public SerialPortStream(SerialPort Serial) {
				this.Serial = Serial;
			}

			public override int Read(byte[] buffer, int offset, int count) {
				lock (_inputBuffer) {
					while (true) {
						if (_closed) {
							throw new ObjectDisposedException ("Stream closed");
						} else if (_inputBuffer.Count == 0) {
							Monitor.Wait (_inputBuffer);
						} else {
							int ret = 0;
							for (int i = offset; i < offset + count && _inputBuffer.Count > 0; i++) {
								buffer [i] = _inputBuffer.Dequeue ();
								ret++;
							}
							return ret;
						}
					}
				}
			}

			public override void Write(byte[] buffer, int offset, int count) {
				if (_closed) {
					throw new ObjectDisposedException ("Stream closed");
				}
				MemoryStream stream = new MemoryStream (count + 4);
				stream.WriteInt (Serial.Index);
				stream.Write (buffer, offset, count);
				Serial.Rex.SendMessage (RexDevice.COMMAND_RS232_WRITE, stream.GetBuffer ());
			}

			public override void Flush() {
				//No output buffering :)
			}

			public override long Seek(long offset, SeekOrigin origin) {
				throw new NotSupportedException ();
			}

			public override void SetLength(long value) {
				throw new NotSupportedException ();
			}

			protected override void Dispose(bool disposing) {
				if (_closed) {
					return;
				}

				try {
					//Send RS232 Close command
					MemoryStream stream = new MemoryStream (4);
					stream.WriteInt (Serial.Index);
					Serial.Rex.SendMessage (RexDevice.COMMAND_RS232_CLOSE, stream.GetBuffer ());
				} catch (ObjectDisposedException) {
					//rex disconnected ?
				}

				_closed = true;
				Serial._stream = null;
				lock (_inputBuffer) {
					Monitor.PulseAll (_inputBuffer);
				}
			}
		};
	}
}
