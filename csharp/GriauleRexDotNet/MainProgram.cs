using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace GriauleRexDotNet {
	
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

