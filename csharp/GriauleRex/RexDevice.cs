using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace GriauleRex {
	public class RexDevice : RexProtocol, IDisposable {

		private object sendLock = new object ();
		private Stream stream;
		internal Task ReceiveLoopTask;

		public String Id { get; private set; }
		public RexFeatures Features { get; private set; }

		public IReadOnlyList<SerialPort> SerialPorts  { get; private set; }
		public IReadOnlyList<DigitalInput> DigitalInputs  { get; private set; }
		public IReadOnlyList<DigitalOutput> Relays  { get; private set; }
		public IReadOnlyList<DigitalOutput> Leds  { get; private set; }
		public DigitalOutput Buzzer  { get; private set; }
		public RexDisplay Display  { get; private set; }

		public event Action Connected;
		public event Action Disconnected;
		public bool IsConnected { get; private set; }

		public event Action<char> KeyTyped;
		public event Action<String, Bitmap> FingerprintCaptured;

		public RexDevice(Stream stream) {
			this.stream = stream;
			this.IsConnected = true;
		}

		public override String ToString() {
			return "[Rex " + Id + (IsConnected ? "]" : " (Offline)]");
		}

		public void Dispose() {
			stream.Close ();
			ReceiveLoopTask.Wait ();
		}

		internal async Task InitializeAsync () {
			this.ReceiveLoopTask = ReceiveLoopAsync ();

			await SendMessageAndWaitResponseAsync (COMMAND_ID_REQUEST, COMMAND_ID_RESPONSE, new byte[0], 
				(id) => {
					this.Id = STRING_ENCODING.GetString(id);
					return this.Id;
				}
			);

			await SendMessageAndWaitResponseAsync (COMMAND_FEATURES_REQUEST, COMMAND_FEATURES_RESPONSE, new byte[0], 
				(raw) => {
					this.Features = new RexFeatures (raw);


					List<DigitalInput> digitalInputs = new List<DigitalInput> ();
					for (int i = 0; i < this.Features.NumDigitalInputs; i++) {
						digitalInputs.Add(new DigitalInput(this, i));
					}
					this.DigitalInputs = digitalInputs.AsReadOnly ();
					this.RawListeners[COMMAND_INPUT_CHANGED] += (payload) => {
						Stream stream = new MemoryStream(payload);
						int index = stream.ReadInt();
						bool value = stream.ReadInt() != 0;
						this.DigitalInputs[index].Value = value;
					};

					this.RawListeners[COMMAND_KEY_TYPED] += (payload) => {
						Stream stream = new MemoryStream(payload);
						int key = stream.ReadInt();
						if (this.KeyTyped != null) {
							this.KeyTyped((char)key);
						}
					};

					this.RawListeners[COMMAND_IMAGE_ACQUIRED] += (payload) => {
						Stream stream = new MemoryStream(payload);
						int width = stream.ReadInt();
						int height = stream.ReadInt();
						int resX = stream.ReadInt();
						int resY = stream.ReadInt();
						byte[] rawImage = stream.ReadFully(width * height);
						String scanner = STRING_ENCODING.GetString(stream.ReadFully((int)(stream.Length - stream.Position)));

						//Create a grayscale bitmap -- DotNet sucks, therefore it uses a pallete
						Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
						bitmap.SetResolution(resX, resY);

						int i = 0;
						for (int y = 0; y < height; y++) {
							for (int x = 0; x < width; x++) {
								byte v = rawImage[i++];
								bitmap.SetPixel(x, y, Color.FromArgb(v, v, v));
							}
						}

						if (FingerprintCaptured != null) {
							FingerprintCaptured(scanner, bitmap);
						}

					};


					List<DigitalOutput> relays = new List<DigitalOutput> ();
					for (int i = 0; i < this.Features.NumRelays; i++) {
						relays.Add(new DigitalOutput(this, DigitalOutput.DigitalOutputType.Relay, i));
					}
					this.Relays = relays.AsReadOnly ();


					List<DigitalOutput> leds = new List<DigitalOutput> ();
					for (int i = 0; i < this.Features.NumLeds; i++) {
						leds.Add(new DigitalOutput(this, DigitalOutput.DigitalOutputType.Led, i));
					}
					this.Leds = leds.AsReadOnly ();


					List<SerialPort> serials = new List<SerialPort> ();
					for (int i = 0; i < this.Features.NumSerialPorts; i++) {
						serials.Add(new SerialPort(this, i));
					}
					this.SerialPorts = serials.AsReadOnly ();


					this.Buzzer = this.Features.HasBuzzer ? new DigitalOutput (this, DigitalOutput.DigitalOutputType.Buzzer, 0) : null;


					this.Display = this.Features.HasDisplay ? new RexDisplay (this) : null;

					return this.Features;
				}
			);
			if (Connected != null) {
				Connected ();
			}
			await ReceiveLoopTask;
			if (Disconnected != null) {
				Disconnected ();
			}
		}

		private async Task ReceiveLoopAsync()  {
			try {
				while (true) {
					byte[] rawMsg;
					try {
						int len = await stream.ReadIntAsync();
						rawMsg = await stream.ReadFullyAsync(len-4);
					} catch (Exception) {
						break;
					}
					parseRawMessage(rawMsg);
				}
			} finally {
				this.IsConnected = false;
				stream.Close ();
			}
		}

		private void SendMessage(int cmd, byte[] payload)  {
			MemoryStream stream = new MemoryStream (payload.Length + 16);
			stream.WriteInt (payload.Length + 16);
			stream.WriteInt (PROTOCOL_PREFIX);
			stream.WriteInt (cmd);
			stream.WriteInt (payload.Length);
			stream.WriteFully (payload);

			lock (sendLock) {
				this.stream.WriteFully (stream.GetBuffer ());
			}
		}

		private async Task<T> SendMessageAndWaitResponseAsync<T>(int cmdRequest, int cmdResponse, byte[] payload, Func<byte[], T> handler)  {
			Task<T> ret = WaitResponseAsync (cmdResponse, handler);
			SendMessage (cmdRequest, payload);
			return await ret;
		}




		public void Reset() {
			SendMessage (COMMAND_RESET, new byte[0]);
		}




		public struct RexFeatures {
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

			public RexFeatures(byte[] payload) {
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




		public class DigitalInput {
			public readonly RexDevice Rex;
			public readonly int Index;
			private bool _value;

			public delegate void InputChangedEventHandler(bool newValue);
			public event InputChangedEventHandler InputChanged;

			public DigitalInput(RexDevice rex, int index) {
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

			public SerialPort(RexDevice rex, int index) {
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
				//public override bool CanWrite = true;

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

			public DigitalOutput(RexDevice rex, DigitalOutputType type, int index) {
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
						toggle (1, 0, 0);
					} else {
						toggle (0, 1, 0);
					}
				}
			}

			public void Hold(int timeOn) {
				toggle (timeOn, 0, 1);
			}

			public void toggle(int timeOn, int timeOff, int repeats=0) {
				MemoryStream stream = new MemoryStream (20);
				stream.WriteInt ((int)Type);
				stream.WriteInt (Index);
				stream.WriteInt (timeOn);
				stream.WriteInt (timeOff);
				stream.WriteInt (repeats);

				Rex.SendMessage (RexDevice.COMMAND_IO, stream.GetBuffer ());
			}
		}




		public class RexDisplay {

			public readonly RexDevice Rex;

			public readonly DigitalOutput Backlight;

			public RexDisplay(RexDevice rex) {
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
				byte[] textBytes = STRING_ENCODING.GetBytes (text);
				MemoryStream stream = new MemoryStream (12 + textBytes.Length);
				stream.WriteInt (row);
				stream.WriteInt (col);
				stream.WriteInt (0); //?
				stream.WriteFully(textBytes);
				this.Rex.SendMessage (RexDevice.COMMAND_DISPLAY_WRITE, stream.GetBuffer());
			}
		}
	}
}
