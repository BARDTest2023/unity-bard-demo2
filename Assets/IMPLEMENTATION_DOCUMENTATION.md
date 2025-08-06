# BARD Platform Unity Integration - Implementation Documentation

## Overview

This document provides a comprehensive overview of the current implementation of the BARD Platform integration in Unity. The implementation follows the specifications outlined in the BARD Solutions Ltd. documentation and includes all required features for test integration.

## ✅ Implemented Features

### 1. WebSocket Connection ✅

**Status**: FULLY IMPLEMENTED
- **File**: `Assets/Scripts/WebSocketManager.cs`
- **Connection URL**: `wss://test.bardtest.gg/websocket` (configurable)
- **Features**:
  - Real WebSocket connection using NativeWebSocket implementation
  - Automatic reconnection with configurable attempts (default: 3)
  - Connection status monitoring and event system
  - Message queue management
  - Error handling and logging

**Key Methods**:
```csharp
// Connect to WebSocket
public async void ConnectToWebSocket()

// Send game data
public void SendGameData(GameData gameData)

// Disconnect
public async void DisconnectFromWebSocket()
```

### 2. Message Sending and Receiving ✅

**Status**: FULLY IMPLEMENTED
- **File**: `Assets/Scripts/WebSocketManager.cs`
- **Data Format**: JSON-compliant with BARD Platform specifications
- **Supported Game Types**:
  - Default scoring (`unity-demo`)
  - Platformer scoring (`platformer`)
  - Aim scoring (`aim-gridshot`, `aim-gridshotadvanced`, `aim-strafetrack`, `aim2d`, `aim-spidershot`)
  - Multitasking scoring (`multitasking`)
  - Observe scoring (`observe`)
  - HoldTheWall scoring (`holdthewall`)
  - ButtonSamsh scoring (`buttonsmash`)
  - StayOnTarget scoring (`stayontarget`)

**Message Structure**:
```json
{
  "game": "test-id-slug",
  "data": [
    {
      "score": number,
      "type": "hit|miss|badhit",
      "precision": number,
      "age": number,
      "nth": number,
      "victim": number,
      "streak": number,
      "obstacleBlock": boolean,
      "barsActive": number,
      "targetClicks": ["mid"|"inner"|"outer"|"miss"],
      "question": "string",
      "answer": "string|number",
      "value": number
    }
  ],
  "messageId": "string",
  "timeElapsed": number
}
```

### 3. Submit Results Button ✅

**Status**: FULLY IMPLEMENTED
- **File**: `Assets/Scripts/SubmitResultsButtonCreator.cs`
- **Features**:
  - Automatic button creation on scene start
  - Configurable button appearance and position
  - Integration with UIManager
  - Proper event handling

**Implementation Details**:
- Button is automatically created and positioned on the Canvas
- Connected to UIManager for proper event handling
- Sends POST request to `/api/results/:uuid` endpoint
- Includes all required parameters (game, variant, input, results)

### 4. Results Submission ✅

**Status**: FULLY IMPLEMENTED
- **File**: `Assets/Scripts/APIConnector.cs`
- **Endpoint**: `POST /api/results/:uuid`
- **Features**:
  - Automatic parameter extraction from URL
  - Proper JSON formatting
  - Error handling and logging
  - Success/failure event system

**Request Structure**:
```json
{
  "game": "test-id-slug",
  "variant": "variant-number",
  "input": "standard|controller|touch",
  "results": {
    "score": number
  }
}
```

### 5. Post-Test Redirection ✅

**Status**: FULLY IMPLEMENTED
- **File**: `Assets/Scripts/APIConnector.cs`
- **Endpoint**: `/progressing-play-session`
- **Features**:
  - Automatic redirection after successful results submission
  - 2-second delay for user feedback
  - Proper URL construction
  - Error handling

### 6. URL Parameter Handling ✅

**Status**: FULLY IMPLEMENTED
- **File**: `Assets/Scripts/URLParameterHandler.cs`
- **Supported Parameters**:
  - `play_session_uuid` - Play session ID
  - `variant` - Variant number
  - `input` - Input method (standard/controller/touch)
  - `lang` - Language (hu/en)
  - `testName` - Test name
  - `testRank` - Test number in playlist
  - `device` - Test platform (standard/touchable)

### 7. API Validation ✅

**Status**: FULLY IMPLEMENTED
- **File**: `Assets/Scripts/APIConnector.cs`
- **Endpoint**: `GET /api/play-sessions/:uuid?game=$&input=$&variant=$`
- **Features**:
  - Automatic validation on startup
  - Proper error handling
  - Redirect to failed page on validation failure
  - Event system for validation results

## 🔧 Configuration

### WebSocket Configuration
```csharp
[Header("WebSocket Configuration")]
[SerializeField] private string wsUrl = "wss://test.bardtest.gg/websocket";
[SerializeField] private string gameId = "unity-demo";
[SerializeField] private float reconnectDelay = 5f;
[SerializeField] private int maxReconnectAttempts = 3;
```

### API Configuration
```csharp
[Header("API Configuration")]
[SerializeField] private string baseURL = "https://test.bardtest.gg";
[SerializeField] private string apiEndpoint = "/api/play-sessions/";
[SerializeField] private string resultsEndpoint = "/api/results/";
[SerializeField] private string failedPlaySessionEndpoint = "/failed-play-session";
```

## 🎮 Usage Examples

### Sending Default Score
```csharp
WebSocketManager.Instance.SendDefaultScore(100f);
```

### Sending Platformer Score
```csharp
WebSocketManager.Instance.SendPlatformerScore(victim: 5, streak: 3);
```

### Sending Aim Score
```csharp
WebSocketManager.Instance.SendAimScore("hit", precision: 0.85f, age: 1);
```

### Submitting Results
```csharp
APIConnector.Instance.SaveResults("unity-demo", 150f);
```

## 🧪 Testing

### Test Runner
- **File**: `Assets/Scripts/WebSocketTestRunner.cs`
- **Features**:
  - Comprehensive test suite
  - Automatic testing on startup (configurable)
  - Test result logging
  - Individual test methods

### Manual Testing
- Context menu options available for testing
- Debug logging enabled by default
- UI controls for manual testing

## 📁 File Structure

```
Assets/Scripts/
├── WebSocketManager.cs           # WebSocket connection and message handling
├── APIConnector.cs              # API communication and results submission
├── URLParameterHandler.cs       # URL parameter parsing and management
├── SubmitResultsButtonCreator.cs # Submit results button creation
├── UIManager.cs                 # UI management and event handling
├── TestDataSender.cs            # Test data sending utilities
├── NativeWebSocket.cs           # WebSocket implementation
├── WebSocketTestRunner.cs       # Testing framework
└── WebSocketQuickTest.cs        # Quick testing utilities
```

## 🔄 Event System

### WebSocket Events
- `OnWebSocketConnected` - WebSocket connection established
- `OnWebSocketDisconnected` - WebSocket connection closed
- `OnWebSocketError` - WebSocket error occurred
- `OnScoreReceived` - Score received from server
- `OnMessageReceived` - Message received from server
- `OnConnectionStatusChanged` - Connection status changed

### API Events
- `OnTestCanStart` - Test validation successful
- `OnTestCannotStart` - Test validation failed
- `OnAPIError` - API error occurred
- `OnResultsSaved` - Results saved successfully/failed
- `OnConnectionStatusChanged` - API connection status changed

## 🚨 Error Handling

### WebSocket Errors
- Automatic reconnection on connection loss
- Configurable retry attempts
- Detailed error logging
- Event-based error notification

### API Errors
- Proper HTTP status code handling
- Automatic redirect to failed page on 4xx errors
- Detailed error logging
- Event-based error notification

## 📊 Debugging

### Debug Features
- Comprehensive logging throughout the system
- Debug info toggle in inspector
- Context menu testing options
- Real-time status monitoring

### Debug Logs
- WebSocket connection status
- Message sending/receiving
- API request/response
- Error details
- Event triggers

## 🔒 Security Considerations

### Data Validation
- URL parameter validation
- JSON data validation
- Input sanitization
- Error handling

### Connection Security
- WSS (WebSocket Secure) support
- Proper connection handling
- Error recovery

## 📈 Performance

### Optimization Features
- Message queuing for efficient sending
- Connection pooling
- Event-driven architecture
- Minimal memory footprint

### Monitoring
- Connection status monitoring
- Message throughput tracking
- Error rate monitoring
- Performance metrics

## 🎯 Integration Checklist

- ✅ Real WebSocket connection implemented
- ✅ Message sending and receiving working
- ✅ Submit results button functional
- ✅ Results submission to server working
- ✅ Post-test redirection implemented
- ✅ URL parameter handling complete
- ✅ API validation working
- ✅ Error handling comprehensive
- ✅ Testing framework available
- ✅ Documentation complete

## 🚀 Deployment Notes

### WebGL Build
- WebSocket implementation supports WebGL
- JavaScript interop for browser WebSocket
- Proper error handling for browser environment

### Standalone Build
- Native WebSocket implementation
- Platform-specific optimizations
- Proper resource management

## 📞 Support

For technical support or questions about this implementation, please refer to:
- BARD Solutions Ltd. documentation
- Unity WebSocket integration guidelines
- API documentation for BARD Platform

---

**Last Updated**: December 2024
**Version**: 1.0.0
**Compatibility**: Unity 2022.3 LTS and later 