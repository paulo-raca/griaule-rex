using System.IO;
using System.Threading.Tasks;

namespace GriauleRex {
	
	internal static class StreamHack {
		
		public static int ReadInt(this Stream stream) {
			byte[] bytes = stream.ReadFully(4);
			return (((int)bytes[0])<<0) | (((int)bytes[1])<<8) | (((int)bytes[2])<<16) | (((int)bytes[3])<<24);
		}

		public static void WriteInt(this Stream stream, int value) {
			byte[] bytes = {
				(byte)(value >> 0),
				(byte)(value >> 8),
				(byte)(value >> 16),
				(byte)(value >> 24)
			};
			stream.WriteFully(bytes);
		}

		public static async Task<int> ReadIntAsync(this Stream stream) {
			byte[] bytes = await stream.ReadFullyAsync(4);
			return (((int)bytes[0])<<0) | (((int)bytes[1])<<8) | (((int)bytes[2])<<16) | (((int)bytes[3])<<24);
		}

		public static async Task WriteIntAsync(this Stream stream, int value) {
			byte[] bytes = {
				(byte)(value >> 0),
				(byte)(value >> 8),
				(byte)(value >> 16),
				(byte)(value >> 24)
			};
			await stream.WriteFullyAsync(bytes);
		}

		public static byte[] ReadFully(this Stream stream, int len) {
			byte[] buffer = new byte[len];
			int offset = 0;
			int readBytes = 0;
			do {
				readBytes = stream.Read(buffer, offset, buffer.Length - offset);
				offset += readBytes;
			} while (readBytes > 0 && offset < buffer.Length);

			if (offset < buffer.Length) {
				throw new EndOfStreamException();
			}
			return buffer;
		}

		public static void WriteFully(this Stream stream, byte[] value) {
			stream.Write (value, 0, value.Length);
		}

		public static async Task<byte[]> ReadFullyAsync(this Stream stream, int len) {
			byte[] buffer = new byte[len];
			int offset = 0;
			int readBytes = 0;
			do {
				readBytes = await stream.ReadAsync(buffer, offset, buffer.Length - offset);
				offset += readBytes;
			} while (readBytes > 0 && offset < buffer.Length);

			if (offset < buffer.Length) {
				throw new EndOfStreamException();
			}
			return buffer;
		}

		public static async Task WriteFullyAsync(this Stream stream, byte[] value) {
			await stream.WriteAsync (value, 0, value.Length);
		}
	}
}
