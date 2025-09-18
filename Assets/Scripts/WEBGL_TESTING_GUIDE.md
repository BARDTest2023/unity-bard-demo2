# WebGL Socket.IO Testing Guide

This guide explains how to test the WebGL Socket.IO implementation with the BARD platform.

## Quick Start Testing

### 1. In Unity Editor

1. **Open the APITest scene**
2. **Right-click on SocketIOManager in hierarchy**
3. **Select "Quick Connection Check"** from context menu
4. **Check Console** for connection status

### 2. Comprehensive Testing

1. **Right-click on SocketIOManager in hierarchy**
2. **Select "Run WebGL Socket.IO Tests"** from context menu
3. **Watch Console** for detailed test results
4. **Tests include:**
   - Platform Detection
   - JSON Serialization
   - Connection Test
   - Message Sending
   - Game Data Sending
   - Score Receiving
   - Reconnection Test

### 3. Server Validation

1. **Add ServerValidator component** to a GameObject
2. **Right-click on ServerValidator**
3. **Select "Validate All Servers"** from context menu
4. **Check which servers are available**

## Testing with Local Server

### 1. Start Local Test Server

```bash
cd /path/to/project
node test-socketio-server.js
```

The server will run on `http://localhost:3333`

### 2. Configure Unity for Local Testing

1. **In SocketIOManager inspector:**
   - Set Server URL: `http://localhost`
   - Set Server Port: `3333`
   - Enable "Bypass API Validation"
   - Enable "Auto Connect"

### 3. Test Connection

1. **Play the scene**
2. **Click "Connect" button** in UI
3. **Check Console** for connection messages
4. **Test various game data** using UI buttons

## Testing with BARD Platform

### 1. Configure for BARD

1. **In SocketIOManager inspector:**
   - Set Server URL: `http://test.bardtest.gg`
   - Set Server Port: `80`
   - Disable "Bypass API Validation"
   - Configure URL parameters as needed

### 2. Test with Real BARD Server

1. **Ensure valid play session UUID**
2. **Run API validation first**
3. **Test Socket.IO connection**
4. **Send various game data types**

## WebGL Build Testing

### 1. Build for WebGL

1. **File > Build Settings**
2. **Select WebGL platform**
3. **Configure build settings:**
   - Compression Format: Disabled (for testing)
   - Exception Support: Explicitly Thrown Exceptions Only
   - Enable "Development Build" for debugging

### 2. Test in Browser

1. **Serve the build** using a local web server
2. **Open browser Developer Tools** (F12)
3. **Check Console** for WebGL-specific logs
4. **Look for Socket.IO connection messages**

### 3. Common WebGL Issues

- **CORS Issues**: Ensure server allows cross-origin requests
- **Mixed Content**: Use HTTPS if serving over HTTPS
- **Browser Compatibility**: Test in Chrome, Firefox, Safari
- **Network Restrictions**: Check firewall/proxy settings

## Test Results Interpretation

### ✅ Success Indicators

```
[SocketIO WebGL] Connected to server, socket ID: abc123
[SocketIOManager] WebGL Socket.IO connected
[Test] Connection: PASSED
[Test] Game Data Sending: PASSED
```

### ❌ Failure Indicators

```
[SocketIO WebGL] Connection error: timeout
[Test] Connection: FAILED - Timeout
[ServerValidator] ❌ Server: Not available
```

### 🔧 Debugging Steps

1. **Check Server Availability:**
   ```
   Right-click ServerValidator > Validate All Servers
   ```

2. **Test JSON Serialization:**
   ```
   Right-click SocketIOManager > Test WebGL JSON Serialization
   ```

3. **Check Platform Detection:**
   ```
   Console should show: Platform: WebGL Build
   ```

4. **Verify Socket.IO Library Loading:**
   ```
   Browser console should show: Socket.IO library loaded successfully
   ```

## Manual Testing Checklist

### Connection Testing
- [ ] Server availability check passes
- [ ] Socket.IO connection establishes
- [ ] Connection status updates correctly
- [ ] Error handling works properly

### Data Transmission
- [ ] Default score sending works
- [ ] Platformer score sending works
- [ ] Aim score sending works
- [ ] All game types send correctly
- [ ] JSON serialization is valid

### Response Handling
- [ ] Score responses are received
- [ ] Messages are received correctly
- [ ] JSON deserialization works
- [ ] Event callbacks trigger properly

### WebGL Specific
- [ ] JavaScript interop functions correctly
- [ ] SendMessage callbacks work
- [ ] No async/await errors in browser
- [ ] Memory usage is reasonable

### Error Recovery
- [ ] Connection failures are handled
- [ ] Reconnection attempts work
- [ ] Fallback servers are tried
- [ ] User feedback is provided

## Troubleshooting

### "Socket.IO library failed to load"
- Check internet connection
- Verify CDN accessibility
- Consider local Socket.IO library

### "Connection timeout"
- Verify server is running
- Check network connectivity
- Increase timeout values
- Try fallback servers

### "JSON parsing errors"
- Check WebGLJsonHelper tests
- Verify data structure
- Look for null/undefined values
- Check numeric ranges (NaN, Infinity)

### "SendMessage not working"
- Verify GameObject name is correct
- Check callback method signatures
- Ensure Unity is not paused
- Verify WebGL build settings

## Advanced Testing

### Performance Testing
```csharp
// Test rapid message sending
for (int i = 0; i < 100; i++)
{
    SocketIOManager.Instance.SendDefaultScore(i);
    yield return new WaitForSeconds(0.1f);
}
```

### Stress Testing
```csharp
// Test connection stability
for (int i = 0; i < 10; i++)
{
    SocketIOManager.Instance.DisconnectFromSocketIO();
    yield return new WaitForSeconds(2f);
    SocketIOManager.Instance.ConnectToSocketIO();
    yield return new WaitForSeconds(5f);
}
```

### Custom Game Data Testing
```csharp
// Test custom game data structures
GameData customData = new GameData
{
    game = "custom-test",
    data = new List<GameMetric> { /* custom metrics */ }
};
SocketIOManager.Instance.SendGameData(customData);
```

## Support

For issues with WebGL Socket.IO implementation:

1. **Check Console Logs** first (both Unity and Browser)
2. **Run Comprehensive Tests** to isolate the issue
3. **Validate Server Availability** before reporting connection issues
4. **Test with Local Server** to rule out network issues
5. **Compare Standalone vs WebGL** behavior

Remember: WebGL has different networking constraints than standalone builds, so some behaviors may differ between platforms.
