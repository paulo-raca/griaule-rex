package veridis.embedded.rex;

import java.util.ArrayList;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import veridis.embedded.rex.RexProtocol.MessageHandler;

public abstract class RexEndpoint {
	
	protected Map<Integer, List<MessageHandler>> handlers = new HashMap<Integer, List<MessageHandler>>();
	
	public void addMessageHandler(MessageHandler handler) {
		synchronized (this.handlers) {
			int type = handler.getMessageType();
			
			synchronized (handlers) {
				List<MessageHandler> handlers = new ArrayList<MessageHandler>();			
				if (this.handlers.containsKey(type))
					handlers.addAll(this.handlers.get(type));
				handlers.add(handler);
				this.handlers.put(type, handlers);
			}
		}
	}
	public void removeMessageHandler(MessageHandler handler) {
		int type = handler.getMessageType();
		
		synchronized (handlers) {
			List<MessageHandler> handlers = new ArrayList<MessageHandler>();			
			if (this.handlers.containsKey(type))
				handlers.addAll(this.handlers.get(type));
			handlers.remove(handler);
			this.handlers.put(type, handlers);
		}
	}
	
	
	@SuppressWarnings("unchecked")
	public List<MessageHandler> getMessageHandlers(int code) {
		synchronized (handlers) {
			List<MessageHandler> list = handlers.get(code);
 			if (list==null) { 
				return Collections.EMPTY_LIST;
 			} else {
 				return list;
 			}
		}
	}
}