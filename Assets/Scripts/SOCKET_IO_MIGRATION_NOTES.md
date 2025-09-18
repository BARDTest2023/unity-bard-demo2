# Socket.IO Migration Notes

## Overview

The Unity project has been successfully migrated from plain WebSocket communication to Socket.IO to ensure compatibility with the AdonisJS v5 platform.

## Changes Made

### 1. Library Integration
- **Added**: SocketIOUnity library (v1.1.5) in `Assets/Scripts/SocketIOUnity/`
- **Added**: Newtonsoft.Json dependency in `Packages/manifest.json`
- **Library Source**: https://github.com/itisnajim/SocketIOUnity

### 2. New Socket.IO Manager
- **Created**: `Assets/Scripts/SocketIOManager.cs` - replaces WebSocketManager
- **Features**:
  - Socket.IO v4 compatibility (configurable)
  - Event-based communication (`gameData`, `score`, `message`)
  - Automatic reconnection
  - Unity main thread event handling
  - Backward compatibility methods

### 3. Updated Components
- **UIManager.cs**: Updated to use SocketIOManager instead of WebSocketManager
- **TestDataSender.cs**: Updated to use SocketIOManager for sending test data
- **WebSocketTestRunner.cs**: Updated to test Socket.IO connection
- **WebSocketManager.cs**: Deprecated (renamed to WebSocketManager_DEPRECATED)

### 4. Configuration Changes
- **Server URL**: Changed from `wss://test.bardtest.gg/websocket` to `http://test.bardtest.gg:80`
- **Protocol**: Changed from WebSocket to Socket.IO
- **Transport**: Uses WebSocket transport within Socket.IO framework

## Socket.IO Events

### Outgoing Events (Unity → Server)
- `gameData`: Sends game scoring data to the server

### Incoming Events (Server → Unity)
- `score`: Receives score responses from server
- `message`: Receives general messages from server

### Connection Events
- `connect`: Socket.IO connection established
- `disconnect`: Socket.IO connection closed
- `error`: Connection or communication errors

## Testing Instructions

### 1. Unity Editor Testing
1. Open the Unity project
2. Load the `APITest` scene
3. Click the "Connect" button in the UI
4. Verify Socket.IO connection in the console logs
5. Test sending various game data types using the UI buttons

### 2. Server Requirements
The AdonisJS v5 server must be configured to handle Socket.IO connections and the following events:

```javascript
// Server-side event handling example
io.on('connection', (socket) => {
  console.log('Unity client connected');
  
  socket.on('gameData', (data) => {
    console.log('Received game data:', data);
    
    // Process game data and send response
    socket.emit('score', {
      messageId: data.messageId || 'response_' + Date.now(),
      value: calculateScore(data)
    });
  });
  
  socket.on('disconnect', () => {
    console.log('Unity client disconnected');
  });
});
```

### 3. Connection URL Format
- **Development**: `http://localhost:3333` (AdonisJS default)
- **Production**: `http://test.bardtest.gg:80` (configured in SocketIOManager)

## Troubleshooting

### Common Issues

1. **Connection Refused**
   - Ensure AdonisJS server is running with Socket.IO enabled
   - Check firewall settings for the specified port
   - Verify the server URL and port in SocketIOManager

2. **Events Not Received**
   - Confirm server is listening for the correct event names
   - Check JSON serialization compatibility
   - Verify Unity main thread event handling

3. **Reconnection Issues**
   - Check network connectivity
   - Verify reconnection settings in SocketIOManager
   - Monitor server-side connection handling

### Debug Logging
Enable debug logging in SocketIOManager by setting `showDebugInfo = true` in the inspector to see detailed connection and message information.

## Migration Benefits

1. **AdonisJS v5 Compatibility**: Full compatibility with Socket.IO framework used by AdonisJS
2. **Improved Reliability**: Better connection management and automatic reconnection
3. **Event-Based Architecture**: More structured communication with named events
4. **Enhanced Error Handling**: Better error reporting and recovery mechanisms
5. **Cross-Platform Support**: Works on all Unity-supported platforms including WebGL

## Files Modified

- `Assets/Scripts/SocketIOManager.cs` (NEW)
- `Assets/Scripts/UIManager.cs` (UPDATED)
- `Assets/Scripts/TestDataSender.cs` (UPDATED)
- `Assets/Scripts/WebSocketTestRunner.cs` (UPDATED)
- `Assets/Scripts/WebSocketManager.cs` (DEPRECATED)
- `Assets/IMPLEMENTATION_DOCUMENTATION.md` (UPDATED)
- `Packages/manifest.json` (UPDATED)

## Next Steps

1. Test the Socket.IO connection with the actual AdonisJS v5 server
2. Verify all game data types are properly handled by the server
3. Test reconnection behavior under various network conditions
4. Update any additional components that may reference the old WebSocketManager
