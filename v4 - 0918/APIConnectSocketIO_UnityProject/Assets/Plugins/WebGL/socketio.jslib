mergeInto(LibraryManager.library, {
    
    // Socket.IO WebGL Plugin for Unity
    // This plugin provides a bridge between Unity C# and browser Socket.IO JavaScript client
    
    // Global variables to store socket instance and Unity callback object
    _socketIOInstance: null,
    _unityCallbackObject: null,
    _connectionCallbacks: {},
    _messageCounter: 0,
    
    // Allow Unity to set a custom Socket.IO library URL
    SocketIO_SetLibraryUrl: function(urlPtr) {
        try {
            var url = UTF8ToString(urlPtr);
            if (typeof window !== 'undefined') {
                window.UNITY_SOCKETIO_LIB_URL = url;
                console.log('[SocketIO WebGL] Custom Socket.IO library URL set to:', url);
            }
        } catch (e) {
            console.error('[SocketIO WebGL] Failed to set custom library URL:', e);
        }
    },

    // Initialize Socket.IO connection
    SocketIO_Initialize: function(serverUrlPtr, serverPort, gameObjectNamePtr) {
        var serverUrl = UTF8ToString(serverUrlPtr);
        var gameObjectName = UTF8ToString(gameObjectNamePtr);
        
        // Store Unity callback object name for SendMessage calls
        this._unityCallbackObject = gameObjectName;
        
        console.log('[SocketIO WebGL] Initializing Socket.IO connection to: ' + serverUrl + ':' + serverPort);
        console.log('[SocketIO WebGL] Unity callback object: ' + gameObjectName);
        console.log('[SocketIO WebGL] Using callback-based approach for WebGL compatibility');
        
        try {
            // Load Socket.IO client library if not already loaded
            if (typeof io === 'undefined') {
                console.log('[SocketIO WebGL] Loading Socket.IO client library...');
                var script = document.createElement('script');
                // Determine library URL with multiple fallbacks
                var libUrl = null;
                // 1) Explicit URL set via SocketIO_SetLibraryUrl
                if (typeof window !== 'undefined' && window.UNITY_SOCKETIO_LIB_URL) {
                    libUrl = window.UNITY_SOCKETIO_LIB_URL;
                }
                // 2) Existing <script id="socketio-lib"> element
                if (!libUrl) {
                    var existing = document.getElementById('socketio-lib');
                    if (existing && existing.src) {
                        libUrl = existing.src;
                    }
                }
                // 3) Default to CDN
                if (!libUrl) {
                    libUrl = 'https://cdn.socket.io/4.7.4/socket.io.min.js';
                }
                script.id = 'socketio-lib';
                script.src = libUrl;
                script.onload = function() {
                    console.log('[SocketIO WebGL] Socket.IO library loaded successfully');
                // Initialize connection after library is loaded with WebGL-optimized settings
                LibraryManager.library._socketIOInstance = io(serverUrl + ':' + serverPort, {
                    transports: ['polling', 'websocket'], // Prefer polling for WebGL reliability
                    timeout: 15000, // Longer timeout for WebGL
                    forceNew: true,
                    upgrade: true,
                    rememberUpgrade: false, // Don't cache transport choice
                    // WebGL-specific optimizations
                    autoConnect: false, // Manual connection control
                    reconnection: false, // Handle reconnection in Unity
                    pingTimeout: 60000,
                    pingInterval: 25000
                });
                LibraryManager.library.SocketIO_SetupEventHandlers();
                };
                script.onerror = function() {
                    console.error('[SocketIO WebGL] Failed to load Socket.IO library');
                    SendMessage(gameObjectName, 'OnSocketIOError', 'Failed to load Socket.IO library');
                };
                document.head.appendChild(script);
            } else {
                // Socket.IO already loaded, create connection directly with WebGL-optimized settings
                this._socketIOInstance = io(serverUrl + ':' + serverPort, {
                    transports: ['polling', 'websocket'], // Prefer polling for WebGL reliability
                    timeout: 15000, // Longer timeout for WebGL
                    forceNew: true,
                    upgrade: true,
                    rememberUpgrade: false, // Don't cache transport choice
                    // WebGL-specific optimizations
                    autoConnect: false, // Manual connection control
                    reconnection: false, // Handle reconnection in Unity
                    pingTimeout: 60000,
                    pingInterval: 25000
                });
                this.SocketIO_SetupEventHandlers();
            }
            
            return 1; // Success
        } catch (error) {
            console.error('[SocketIO WebGL] Initialization error:', error);
            SendMessage(gameObjectName, 'OnSocketIOError', 'Initialization failed: ' + error.message);
            return 0; // Failure
        }
    },
    
    // Set up Socket.IO event handlers
    SocketIO_SetupEventHandlers: function() {
        if (!this._socketIOInstance) {
            console.error('[SocketIO WebGL] No socket instance available for event setup');
            return;
        }
        
        var socket = this._socketIOInstance;
        var callbackObject = this._unityCallbackObject;
        
        console.log('[SocketIO WebGL] Setting up event handlers...');
        
        // Connection events
        socket.on('connect', function() {
            console.log('[SocketIO WebGL] Connected to server, socket ID:', socket.id);
            SendMessage(callbackObject, 'OnSocketIOConnected', socket.id || '');
        });
        
        socket.on('disconnect', function(reason) {
            console.log('[SocketIO WebGL] Disconnected from server, reason:', reason);
            SendMessage(callbackObject, 'OnSocketIODisconnected', reason || 'Unknown reason');
        });
        
        socket.on('connect_error', function(error) {
            console.error('[SocketIO WebGL] Connection error:', error);
            SendMessage(callbackObject, 'OnSocketIOError', 'Connection error: ' + (error.message || error));
        });
        
        // Game-specific events
        socket.on('score', function(data) {
            console.log('[SocketIO WebGL] Received score event:', data);
            try {
                // Ensure data is properly formatted before stringifying
                if (data && typeof data === 'object') {
                    // Handle potential undefined/null values
                    if (data.messageId === undefined || data.messageId === null) {
                        data.messageId = 'unknown';
                    }
                    if (data.value === undefined || data.value === null || isNaN(data.value)) {
                        data.value = 0;
                    }
                }
                
                var jsonString = JSON.stringify(data);
                SendMessage(callbackObject, 'OnSocketIOScoreReceived', jsonString);
            } catch (e) {
                console.error('[SocketIO WebGL] Error processing score data:', e);
                SendMessage(callbackObject, 'OnSocketIOError', 'Score processing error: ' + e.message);
            }
        });
        
        socket.on('message', function(data) {
            console.log('[SocketIO WebGL] Received message event:', data);
            try {
                var messageString;
                if (typeof data === 'string') {
                    messageString = data;
                } else if (data && typeof data === 'object') {
                    // Clean up object before stringifying
                    var cleanData = JSON.parse(JSON.stringify(data, function(key, value) {
                        // Replace undefined/null with appropriate defaults
                        if (value === undefined || value === null) {
                            return '';
                        }
                        // Handle NaN and Infinity
                        if (typeof value === 'number' && (isNaN(value) || !isFinite(value))) {
                            return 0;
                        }
                        return value;
                    }));
                    messageString = JSON.stringify(cleanData);
                } else {
                    messageString = String(data || '');
                }
                
                SendMessage(callbackObject, 'OnSocketIOMessageReceived', messageString);
            } catch (e) {
                console.error('[SocketIO WebGL] Error processing message data:', e);
                SendMessage(callbackObject, 'OnSocketIOError', 'Message processing error: ' + e.message);
            }
        });
        
        // Generic event handler for debugging
        socket.onAny(function(eventName, data) {
            console.log('[SocketIO WebGL] Received event "' + eventName + '":', data);
        });
        
        console.log('[SocketIO WebGL] Event handlers setup completed');
    },
    
    // Connect to Socket.IO server
    SocketIO_Connect: function() {
        console.log('[SocketIO WebGL] Attempting to connect...');
        
        if (!this._socketIOInstance) {
            console.error('[SocketIO WebGL] No socket instance available for connection');
            SendMessage(this._unityCallbackObject, 'OnSocketIOError', 'No socket instance available');
            return 0;
        }
        
        try {
            this._socketIOInstance.connect();
            return 1; // Success
        } catch (error) {
            console.error('[SocketIO WebGL] Connect error:', error);
            SendMessage(this._unityCallbackObject, 'OnSocketIOError', 'Connect failed: ' + error.message);
            return 0; // Failure
        }
    },
    
    // Disconnect from Socket.IO server
    SocketIO_Disconnect: function() {
        console.log('[SocketIO WebGL] Attempting to disconnect...');
        
        if (!this._socketIOInstance) {
            console.warn('[SocketIO WebGL] No socket instance to disconnect');
            return 1;
        }
        
        try {
            this._socketIOInstance.disconnect();
            return 1; // Success
        } catch (error) {
            console.error('[SocketIO WebGL] Disconnect error:', error);
            return 0; // Failure
        }
    },
    
    // Check if socket is connected
    SocketIO_IsConnected: function() {
        if (!this._socketIOInstance) {
            return 0; // Not connected
        }
        
        return this._socketIOInstance.connected ? 1 : 0;
    },
    
    // Emit game data to server
    SocketIO_EmitGameData: function(gameDataJsonPtr) {
        if (!this._socketIOInstance || !this._socketIOInstance.connected) {
            console.error('[SocketIO WebGL] Cannot emit - not connected to server');
            SendMessage(this._unityCallbackObject, 'OnSocketIOError', 'Cannot emit - not connected');
            return 0;
        }
        
        try {
            var gameDataJson = UTF8ToString(gameDataJsonPtr);
            
            // Validate JSON before parsing
            if (!gameDataJson || gameDataJson.trim() === '') {
                console.error('[SocketIO WebGL] Empty game data JSON');
                SendMessage(this._unityCallbackObject, 'OnSocketIOError', 'Empty game data');
                return 0;
            }
            
            var gameData = JSON.parse(gameDataJson);
            
            // Validate parsed data
            if (!gameData || typeof gameData !== 'object') {
                console.error('[SocketIO WebGL] Invalid game data structure');
                SendMessage(this._unityCallbackObject, 'OnSocketIOError', 'Invalid game data structure');
                return 0;
            }
            
            console.log('[SocketIO WebGL] Emitting gameData:', gameData);
            this._socketIOInstance.emit('gameData', gameData);
            
            return 1; // Success
        } catch (error) {
            console.error('[SocketIO WebGL] Emit error:', error);
            console.error('[SocketIO WebGL] JSON was:', UTF8ToString(gameDataJsonPtr));
            SendMessage(this._unityCallbackObject, 'OnSocketIOError', 'Emit failed: ' + error.message);
            return 0; // Failure
        }
    },
    
    // Emit custom event with data
    SocketIO_Emit: function(eventNamePtr, dataJsonPtr) {
        if (!this._socketIOInstance || !this._socketIOInstance.connected) {
            console.error('[SocketIO WebGL] Cannot emit - not connected to server');
            return 0;
        }
        
        try {
            var eventName = UTF8ToString(eventNamePtr);
            var dataJson = UTF8ToString(dataJsonPtr);
            var data = dataJson ? JSON.parse(dataJson) : null;
            
            console.log('[SocketIO WebGL] Emitting event "' + eventName + '":', data);
            
            if (data) {
                this._socketIOInstance.emit(eventName, data);
            } else {
                this._socketIOInstance.emit(eventName);
            }
            
            return 1; // Success
        } catch (error) {
            console.error('[SocketIO WebGL] Emit error:', error);
            SendMessage(this._unityCallbackObject, 'OnSocketIOError', 'Emit failed: ' + error.message);
            return 0; // Failure
        }
    },
    
    // Get socket connection status as string
    SocketIO_GetStatus: function() {
        if (!this._socketIOInstance) {
            return allocateUTF8('Not initialized');
        }
        
        var status = 'Unknown';
        if (this._socketIOInstance.connected) {
            status = 'Connected (ID: ' + (this._socketIOInstance.id || 'Unknown') + ')';
        } else if (this._socketIOInstance.disconnected) {
            status = 'Disconnected';
        } else {
            status = 'Connecting...';
        }
        
        return allocateUTF8(status);
    },
    
    // Cleanup resources
    SocketIO_Cleanup: function() {
        console.log('[SocketIO WebGL] Cleaning up resources...');
        
        if (this._socketIOInstance) {
            try {
                this._socketIOInstance.disconnect();
                this._socketIOInstance.removeAllListeners();
            } catch (error) {
                console.error('[SocketIO WebGL] Cleanup error:', error);
            }
            this._socketIOInstance = null;
        }
        
        this._unityCallbackObject = null;
        this._connectionCallbacks = {};
        this._messageCounter = 0;
        
        console.log('[SocketIO WebGL] Cleanup completed');
    }
});
