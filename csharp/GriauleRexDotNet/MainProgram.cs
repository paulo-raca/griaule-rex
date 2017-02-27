using System;
using System.Net;
using System.Threading;
using System.Net.NetworkInformation;

namespace GriauleRexDotNet {
	
	public static class MainProgram {
		
		private static bool OnRexDiscovered(String name, PhysicalAddress mac, IPAddress ip, IPAddress mask, IPAddress gateway) {
			Console.WriteLine ("Received discovery from " + name + " / " + mac + " / " + ip + " / " + mask + " / " + gateway);
			return true; // Request a connection
		}

		public static void Main (string[] args) {
			using (RexClient rexClient = new RexClient ()) {
				rexClient.RexDiscovered += OnRexDiscovered;
				Thread.Sleep(500000);
				Console.WriteLine ("Closing");
			}
			Thread.Sleep(5000);
		}

	}

}

