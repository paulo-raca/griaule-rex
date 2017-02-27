package veridis.embedded.rex;
import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.DataInputStream;
import java.io.IOException;
import java.io.OutputStream;
import java.net.DatagramPacket;
import java.net.DatagramSocket;
import java.net.InetSocketAddress;
import java.net.Socket;
import java.net.SocketAddress;
import java.net.SocketException;
import java.util.Arrays;
import java.util.List;

public abstract class RexProtocol {
	/**
	 * REX PROTOCOL
	 * 
	 * O protocolo é divido em mensagens, sendo bastante simétrico, isto é, tem a mesma 
	 * estrutura em ambas as direções, ainda que mensagens diferentes sejam 
	 * enviadas em cada direção.
	 * 
	 * (Todas os campos inteiros são de 4 bytes Little-Endian)
	 * 
	 * O mesmo protocolo básico é utilizado para comunicações em TCP quanto em UDP.
	 * Mensagens em UDP são utilizadas apenas pelas mensagens de descoberta.
	 * O Rex se conecta à um servidor estabelendo uma conexão TCP, utilizada para trocar as demais mensagens.
	 *
	 * A transmissão de mensagens em TCP e UDP são bastante similares, mas cabem algumas distinções:
	 * - UDP:
	 *     O corpo de um datagrama UDP corresponde à uma mensagem do protocolo Rex.
	 * - TCP:
	 *     Mensagens são enviadas sequencialmente em ambas as direções. 
	 *     Para conseguir separar as mensagens no destino, cada mensagem é prefixada por um inteiro 
	 *     (4 bytes, little-endian) com o tamanho da mensagem. Os (tamanho-4) 
	 *     bytes seguintes correspondem à mensagem.
	 * 
	 * O Primeiro campo da mensagem indica a versão do protocolo. Em todos os casos, 
	 * devem ser 4 bytes com a String "REX0". 
	 * 
	 * O Segundo campo é um inteiro com o código do comando (Comando inicializar display, comando acionar GPIO, etc)
	 * 
	 * O terceiro campo é a quantidade de bytes nos argumentos do comando. Esse argumento é redundante com o tamanho da mensagem - 12. 
	 * 
	 * Em seguida vem os argumentos do comando, o tamanho e significado depende do comando.
	 * 
	 * ===================== Discovery - 0x01 =====================
	 * Direção: Rex=>Servidor (Broadcast), UDP
	 * Enviado em Broadcast UDP, porta 2800, a cada 1 segundo, 
	 * para que todo mundo saiba que o Rex está na rede.
	 * 
	 * Campos:
	 *   - ID: 12 bytes, ASCII 
	 *     Identificador do Rex, mostrado no programa. 
	 *     Por convenção, o ID é o endereço MAC, mas poderia ser outra coisa.
	 *     Exemplo: "001e331d0fb6" é para o MAC 00:1e:33:1d:0f:b6
	 *     
	 *   - MAC Address: 12 bytes, ASCII
	 *     Endereço MAC, mostrado como ASCII.
	 *     Exemplo: "001e331d0fb6" é para o MAC 00:1e:33:1d:0f:b6
	 *   
	 *   - IPv4 Addres: 4 bytes
	 *     Cada byte corresponde a um elemento do IP. 
	 *     Por exemplo, o IP 192.168.1.56 é representado pelos bytes {192, 168, 1, 56}
	 *     
	 *   - IPv4 Mask: 4 bytes
	 *     Mascara do IP, mesma convenção do IP Address
	 *     
	 *   - Gateway: 4 bytes
	 *     Endereço IPv4 do gateway, mesma convenção do IP Address
	 *
	 *
	 *
	 *
	 *
	 * ===================== ConnectionRequest - 0x02 =====================
	 * Direção: Servidor => Rex, UDP porta 1025
	 * Quando o servidor recebe um pacote de descoberta de 
	 * um Rex com o qual ele queira se conectar, ele irá responder 
	 * com esta mensagem, contendo um par IP:porta para que a conexão TCP seja estabelecida.
	 * 
	 * A mensagem será enviada para o IP do Rex desejado, na porta 1025
	 * Campos:
	 *   - IP: 4 bytes
	 *     Endereço IP no qual o Rex deve se conectar. Este IP está na ordem contrária da usada no pacote de descoberta (?!).
	 *     Por exemplo: 192.168.1.56 vira {56, 1, 168, 192}
	 *     
	 *   - Porta: inteiro (4 bytes, little endian)
	 *     Porta TCP na qual o servidor está escutando.
	 *     
	 *     
	 *
	 *     
	 *     
	 * ===================== Feature Request - 0x0A =====================
	 * Direção: Servidor => Rex, TCP
	 * O servidor manda esta mensagem pedindo as configurações do Rex (Número de portas de entrada, relés, leitores biométricos, etc)
	 * O rex deverá responder com um "FeatureResponse".
	 * Esta mensagem não tem campos
	 * 
	 * 
	 * 
	 * 
	 * 
	 * ===================== Feature Response - 0x0B =====================
	 * Direção: Rex => Servidor, TCP
	 * Resposta ao "FeatureRequest".
	 * A mensagem é composta por vários campos, todos eles inteiros de 4 bytes:
	 * Campos:
	 * - Número de Relés
	 * - Número de portas seriais 
	 * - Número de entradas digitais
	 * - Número de Leds
	 * - Número de leitores biométricos
	 * - Presença de teclado (1 se tem, 0 caso contrário)
	 * - Presença de display (1 se tem, 0 caso contrário)
	 * - Presença de Buzzer  (1 se tem, 0 caso contrário)
	 * - Capacidade de tocar MP3 (?!?! - Sempre zero!)
	 * - versionRev - 4o elemento do número da Versão
	 * - versionSub - 3o elemento do número da Versão
	 * - versionMin - 2o elemento do número da Versão
	 * - versionMaj - 1o elemento do número da Versão
	 * 
	 * 
	 * 
	 * 
	 * 
	 * ===================== Reset - 0x0C =====================
	 * Direção: Servidor => Rex, TCP
	 * O servidor manda esta mensagem pedindo para o Rex reiniciar.
	 * Esta mensagem não tem campos
	 * 
	 * 
	 * 
	 * 
	 * 
	 * ===================== ID Request - 0x0D =====================
	 * Direção: Servidor => Rex, TCP
	 * O servidor manda esta mensagem pedindo o ID do Rex
	 * O rex deverá responder com um "IdResponse".
	 * Esta mensagem não tem campos
	 * 
	 * 
	 * 
	 * 
	 * 
	 * ===================== ID Response - 0x0E =====================
	 * Direção: Rex => Servidor, TCP
	 * Resposta ao "IdRequest".
	 * A mensagem contém um único campo, de 12 bytes (ASCII), com o ID do Rex.
	 * 
	 * 
	 * 
	 * 
	 * 
	 * ===================== Digital Output - 0x12 =====================
	 * Direção: Servidor => Rex, TCP
	 * Enviado pelo servidor para controlar saidas digitais, buzzer, backlight, leds, etc.
	 * O controle da saída digital permite ligar/desligar de forma temporizada.
	 * Por isso, possui os campos delay_on, delay_off, repeats.
	 * O led/relay/etc ficará ligado por delay_on milisegundos, desligará por delay_off milisegundos, e iniciára um novo ciclo, repeats vezes.
	 * 
	 * A mensagem é composta por vários campos, todos eles inteiros de 4 bytes:
	 * Campos:
	 *   - Tipo de saída: 
	 *     Relé=1, Led=2, backlight=3, buzzer=4
	 *     
	 *   - Número da saída 
	 *     Número do relé / led / backlight / buzzer que será acionado.
	 *     Primeira saída é zero.
	 *     
	 *   - delay_on
	 *     Tempo que a saída ficará ligada a cada repetição. Zero para desligar a saída
	 *      
	 *   - delay_off
	 *     Tempo que a saída ficará desligada a cada repetição.
	 *     
	 *   - repeats
	 *     Número de repetição. 0 para repetir infinitamente.
	 *     Use delay_off=0, repeats=0 para "deixar ligado" 
	 *     
	 *     
	 *     
	 *     
	 *     
	 * ===================== Digital Input Changed - 0x3c =====================
	 * Direção: Rex=>Servidor, TCP
	 * Enviado pelo Rex para notificar uma alteração em uma das entradas digitais.
	 * 
	 * A mensagem é composta por vários campos, todos eles inteiros de 4 bytes:
	 * Campos:
	 *   - Número da entrada
	 *     Primeira entrada tem numero zero. 
	 *     
	 *   - Valor 
	 *     Zero=desligada, Um=ligada.
	 *
	 *      
	 *      
	 *
	 *      
	 * ===================== Key Typed - 0x50 =====================
	 * Direção: Rex=>Servidor, TCP
	 * Enviado pelo Rex para notificar que uma tecla foi digitada.
	 * 
	 * A mensagem é composta por vários campos, todos eles inteiros de 4 bytes:
	 * Campos:
	 *   - Identificador da tecla - inteiros de 4 bytes.
	 *     O código "ASCII" da tecla. Por exemplo, os Digitos ('0'-'9' são representados pelo valores ASCII 48-57, Enter=13, Esc=27)
	 *
	 *     
	 *     
	 *
	 *     
	 * ===================== Image Captured - 0x46 =====================
	 * Direção: Rex=>Servidor, TCP
	 * Enviado pelo Rex para notificar que uma imagem foi capturada nos leitores biométricos.
	 * 
	 * A mensagem é composta por vários campos, todos eles inteiros de 4 bytes:
	 * Campos:
	 *   - Identificador da tecla - inteiros de 4 bytes.
	 *     O código "ASCII" da tecla. Por exemplo, os Digitos ('0'-'9' são representados pelo valores ASCII 48-57, Enter=13, Esc=27)


COMMAND_IO                 = 0x12;
COMMAND_INPUT_CHANGED      = 0x3c;
COMMAND_IMAGE_ACQUIRED     = 0x46;
COMMAND_KEY_TYPED          = 0x50;	
	
COMMAND_RS232_OPEN     = 0x32;
COMMAND_RS232_SET_MODE = 0x33;
COMMAND_RS232_READ     = 0x34;
COMMAND_RS232_WRITE    = 0x35;
COMMAND_RS232_CLOSE    = 0x36;
	
COMMAND_DISPLAY_INITIALIZE       = 0x1e;
COMMAND_DISPLAY_CLEAR            = 0x1f;
COMMAND_DISPLAY_SET_ENTRY_MODE   = 0x21;
COMMAND_DISPLAY_SET_CURSOR       = 0x23;
COMMAND_DISPLAY_MOVE             = 0x24;
COMMAND_DISPLAY_WRITE            = 0x28;
	 */
	public final RexEndpoint rex;
	
	/**Versão do protocolo ("REX0") */
	public static byte[] PACKET_PROTOCOL = Util.StringToBytes("REX0");

	public static final int VERSION_MAJOR = 1;
	public static final int VERSION_MINOR = 1;
	public static final int VERSION_SUB = 0;
	public static final int VERSION_REV = 0;
	
	public abstract static class MessageHandler {
		private final int messageType;
		public MessageHandler(int messageType) {
			this.messageType = messageType;
		}
		public final int getMessageType() {
			return messageType;
		}
		public abstract void handle(DataInputStream in, int inLength, RexProtocol comm) throws IOException;
	}
	
	
	public RexProtocol(RexEndpoint rex) {
		this.rex = rex;
	}
	public abstract void sendCommand(int cmd, byte[] ... data) throws IOException;
	public abstract void handleProtocol() throws IOException; 
	
	public void handleMessage(DataInputStream in, int totalLength) throws IOException {
		byte[] protocol = new byte[4];
		in.readFully(protocol);
		if (!Arrays.equals(protocol, PACKET_PROTOCOL))
			throw new IOException("Invalid protocol version");

		int cmd = Util.readInt(in);
		int cmdLen = Util.readInt(in);
		if (cmdLen != totalLength - 12)
			throw new IOException("Invalid message size");

		byte[] contents = new byte[cmdLen];
		in.readFully(contents);

		
		List<MessageHandler> handlers = rex.getMessageHandlers(cmd); 
		if (handlers.size() == 0) {
			System.err.println(Util.ERR_PREFIX + "Unsupported command: 0x" + Integer.toHexString(cmd));
			//throw new IOException("Unsupported Command: 0x" + Integer.toHexString(cmd) + "\n"); //FIXME atirar erro?
		} else {			
			for (MessageHandler handler : handlers) {
				handler.handle(new DataInputStream(new ByteArrayInputStream(contents)), cmdLen, this);
			}
		}
	}
	
	
	
	

	

	

	

	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	

	
	
	
	
	
	
	
	
	
	
	public static class UDP extends RexProtocol {
		public static final int PORT_DISCOVERY = 2800;
		public static final int PORT_CONNECTION_REQUEST = 1025;
		SocketAddress destinationAddress;
		DatagramSocket datagramSocket;
		
		public UDP(RexEndpoint rex, int port) throws SocketException {
			super(rex);
			datagramSocket = new DatagramSocket(port);
			datagramSocket.setBroadcast(true);
		}
		
		public void setDestinationAddress(InetSocketAddress destinationAddress) {
			this.destinationAddress = destinationAddress;
		}
		
		public synchronized void sendCommand(int cmd, byte[] ... data) throws IOException {
			int len = 0;
			for (byte[] b : data)
				len += b.length;
			ByteArrayOutputStream out = new ByteArrayOutputStream();
			out.write(PACKET_PROTOCOL);
			Util.writeInt(out, cmd);
			Util.writeInt(out, len);
			for (byte[] b : data)
				out.write(b);
			
			datagramSocket.send(new DatagramPacket(out.toByteArray(), out.size(), destinationAddress));
		}
		
		public void handleProtocol() throws IOException {
			byte[] packetBuf = new byte[1024];
			DatagramPacket pack = new DatagramPacket(packetBuf, packetBuf.length);
			while (true) {
				datagramSocket.receive(pack);
				//System.out.println("Got UDP Pack " + Arrays.toString(packetBuf));
				handleMessage(new DataInputStream(new ByteArrayInputStream(pack.getData(), 0, pack.getLength())), pack.getLength());
			}
		}
	}
	
	
	
	
	
	

	public static class TCP extends RexProtocol {
		/** Socket */
		public final Socket socket;
		/** Stream de entrada */
		private final DataInputStream in;
		/** Stream de saida */
		private final OutputStream out;
		
		public TCP(RexEndpoint rex, Socket socket) throws IOException {
			super(rex);
			this.socket = socket;
			socket.setKeepAlive(true);
			//System.out.println("KeepAlive: " + socket.getKeepAlive());
			this.in = new DataInputStream(socket.getInputStream());
			this.out = socket.getOutputStream();
		}
		
		public SocketAddress getRemoteAddress() {
			return socket.getRemoteSocketAddress();
		}
		
		public synchronized void sendCommand(int cmd, byte[] ... data) throws IOException {
			try {
				//System.out.println("Sending Command " + cmd);
				int len = 0;
				for (byte[] b : data)
					len += b.length;
				Util.writeInt(out, len+16);
				out.write(PACKET_PROTOCOL);
				Util.writeInt(out, cmd);
				Util.writeInt(out, len);
				for (byte[] b : data)
					out.write(b);
				out.flush();
				//System.out.println("Command " + cmd + " Sent");
			} catch (IOException e) {
				try {
					socket.close();
				} catch (Exception e2) {}
				throw e;
			}
		}
		
		public void handleProtocol() throws IOException {
			SocketAddress server = socket.getRemoteSocketAddress();
			System.out.println("============CONNECTED TO " + server + "============");
			
			try {
				while (true) {			
					int instrLength = Util.readInt(in) - 4;
					
					byte[] packBuf = new byte[instrLength];
					in.readFully(packBuf);
					DataInputStream cmdIn = new DataInputStream(new ByteArrayInputStream(packBuf));
					handleMessage(cmdIn, instrLength); 
				}
			} catch (Exception e) {
				//e.printStackTrace();
			} finally {
				System.out.println("============DISCONNECTED FROM " + server + "============");
			}
		}
		
		public void close() {
			try {
				socket.close();
			} catch (IOException e) {}
		}
	}
}
