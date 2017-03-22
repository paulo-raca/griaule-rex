using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GriauleRex {

	public abstract class RexProtocol {
		
		internal static Encoding STRING_ENCODING = Encoding.ASCII;

		internal const int PROTOCOL_PREFIX = 0x30584552; // "REX0" as a integer, for convenience

		internal const int COMMAND_DISCOVERY          = 0x01;
		internal const int COMMAND_CONNECTION_REQUEST = 0x02;
		internal const int COMMAND_FEATURES_REQUEST   = 0x0a;
		internal const int COMMAND_FEATURES_RESPONSE  = 0x0b;
		internal const int COMMAND_RESET              = 0x0c;
		internal const int COMMAND_ID_REQUEST         = 0x0d;
		internal const int COMMAND_ID_RESPONSE        = 0x0e;

		internal const int COMMAND_IO                 = 0x12;
		internal const int COMMAND_INPUT_CHANGED      = 0x3c;
		internal const int COMMAND_IMAGE_ACQUIRED     = 0x46;
		internal const int COMMAND_KEY_TYPED          = 0x50;

		internal const int COMMAND_DISPLAY_INITIALIZE       = 0x1e;
		internal const int COMMAND_DISPLAY_CLEAR            = 0x1f;
		internal const int COMMAND_DISPLAY_SET_ENTRY_MODE   = 0x21;
		internal const int COMMAND_DISPLAY_SET_CURSOR       = 0x23;
		internal const int COMMAND_DISPLAY_MOVE             = 0x24;
		internal const int COMMAND_DISPLAY_WRITE            = 0x28;

		internal const int COMMAND_RS232_OPEN       = 0x32;
		internal const int COMMAND_RS232_SET_MODE   = 0x33;
		internal const int COMMAND_RS232_READ       = 0x34;
		internal const int COMMAND_RS232_WRITE      = 0x35;
		internal const int COMMAND_RS232_CLOSE      = 0x36;

		protected delegate void RawMessageReceivedEventHandler(byte[] args);

		protected DefaultDictionary<int, RawMessageReceivedEventHandler> RawListeners =
			new DefaultDictionary <int, RawMessageReceivedEventHandler>();

		protected void parseRawMessage(byte[] rawMessage) {
			MemoryStream stream = new MemoryStream (rawMessage);

			int protocol = stream.ReadInt();
			if (protocol != PROTOCOL_PREFIX) {
				throw new IOException("Invalid message header, expected REX0");
			}		

			int cmd = stream.ReadInt();
			int cmdLen = stream.ReadInt();
			if (cmdLen != stream.Length - 12) {
				throw new IOException ("Invalid message size");
			}

			byte[] payload = stream.ReadFully (cmdLen);

			RawMessageReceivedEventHandler listener = RawListeners [cmd];
			if (listener != null) {
				listener (payload);
			} else {
				Console.WriteLine (this + ": Unhandled message " + cmd);
			}
		}

		protected async Task<T> WaitResponseAsync<T>(int cmd, Func<byte[], T> handler) {
			TaskCompletionSource<T> task = new TaskCompletionSource<T>();
			RawMessageReceivedEventHandler wrapper = null;
			wrapper = (payload) => {
				RawListeners [cmd] -= wrapper;
				task.SetResult(handler(payload));
			};
			RawListeners [cmd] += wrapper;
			return await task.Task;
		}



		protected class DefaultDictionary<TKey, TValue> : Dictionary<TKey, TValue> {

			public new TValue this[TKey key] {
				get {
					TValue val;
					if (!TryGetValue(key, out val)) {
						val = default(TValue);
						Add(key, val);
					}
					return val;
				}

				set { 
					base[key] = value; 
				}
			}
		}
	}
}
