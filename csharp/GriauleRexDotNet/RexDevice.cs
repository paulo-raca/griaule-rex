using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace GriauleRexDotNet {
	public class RexDevice : RexProtocol, IDisposable {

		private RexClient rexClient;
		private Stream stream;

		public String Id { get; private set; }
		public RexFeatures Features { get; private set; }

		public IReadOnlyList<DigitalOutput> Relays  { get; private set; }
		public IReadOnlyList<DigitalOutput> Leds  { get; private set; }
		public DigitalOutput Buzzer  { get; private set; }
		public RexDisplay Display  { get; private set; }


		public RexDevice(RexClient rexClient, Stream stream) {
			this.rexClient = rexClient;
			this.stream = stream;
			Console.WriteLine ("Rex Client created!");
			SendMessage(COMMAND_ID_REQUEST, new byte[0]);
			ReceiveLoop ();

			Initialize ();
		}

		public void Dispose() {
			stream.Close ();
			//TODO: await ReceiveLoop
		}

		public async Task Reset() {
			await SendMessage (COMMAND_RESET, new byte[0]);
		}

		private async Task<String> getId() {
			byte[] raw = await SendMessageAndWaitResponse (COMMAND_ID_REQUEST, COMMAND_ID_RESPONSE, new byte[0]);
			return STRING_ENCODING.GetString(raw);
		}
		private async Task<RexFeatures> getFeatures() {
			byte[] raw = await SendMessageAndWaitResponse (COMMAND_FEATURES_REQUEST, COMMAND_FEATURES_RESPONSE, new byte[0]);
			return new RexFeatures (raw);
		}

		private async void Initialize () {
			this.Id = await getId ();
			this.Features = await getFeatures ();

			List<DigitalOutput> relays = new List<DigitalOutput> ();
			for (int i = 0; i < this.Features.NumRelays; i++) {
				relays.Add(new DigitalOutput(this, DigitalOutput.TYPE_RELAY, i));
			}
			this.Relays = relays.AsReadOnly ();

			List<DigitalOutput> leds = new List<DigitalOutput> ();
			for (int i = 0; i < this.Features.NumLeds; i++) {
				leds.Add(new DigitalOutput(this, DigitalOutput.TYPE_LED, i));
			}
			this.Leds = leds.AsReadOnly ();

			this.Buzzer = this.Features.HasBuzzer ? new DigitalOutput (this, DigitalOutput.TYPE_BUZZER, 0) : null;
			this.Display = this.Features.HasDisplay ? new RexDisplay (this) : null;


			await this.Relays [0].toggle (100, 100, 2);
			await this.Buzzer.toggle (10, 10, 5);
			await this.Display.WriteAt ("Foobar", 1, 1);
		}

		private async Task ReceiveLoop()  {
			while (true) {
				try {
					int len = await stream.ReadIntAsync();
					byte[] rawMsg = await stream.ReadFullyAsync(len-4);
					parseRawMessage(rawMsg);

				} catch (ObjectDisposedException) {
					Console.WriteLine ("break RexDevice.ReceiveLoop()");
					break;
				}
			}
		}

		private async Task SendMessage(int cmd, byte[] payload)  {
			MemoryStream stream = new MemoryStream (payload.Length + 16);
			stream.WriteInt (payload.Length + 16);
			stream.WriteInt (PROTOCOL_PREFIX);
			stream.WriteInt (cmd);
			stream.WriteInt (payload.Length);
			stream.WriteFully (payload);

			await this.stream.WriteFullyAsync (stream.GetBuffer ());
		}

		private async Task<byte[]> SendMessageAndWaitResponse(int cmdRequest, int cmdResponse, byte[] payload)  {
			Task<byte[]> ret = WaitResponse (cmdResponse);
			SendMessage (cmdRequest, payload);
			return await ret;
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
		}





		public class DigitalOutput {
			public const int TYPE_RELAY = 1;
			public const int TYPE_LED = 2;
			public const int TYPE_BACKLIGHT = 3;
			public const int TYPE_BUZZER = 4;

			private RexDevice rex;
			private int type;
			private int index;

			public DigitalOutput(RexDevice rex, int type, int index) {
				this.rex = rex;
				this.type = type;
				this.index = index;
			}

			public async Task On(int timeOn=0) {
				if (timeOn == 0) {
					await toggle (1, 0, 0);
				} else {
					await toggle (timeOn, 0, 1);
				}
			}

			public async Task Off() {
				await toggle (0, 1, 0);
			}

			public async Task toggle(int timeOn, int timeOff, int repeats=0) {
				MemoryStream stream = new MemoryStream (20);
				stream.WriteInt (type);
				stream.WriteInt (index);
				stream.WriteInt (timeOn);
				stream.WriteInt (timeOff);
				stream.WriteInt (repeats);

				await rex.SendMessage (RexDevice.COMMAND_IO, stream.GetBuffer ());
			}
		}

		public class RexDisplay {

			private RexDevice rex;

			public RexDisplay(RexDevice rex) {
				this.rex = rex;
			}

			public async Task Clear() {
				await rex.SendMessage (RexDevice.COMMAND_DISPLAY_CLEAR, new byte[0]);
			}

			public async Task Initialize(int width=16, int height=2, bool font5x10=false) {
				MemoryStream stream = new MemoryStream (16);
				stream.WriteInt (width);
				stream.WriteInt (height);
				stream.WriteInt (8); // Bus width, ignored
				stream.WriteInt (font5x10 ? 1 : 0);
				await rex.SendMessage (RexDevice.COMMAND_DISPLAY_INITIALIZE, stream.GetBuffer());
			}

			public async Task SetEntryMode(bool moveMessage=false, bool toRight=false) {
				MemoryStream stream = new MemoryStream (4);
				stream.WriteInt ( 
					4
					| (toRight ? 2 : 0) 
					| (moveMessage ? 1 : 0) 
				);
				await rex.SendMessage (RexDevice.COMMAND_DISPLAY_SET_ENTRY_MODE, stream.GetBuffer());
			}

			public async Task SetCursor(bool displayOn=true, bool cursonOn=false, bool cursorBlinking=false) {
				MemoryStream stream = new MemoryStream (4);
				stream.WriteInt (
					(displayOn ? 4 : 0)
					| (cursonOn ? 2 : 0)
					| (cursorBlinking ? 1 : 0)
				);
				await rex.SendMessage (RexDevice.COMMAND_DISPLAY_SET_CURSOR, stream.GetBuffer());
			}

			public async Task Write(String text) {
				await WriteAt (text, -1, -1);
			}

			public async Task WriteAt(String text, int row, int col) {
				byte[] textBytes = STRING_ENCODING.GetBytes (text);
				MemoryStream stream = new MemoryStream (12 + textBytes.Length);
				stream.WriteInt (row);
				stream.WriteInt (col);
				stream.WriteInt (0); //?
				stream.WriteFully(textBytes);
				await rex.SendMessage (RexDevice.COMMAND_DISPLAY_WRITE, stream.GetBuffer());
			}
		}
	}
}
