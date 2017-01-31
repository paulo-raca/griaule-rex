package veridis.embedded.rex.server;
import java.io.IOException;
import java.net.ServerSocket;
import java.net.Socket;
import java.util.concurrent.Semaphore;

import veridis.embedded.rex.RexProtocol;


public class RexServerDiscovery {	
	RexServer rex;
	RexProtocol.UDP protocol;
	ServerSocket serverSocket;

	public RexServerDiscovery(final RexServer rex, int portNumber) throws IOException {
		this.rex = rex;
		this.protocol = new RexProtocol.UDP(rex, RexProtocol.UDP.PORT_DISCOVERY);
		this.serverSocket = new ServerSocket(portNumber);

		//Recebe as respostas de ConnectionRequest
		Thread threadReceiveDiscovery = new Thread("JA200-Discovery-Receive") {
			public void run() {
				try {
					protocol.handleProtocol();
				} catch (IOException e) {
					e.printStackTrace();
				}
			};
		};
		threadReceiveDiscovery.start();
		
		
		//Recebe as respostas de ConnectionRequest
		Thread threadReceiveConnections = new Thread("JA200-Receive-Connections") {
			public void run() {
				try {
					while (true) {
						final Socket socket = serverSocket.accept();
						final RexProtocol.TCP conn = new RexProtocol.TCP(rex, socket);
						
						final Semaphore sem = new Semaphore(0);
						
						new Thread("JA200-Communication-With-" + socket.getRemoteSocketAddress()) {
							public void run() {
								try {
									conn.handleProtocol();
								} catch (Exception e) {
									e.printStackTrace();
								}
								sem.release();
							};
						}.start();
					
						new Thread("JA200-Connection-To-" + socket.getRemoteSocketAddress()) {
							public void run() {
								try {
									rex.connectionStarted(conn);
								} catch (Exception e) {}
								try {
									sem.acquire();
									rex.connectionFinished(conn);
								} catch (Exception e) {}
							};
						}.start();
					}
				} catch (IOException e) {}
			};
		};
		threadReceiveConnections.start();
	}
}
