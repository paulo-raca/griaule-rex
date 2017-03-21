using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using GriauleRex;

namespace Sample {
	
	public static class MainProgram {

		public static void Main (string[] args) {
			using (RexClient rexClient = new RexClient ()) {

				rexClient.Discovered += (name, mac, ip, mask, gateway) => {
					Console.WriteLine ("Received discovery from " + name + " / " + mac + " / " + ip + " / " + mask + " / " + gateway);
					rexClient.RequestConnection(ip);
				};

				rexClient.Connected += (rex) => {
					Console.WriteLine ("Connected to " + rex);
					Console.WriteLine ("Features: " + rex.Features);
					Console.WriteLine ("Inputs: " + String.Join(", ", rex.DigitalInputs));
					Console.WriteLine ("Relays: " + String.Join(", ", rex.Relays));
					Console.WriteLine ("Leds: " + String.Join(", ", rex.Leds));
					Console.WriteLine ("Buzzer: " + rex.Buzzer);
					Console.WriteLine ("Display: " + rex.Display);
					rex.Relays[0].toggle (100, 100, 2);
					rex.Buzzer.toggle(10, 10, 4);
					rex.Display.WriteAt ("Foobar", 1, 1);
					rex.Display.Write ("Bah");
					rex.Display.Backlight.toggle (100, 100);

					foreach (RexDevice.DigitalInput input in rex.DigitalInputs) {
						input.InputChanged += (newValue) => {
							Console.WriteLine ("Input changed: " + input);
						};
					}
					rex.KeyTyped += (key) => {
						Console.WriteLine ("Key typed: " + key);
					};
					rex.FingerprintCaptured += (scanner, fingerprint) => {
						String file = rex.Id + "-" + scanner + ".jpg";
						using (FileStream fs = File.Create(file)) {
							fingerprint.Save(fs, System.Drawing.Imaging.ImageFormat.Jpeg); // Aparently ignores the DPI when saving the image... :(
							Console.WriteLine ("Fingerprint captured: " + file + ", " 
								+ fingerprint.Width + "x" + fingerprint.Height
								+ " @" + (int)Math.Sqrt(fingerprint.HorizontalResolution * fingerprint.VerticalResolution) + "dpi");
						}
					};
					testSerialPorts(rex);
					toggleRandomStuff(rex);
					closeAfter(rex, 10);
				};

				rexClient.Disconnected += (rex) => {
					Console.WriteLine ("Disconnected from " + rex);
				};

				Thread.Sleep(5000);
				Console.WriteLine ("Closing RexClient");
			}
			Thread.Sleep(10000);
		}


		private static async void closeAfter(RexDevice rex, int secs) {
			await Task.Delay (secs * 1000);
			rex.Dispose();
		}

		private static async void testSerialPorts(RexDevice rex) {
			try {
				while (true) {
					foreach (RexDevice.SerialPort serial in rex.SerialPorts) {
						using (Stream stream = serial.Open()) {
							byte[] buffer = Encoding.ASCII.GetBytes("Foo Bar 123\n");
							stream.Write(buffer, 0, buffer.Length);
							stream.Dispose();
						}
						await Task.Delay (500);
					}
				}
			} catch (ObjectDisposedException) {
				Console.WriteLine ("Rex has disconnected, stopping testSerialPorts()");
			}
		}

		private static async void toggleRandomStuff(RexDevice rex) {
			try {
				List<RexDevice.DigitalOutput> pins = new List<RexDevice.DigitalOutput> ();
				pins.AddRange (rex.Relays);
				pins.AddRange (rex.Leds);
				pins.Add (rex.Buzzer);
				pins.Add (rex.Display.Backlight);

				while (true) {
					foreach (RexDevice.DigitalOutput pin in pins) {
						pin.toggle (100, 100, 2);
						await Task.Delay (500);
					}
				}
			} catch (ObjectDisposedException) {
				Console.WriteLine ("Rex has disconnected, stopping toggleRandomStuff()");
			}
		}

	}

}

