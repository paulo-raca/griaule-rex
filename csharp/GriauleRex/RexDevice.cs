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
		private Task ReceiveLoopTask;

		public String Id { get; private set; }
		public Features Features { get; private set; }

		public IReadOnlyList<SerialPort> SerialPorts  { get; private set; }
		public IReadOnlyList<DigitalInput> DigitalInputs  { get; private set; }
		public IReadOnlyList<DigitalOutput> Relays  { get; private set; }
		public IReadOnlyList<DigitalOutput> Leds  { get; private set; }
		public DigitalOutput Buzzer  { get; private set; }
		public Display Display  { get; private set; }

		public event Action Connected;
		public event Action Disconnected;
		public bool IsConnected { get; private set; }

		public event Action<char> KeyTyped;
		public event Action<String, Bitmap> FingerprintCaptured;

		public RexDevice(Stream stream) {
			this.stream = stream;
			this.IsConnected = true;
		}

		public void Reset() {
			SendMessage (COMMAND_RESET, new byte[0]);
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
					this.Features = new Features (this, raw);


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


					this.Display = this.Features.HasDisplay ? new Display (this) : null;

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

		internal void SendMessage(int cmd, byte[] payload)  {
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
	}
}
