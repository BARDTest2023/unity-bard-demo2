using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using SocketIOClient;
using System.Linq;

// WebGL platform detection
#if UNITY_WEBGL && !UNITY_EDITOR
#define WEBGL_BUILD
#endif

[System.Serializable]
public class GameData
{
    public string game;
    public List<GameMetric> data;
    public string messageId;
    public float timeElapsed;
}

[System.Serializable]
public class GameMetric
{
    public float score;
    public string type;
    public float precision;
    public int age;
    public int nth;
    public int victim;
    public int streak;
    public bool obstacleBlock;
    public int barsActive;
    public List<string> targetClicks;
    public string question;
    public string answer;
    public float value;
    public float timeElapsed; // Added for consistency with WebGLJsonHelper
}

[System.Serializable]
public class SocketIOResponse
{
    public string messageId;
    public float value;
}

public class SocketIOManager : MonoBehaviour
{
    [Header("Socket.IO Configuration")]
    [SerializeField] private string serverUrl = "http://test.bardtest.gg";
    [SerializeField] private int serverPort = 80;
    [SerializeField] private string gameId = "unity-demo";
    [SerializeField] private float reconnectDelay = 5f;
    [SerializeField] private int maxReconnectAttempts = 3;
    [SerializeField] private float connectionTimeout = 10f;
    [SerializeField] private bool useFallbackServers = true;
    
    [Header("Fallback Servers")]
    [SerializeField] private List<string> fallbackUrls = new List<string>
    {
        "http://localhost:3333",
        "https://socket-io-chat.now.sh",
        "wss://ws.postman-echo.com"
    };

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool autoConnect = true;
    [SerializeField] private bool bypassAPIValidation = false;

    public static SocketIOManager Instance { get; private set; }

    // Socket.IO instance - platform specific
#if WEBGL_BUILD
    private SocketIOWebGL webglSocket;
#else
    private SocketIOUnity socket;
#endif
    private Coroutine reconnectCoroutine;
    private Coroutine connectionTimeoutCoroutine;
    private int reconnectAttempts = 0;
    private int currentServerIndex = 0;
    private bool isConnecting = false;
    private bool connectionTimedOut = false;

    // Message tracking
    private int messageCounter = 0;

    // Events
    public event Action OnSocketConnected;
    public event Action OnSocketDisconnected;
    public event Action<string> OnSocketError;
    public event Action<SocketIOResponse> OnScoreReceived;
    public event Action<string> OnMessageReceived;
    public event Action<bool> OnConnectionStatusChanged;

    // Connection status - platform specific
    public bool IsConnected 
    {
        get
        {
#if WEBGL_BUILD
            return webglSocket != null && webglSocket.IsConnected;
#else
            return socket != null && socket.Connected;
#endif
        }
    }
    public bool IsConnecting => isConnecting;

    private void Awake()
    {
        Debug.Log("SocketIOManager Awake");
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize platform-specific socket implementation
            InitializePlatformSocket();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    /// <summary>
    /// Initialize the appropriate Socket.IO implementation based on platform
    /// </summary>
    private void InitializePlatformSocket()
    {
#if WEBGL_BUILD
        Debug.Log("[SocketIOManager] Initializing WebGL Socket.IO implementation");
        
        // Add SocketIOWebGL component if not present
        webglSocket = GetComponent<SocketIOWebGL>();
        if (webglSocket == null)
        {
            webglSocket = gameObject.AddComponent<SocketIOWebGL>();
        }
        
        // Subscribe to WebGL socket events
        webglSocket.OnSocketConnected += OnWebGLSocketConnected;
        webglSocket.OnSocketDisconnected += OnWebGLSocketDisconnected;
        webglSocket.OnSocketError += OnWebGLSocketError;
        webglSocket.OnScoreReceived += OnWebGLScoreReceived;
        webglSocket.OnMessageReceived += OnWebGLMessageReceived;
        
        if (showDebugInfo)
            Debug.Log("[SocketIOManager] WebGL Socket.IO implementation initialized");
#else
        Debug.Log("[SocketIOManager] Using standard SocketIOUnity implementation");
        // Standard implementation - no additional setup needed here
#endif
    }

    private void Start()
    {
        Debug.Log("SocketIOManager Start");
        if (autoConnect)
        {
            if (bypassAPIValidation)
            {
                Debug.Log("SocketIOManager: Bypassing API validation for testing");
                ConnectToSocketIO();
            }
            else
            {
                // Wait for API validation before connecting
                if (APIConnector.Instance != null)
                {
                    APIConnector.Instance.OnTestCanStart += OnTestCanStart;
                }
            }
        }
    }

    private void OnTestCanStart(PlaySessionData data)
    {
        Debug.Log("SocketIOManager OnTestCanStart");
        if (showDebugInfo)
            Debug.Log("Test can start - connecting to Socket.IO server...");
        
        ConnectToSocketIO();
    }

    /// <summary>
    /// Connects to the Socket.IO server with enhanced error handling and fallbacks
    /// </summary>
    public void ConnectToSocketIO()
    {
        Debug.Log("SocketIOManager ConnectToSocketIO");
        if (IsConnected || IsConnecting)
        {
            if (showDebugInfo)
                Debug.Log("Socket.IO already connected or connecting");
            return;
        }

        // Reset connection state
        isConnecting = true;
        connectionTimedOut = false;
        
        // Try primary server first
        currentServerIndex = 0;
        
#if WEBGL_BUILD
        AttemptWebGLConnection();
#else
        AttemptConnection();
#endif
    }

    /// <summary>
    /// Attempts connection to current server with timeout and fallback logic
    /// </summary>
    private void AttemptConnection()
    {
        string currentUrl = GetCurrentServerUrl();
        
        if (showDebugInfo)
            Debug.Log($"Attempting connection to: {currentUrl}");

        OnConnectionStatusChanged?.Invoke(true);

        try
        {
            // Clean up previous socket if exists
            CleanupSocket();

            // Create Socket.IO URI
            var uri = new Uri(currentUrl);
            
            // Create new Socket.IO instance with enhanced options
            socket = new SocketIOUnity(uri, new SocketIOOptions
            {
                EIO = EngineIO.V4,
                Transport = SocketIOClient.Transport.TransportProtocol.Polling, // Start with polling for better compatibility
                ConnectionTimeout = TimeSpan.FromSeconds(connectionTimeout),
                Reconnection = false, // Handle reconnection manually
                Query = new Dictionary<string, string>
                {
                    {"gameId", gameId},
                    {"unity", "true"},
                    {"version", Application.version}
                }
            });

            // Subscribe to Socket.IO events
            socket.OnConnected += OnSocketIOConnected;
            socket.OnDisconnected += OnSocketIODisconnected;
            socket.OnError += OnSocketIOError;

            // Listen for game-specific events
            socket.OnUnityThread("score", OnScoreEventReceived);
            socket.OnUnityThread("message", OnMessageEventReceived);

            // Start connection timeout
            if (connectionTimeoutCoroutine != null)
                StopCoroutine(connectionTimeoutCoroutine);
            connectionTimeoutCoroutine = StartCoroutine(ConnectionTimeoutCoroutine());

            // Connect to the server
            socket.Connect();
        }
        catch (Exception e)
        {
            string errorMessage = $"Failed to create Socket.IO connection to {currentUrl}: {e.Message}";
            Debug.LogError(errorMessage);
            HandleConnectionFailure(errorMessage);
        }
    }

#if WEBGL_BUILD
    /// <summary>
    /// Attempts WebGL Socket.IO connection using JavaScript bridge
    /// </summary>
    private void AttemptWebGLConnection()
    {
        string currentUrl = GetCurrentServerUrl();
        
        if (showDebugInfo)
            Debug.Log($"[SocketIOManager] Attempting WebGL connection to: {currentUrl}");

        OnConnectionStatusChanged?.Invoke(true);

        try
        {
            if (webglSocket == null)
            {
                HandleConnectionFailure("WebGL socket not initialized");
                return;
            }
            
            // Extract URL and port for WebGL initialization
            string baseUrl = serverUrl?.Trim() ?? "http://test.bardtest.gg";
            if (!baseUrl.StartsWith("http://") && !baseUrl.StartsWith("https://"))
            {
                baseUrl = "http://" + baseUrl;
            }
            
            // Initialize and connect WebGL socket
            bool initialized = webglSocket.Initialize(baseUrl, serverPort);
            if (initialized)
            {
                webglSocket.Connect();
                
                // Start connection timeout for WebGL
                if (connectionTimeoutCoroutine != null)
                    StopCoroutine(connectionTimeoutCoroutine);
                connectionTimeoutCoroutine = StartCoroutine(ConnectionTimeoutCoroutine());
            }
            else
            {
                HandleConnectionFailure("Failed to initialize WebGL Socket.IO");
            }
        }
        catch (Exception e)
        {
            string errorMessage = $"Failed to create WebGL Socket.IO connection to {currentUrl}: {e.Message}";
            Debug.LogError(errorMessage);
            HandleConnectionFailure(errorMessage);
        }
    }
#endif

    /// <summary>
    /// Gets the current server URL to attempt connection
    /// </summary>
    private string GetCurrentServerUrl()
    {
        if (currentServerIndex == 0)
        {
            // Primary server - ensure no extra spaces and proper URL format
            string cleanUrl = serverUrl?.Trim() ?? "http://localhost";
            
            // Ensure URL has protocol
            if (!cleanUrl.StartsWith("http://") && !cleanUrl.StartsWith("https://") && !cleanUrl.StartsWith("ws://") && !cleanUrl.StartsWith("wss://"))
            {
                cleanUrl = "http://" + cleanUrl;
            }
            
            return $"{cleanUrl}:{serverPort}";
        }
        else if (useFallbackServers && fallbackUrls != null && currentServerIndex <= fallbackUrls.Count)
        {
            // Fallback servers - ensure no extra spaces
            string fallbackUrl = fallbackUrls[currentServerIndex - 1]?.Trim() ?? "http://localhost:3333";
            
            // Ensure URL has protocol
            if (!fallbackUrl.StartsWith("http://") && !fallbackUrl.StartsWith("https://") && !fallbackUrl.StartsWith("ws://") && !fallbackUrl.StartsWith("wss://"))
            {
                fallbackUrl = "http://" + fallbackUrl;
            }
            
            return fallbackUrl;
        }
        else
        {
            // Reset to primary
            currentServerIndex = 0;
            string cleanUrl = serverUrl?.Trim() ?? "http://localhost";
            
            if (!cleanUrl.StartsWith("http://") && !cleanUrl.StartsWith("https://") && !cleanUrl.StartsWith("ws://") && !cleanUrl.StartsWith("wss://"))
            {
                cleanUrl = "http://" + cleanUrl;
            }
            
            return $"{cleanUrl}:{serverPort}";
        }
    }

    /// <summary>
    /// Handles connection timeout
    /// </summary>
    private IEnumerator ConnectionTimeoutCoroutine()
    {
        yield return new WaitForSeconds(connectionTimeout);
        
        if (isConnecting && !IsConnected)
        {
            connectionTimedOut = true;
            string timeoutMessage = $"Connection timeout after {connectionTimeout} seconds";
            Debug.LogWarning(timeoutMessage);
            
            // Try next server or fail
            TryNextServerOrFail(timeoutMessage);
        }
    }

    /// <summary>
    /// Tries next server in fallback list or fails completely
    /// </summary>
    private void TryNextServerOrFail(string lastError)
    {
        currentServerIndex++;
        
        if (useFallbackServers && currentServerIndex <= fallbackUrls.Count)
        {
            if (showDebugInfo)
                Debug.Log($"Trying fallback server {currentServerIndex}...");
            
            // Clean up current connection attempt
            CleanupSocket();
            
            // Try next server
#if WEBGL_BUILD
            AttemptWebGLConnection();
#else
            AttemptConnection();
#endif
        }
        else
        {
            // All servers failed
            HandleConnectionFailure($"All connection attempts failed. Last error: {lastError}");
        }
    }

    /// <summary>
    /// Handles complete connection failure
    /// </summary>
    private void HandleConnectionFailure(string errorMessage)
    {
        isConnecting = false;
        connectionTimedOut = false;
        currentServerIndex = 0;
        
        // Stop timeout coroutine
        if (connectionTimeoutCoroutine != null)
        {
            StopCoroutine(connectionTimeoutCoroutine);
            connectionTimeoutCoroutine = null;
        }
        
        Debug.LogError($"Socket.IO connection failed: {errorMessage}");
        OnSocketError?.Invoke(errorMessage);
        OnConnectionStatusChanged?.Invoke(false);
        
        // Try to reconnect after delay if within retry limits
        if (reconnectAttempts < maxReconnectAttempts)
        {
            StartReconnect();
        }
    }

    /// <summary>
    /// Cleans up existing socket instance
    /// </summary>
    private void CleanupSocket()
    {
#if WEBGL_BUILD
        if (webglSocket != null && webglSocket.IsConnected)
        {
            try
            {
                webglSocket.Disconnect();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error during WebGL socket cleanup: {e.Message}");
            }
        }
#else
        if (socket != null)
        {
            try
            {
                socket.OnConnected -= OnSocketIOConnected;
                socket.OnDisconnected -= OnSocketIODisconnected;
                socket.OnError -= OnSocketIOError;
                
                if (socket.Connected)
                {
                    socket.DisconnectAsync();
                }
                
                socket.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error during socket cleanup: {e.Message}");
            }
            finally
            {
                socket = null;
            }
        }
#endif
    }

    private void OnSocketIOConnected(object sender, EventArgs e)
    {
        Debug.Log("SocketIOManager OnSocketIOConnected");
        isConnecting = false;
        connectionTimedOut = false;
        reconnectAttempts = 0;
        
        // Stop connection timeout
        if (connectionTimeoutCoroutine != null)
        {
            StopCoroutine(connectionTimeoutCoroutine);
            connectionTimeoutCoroutine = null;
        }

        string connectedUrl = GetCurrentServerUrl();
        if (showDebugInfo)
        {
            Debug.Log($"✅ Socket.IO connected successfully to: {connectedUrl}");
            Debug.Log($"Socket ID: {socket?.Id ?? "unknown"}");
        }

        OnSocketConnected?.Invoke();
        OnConnectionStatusChanged?.Invoke(false);
        
        // Upgrade to WebSocket if we started with polling
        if (socket != null && showDebugInfo)
        {
            Debug.Log("Connection established, ready for data transmission");
        }
    }

    private void OnSocketIODisconnected(object sender, string reason)
    {
        Debug.Log("SocketIOManager OnSocketIODisconnected");
        isConnecting = false;
        if (showDebugInfo)
            Debug.Log($"Socket.IO disconnected: {reason}");

        OnSocketDisconnected?.Invoke();
        OnConnectionStatusChanged?.Invoke(false);

        // Attempt to reconnect if not manually disconnected
        if (reason != "io client disconnect" && reconnectAttempts < maxReconnectAttempts)
        {
            StartReconnect();
        }
    }

    private void OnSocketIOError(object sender, string error)
    {
        Debug.Log("SocketIOManager OnSocketIOError");
        isConnecting = false;
        string errorMessage = $"Socket.IO error: {error}";
        Debug.LogError(errorMessage);
        OnSocketError?.Invoke(errorMessage);
        OnConnectionStatusChanged?.Invoke(false);
    }

    private void OnScoreEventReceived(SocketIOClient.SocketIOResponse response)
    {
        Debug.Log("SocketIOManager OnScoreEventReceived");
        try
        {
            string jsonData = response.GetValue().GetRawText();
            if (showDebugInfo)
                Debug.Log($"Score event received: {jsonData}");

            SocketIOResponse scoreResponse = JsonUtility.FromJson<SocketIOResponse>(jsonData);
            if (scoreResponse != null)
            {
                OnScoreReceived?.Invoke(scoreResponse);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing score event: {e.Message}");
        }
    }

    private void OnMessageEventReceived(SocketIOClient.SocketIOResponse response)
    {
        Debug.Log("SocketIOManager OnMessageEventReceived");
        try
        {
            string message = response.GetValue().GetRawText();
            if (showDebugInfo)
                Debug.Log($"Message event received: {message}");

            OnMessageReceived?.Invoke(message);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing message event: {e.Message}");
        }
    }

#if WEBGL_BUILD
    /// <summary>
    /// WebGL-specific connection event handler
    /// </summary>
    private void OnWebGLSocketConnected()
    {
        Debug.Log("[SocketIOManager] WebGL Socket.IO connected");
        isConnecting = false;
        connectionTimedOut = false;
        reconnectAttempts = 0;
        
        // Stop connection timeout
        if (connectionTimeoutCoroutine != null)
        {
            StopCoroutine(connectionTimeoutCoroutine);
            connectionTimeoutCoroutine = null;
        }

        if (showDebugInfo)
        {
            string connectedUrl = GetCurrentServerUrl();
            Debug.Log($"✅ WebGL Socket.IO connected successfully to: {connectedUrl}");
        }

        OnSocketConnected?.Invoke();
        OnConnectionStatusChanged?.Invoke(false);
    }
    
    /// <summary>
    /// WebGL-specific disconnection event handler
    /// </summary>
    private void OnWebGLSocketDisconnected()
    {
        Debug.Log("[SocketIOManager] WebGL Socket.IO disconnected");
        isConnecting = false;
        
        OnSocketDisconnected?.Invoke();
        OnConnectionStatusChanged?.Invoke(false);

        // Attempt to reconnect if within retry limits
        if (reconnectAttempts < maxReconnectAttempts)
        {
            StartReconnect();
        }
    }
    
    /// <summary>
    /// WebGL-specific error event handler
    /// </summary>
    private void OnWebGLSocketError(string error)
    {
        Debug.Log($"[SocketIOManager] WebGL Socket.IO error: {error}");
        isConnecting = false;
        
        OnSocketError?.Invoke(error);
        OnConnectionStatusChanged?.Invoke(false);
    }
    
    /// <summary>
    /// WebGL-specific score received event handler
    /// </summary>
    private void OnWebGLScoreReceived(SocketIOResponse response)
    {
        Debug.Log($"[SocketIOManager] WebGL score received: MessageId={response.messageId}, Value={response.value}");
        OnScoreReceived?.Invoke(response);
    }
    
    /// <summary>
    /// WebGL-specific message received event handler
    /// </summary>
    private void OnWebGLMessageReceived(string message)
    {
        Debug.Log($"[SocketIOManager] WebGL message received: {message}");
        OnMessageReceived?.Invoke(message);
    }
#endif

    private void StartReconnect()
    {
        Debug.Log("SocketIOManager StartReconnect");
        if (reconnectCoroutine != null)
        {
            StopCoroutine(reconnectCoroutine);
        }

#if WEBGL_BUILD
        reconnectCoroutine = StartCoroutine(WebGLReconnectCoroutine());
#else
        reconnectCoroutine = StartCoroutine(ReconnectCoroutine());
#endif
    }

    private IEnumerator ReconnectCoroutine()
    {
        Debug.Log("SocketIOManager ReconnectCoroutine");
        reconnectAttempts++;
        
        if (showDebugInfo)
            Debug.Log($"Attempting to reconnect... (Attempt {reconnectAttempts}/{maxReconnectAttempts})");

        yield return new WaitForSeconds(reconnectDelay);

        ConnectToSocketIO();
    }

#if WEBGL_BUILD
    /// <summary>
    /// WebGL-specific reconnection coroutine using callback-based approach
    /// </summary>
    private IEnumerator WebGLReconnectCoroutine()
    {
        Debug.Log("[SocketIOManager] WebGL ReconnectCoroutine");
        reconnectAttempts++;
        
        if (showDebugInfo)
            Debug.Log($"[WebGL] Attempting to reconnect... (Attempt {reconnectAttempts}/{maxReconnectAttempts})");

        // Use exponential backoff for WebGL reconnection
        float delay = reconnectDelay * Mathf.Pow(1.5f, reconnectAttempts - 1);
        delay = Mathf.Min(delay, 30f); // Cap at 30 seconds
        
        if (showDebugInfo)
            Debug.Log($"[WebGL] Waiting {delay:F1} seconds before reconnection attempt...");
        
        yield return new WaitForSeconds(delay);

        if (reconnectAttempts > maxReconnectAttempts)
        {
            Debug.LogError($"[WebGL] Maximum reconnection attempts ({maxReconnectAttempts}) reached. Giving up.");
            OnSocketError?.Invoke("Maximum WebGL reconnection attempts reached");
            yield break;
        }

        // Use callback helper for WebGL-compatible reconnection
        WebGLCallbackHelper.Instance.ExecuteWithTimeout(
            () => ConnectToSocketIO(),
            connectionTimeout,
            (success, result) =>
            {
                if (success)
                {
                    if (showDebugInfo)
                        Debug.Log($"[WebGL] Reconnection attempt {reconnectAttempts} succeeded");
                }
                else
                {
                    if (showDebugInfo)
                        Debug.LogWarning($"[WebGL] Reconnection attempt {reconnectAttempts} failed: {result}");
                    
                    // Retry if we haven't exceeded max attempts
                    if (reconnectAttempts < maxReconnectAttempts)
                    {
                        StartReconnect();
                    }
                }
            }
        );
    }
#endif

    /// <summary>
    /// Disconnects from the Socket.IO server
    /// </summary>
    public void DisconnectFromSocketIO()
    {
        Debug.Log("SocketIOManager DisconnectFromSocketIO");
        
#if WEBGL_BUILD
        if (webglSocket != null && webglSocket.IsConnected)
        {
            if (showDebugInfo)
                Debug.Log("Disconnecting from WebGL Socket.IO server");
            
            webglSocket.Disconnect();
        }
#else
        if (socket != null && socket.Connected)
        {
            if (showDebugInfo)
                Debug.Log("Disconnecting from Socket.IO server");

            // Use callback-based approach for WebGL compatibility
            StartCoroutine(DisconnectCoroutine());
        }
#endif
    }
    
#if !WEBGL_BUILD
    /// <summary>
    /// Coroutine to handle async disconnection in a WebGL-compatible way
    /// </summary>
    private IEnumerator DisconnectCoroutine()
    {
        if (socket != null && socket.Connected)
        {
            var disconnectTask = socket.DisconnectAsync();
            
            // Wait for the task to complete
            while (!disconnectTask.IsCompleted)
            {
                yield return null;
            }
            
            if (disconnectTask.IsFaulted)
            {
                Debug.LogError($"Error during disconnect: {disconnectTask.Exception?.GetBaseException()?.Message}");
            }
        }
    }
#endif

    /// <summary>
    /// Sends default scoring data via Socket.IO
    /// </summary>
    public void SendDefaultScore(float score)
    {
        Debug.Log("SocketIOManager SendDefaultScore");
        GameData gameData = new GameData
        {
            game = gameId,
            data = new List<GameMetric>
            {
                new GameMetric { score = score }
            }
        };

        SendGameData(gameData);
    }

    /// <summary>
    /// Sends platformer scoring data
    /// </summary>
    public void SendPlatformerScore(int victim = 0, int streak = 0)
    {
        Debug.Log("SocketIOManager SendPlatformerScore");
        GameData gameData = new GameData
        {
            game = "platformer",
            data = new List<GameMetric>
            {
                new GameMetric 
                { 
                    victim = victim,
                    streak = streak
                }
            }
        };

        SendGameData(gameData);
    }

    /// <summary>
    /// Sends aim scoring data
    /// </summary>
    public void SendAimScore(string type, float precision = 0f, int age = 0, int nth = 0)
    {
        Debug.Log("SocketIOManager SendAimScore");
        GameData gameData = new GameData
        {
            game = "aim-gridshot",
            messageId = $"p{++messageCounter}",
            data = new List<GameMetric>
            {
                new GameMetric 
                { 
                    type = type,
                    precision = precision,
                    age = age,
                    nth = nth
                }
            }
        };

        SendGameData(gameData);
    }

    /// <summary>
    /// Sends multitasking scoring data
    /// </summary>
    public void SendMultitaskingScore(float score, bool obstacleBlock = false, int barsActive = 0, List<string> targetClicks = null)
    {
        GameData gameData = new GameData
        {
            game = "multitasking",
            data = new List<GameMetric>
            {
                new GameMetric 
                { 
                    score = score,
                    obstacleBlock = obstacleBlock,
                    barsActive = barsActive,
                    targetClicks = targetClicks ?? new List<string>()
                }
            }
        };

        SendGameData(gameData);
    }

    /// <summary>
    /// Sends observe scoring data
    /// </summary>
    public void SendObserveScore(float score, string question, string answer)
    {
        Debug.Log("SocketIOManager SendObserveScore");
        GameData gameData = new GameData
        {
            game = "observe",
            data = new List<GameMetric>
            {
                new GameMetric 
                { 
                    score = score,
                    question = question,
                    answer = answer
                }
            }
        };

        SendGameData(gameData);
    }

    /// <summary>
    /// Sends HoldTheWall scoring data
    /// </summary>
    public void SendHoldTheWallScore(float timeElapsed, float score)
    {
        Debug.Log("SocketIOManager SendHoldTheWallScore");
        GameData gameData = new GameData
        {
            game = "holdthewall",
            timeElapsed = timeElapsed,
            data = new List<GameMetric>
            {
                new GameMetric 
                { 
                    score = score
                }
            }
        };

        SendGameData(gameData);
    }

    /// <summary>
    /// Sends ButtonSamsh scoring data
    /// </summary>
    public void SendButtonSamshScore(float score)
    {
        Debug.Log("SocketIOManager SendButtonSamshScore");
        GameData gameData = new GameData
        {
            game = "buttonsmash",
            data = new List<GameMetric>
            {
                new GameMetric 
                { 
                    score = score
                }
            }
        };

        SendGameData(gameData);
    }

    /// <summary>
    /// Sends StayOnTarget scoring data
    /// </summary>
    public void SendStayOnTargetScore(float timeElapsed, float score)
    {
        Debug.Log("SocketIOManager SendStayOnTargetScore");
        GameData gameData = new GameData
        {
            game = "stayontarget",
            timeElapsed = timeElapsed,
            data = new List<GameMetric>
            {
                new GameMetric 
                { 
                    score = score
                }
            }
        };

        SendGameData(gameData);
    }

    /// <summary>
    /// Sends custom game data via Socket.IO events
    /// </summary>
    public void SendGameData(GameData gameData)
    {
        Debug.Log("SocketIOManager SendGameData");
        if (!IsConnected)
        {
            Debug.LogWarning("Cannot send data - Socket.IO not connected");
            return;
        }

        try
        {
#if WEBGL_BUILD
            // Use WebGL implementation with enhanced JSON serialization
            if (showDebugInfo)
                Debug.Log($"Sending WebGL Socket.IO game data: {WebGLJsonHelper.SerializeGameData(gameData)}");
            
            webglSocket.SendGameData(gameData);
#else
            // Use standard SocketIOUnity implementation
            if (showDebugInfo)
                Debug.Log($"Sending Socket.IO game data: {JsonUtility.ToJson(gameData)}");
            
            string jsonData = JsonUtility.ToJson(gameData);
            socket.EmitStringAsJSON("gameData", jsonData);
#endif
        }
        catch (Exception e)
        {
            string errorMessage = $"Failed to send Socket.IO data: {e.Message}";
            Debug.LogError(errorMessage);
            OnSocketError?.Invoke(errorMessage);
        }
    }

    /// <summary>
    /// Sends a simple test message
    /// </summary>
    public void SendTestMessage()
    {
        Debug.Log("SocketIOManager SendTestMessage");
        GameData testData = new GameData
        {
            game = gameId,
            data = new List<GameMetric>
            {
                new GameMetric { score = 100f }
            }
        };

        SendGameData(testData);
    }

    /// <summary>
    /// Manually trigger Socket.IO connection (for testing)
    /// </summary>
    [ContextMenu("Test Socket.IO Connection")]
    public void TestSocketIOConnection()
    {
        Debug.Log("SocketIOManager TestSocketIOConnection");
        ConnectToSocketIO();
    }

    /// <summary>
    /// Manually send test data (for testing)
    /// </summary>
    [ContextMenu("Test Send Data")]
    public void TestSendData()
    {
        Debug.Log("SocketIOManager TestSendData");
        if (IsConnected)
        {
            SendTestMessage();
        }
        else
        {
            Debug.LogWarning("Socket.IO not connected - cannot send test data");
        }
    }

    /// <summary>
    /// Diagnose connection issues and test server accessibility
    /// </summary>
    [ContextMenu("Diagnose Connection")]
    public void DiagnoseConnection()
    {
        Debug.Log("=== Socket.IO Connection Diagnosis ===");
        Debug.Log($"Primary Server: {serverUrl}:{serverPort}");
        Debug.Log($"Connection Timeout: {connectionTimeout}s");
        Debug.Log($"Use Fallback Servers: {useFallbackServers}");
        Debug.Log($"Current Server Index: {currentServerIndex}");
        Debug.Log($"Reconnect Attempts: {reconnectAttempts}/{maxReconnectAttempts}");
        
        if (fallbackUrls != null && fallbackUrls.Count > 0)
        {
            Debug.Log("Fallback Servers:");
            for (int i = 0; i < fallbackUrls.Count; i++)
            {
                Debug.Log($"  {i + 1}. {fallbackUrls[i]}");
            }
        }
        
#if WEBGL_BUILD
        Debug.Log($"Platform: WebGL Build");
        if (webglSocket != null)
        {
            Debug.Log($"WebGL Socket State: Connected={webglSocket.IsConnected}");
            Debug.Log($"WebGL Socket Status: {webglSocket.GetConnectionStatus()}");
        }
        else
        {
            Debug.Log("WebGL Socket instance is null");
        }
#else
        Debug.Log($"Platform: Standalone Build");
        if (socket != null)
        {
            Debug.Log($"Socket State: Connected={socket.Connected}, ID={socket.Id ?? "null"}");
        }
        else
        {
            Debug.Log("Socket instance is null");
        }
#endif
        
        Debug.Log($"Is Connecting: {isConnecting}");
        Debug.Log($"Connection Timed Out: {connectionTimedOut}");
        
        // Test server accessibility
        StartCoroutine(TestServerAccessibility());
    }

    /// <summary>
    /// Tests server accessibility via HTTP request
    /// </summary>
    private IEnumerator TestServerAccessibility()
    {
        List<string> urlsToTest = new List<string>();
        
        // Add primary server
        string primaryUrl = GetCurrentServerUrl();
        if (!primaryUrl.EndsWith("/socket.io/"))
        {
            primaryUrl = primaryUrl.TrimEnd('/') + "/socket.io/";
        }
        urlsToTest.Add(primaryUrl);
        
        // Add fallback servers
        if (useFallbackServers && fallbackUrls != null)
        {
            foreach (string fallbackUrl in fallbackUrls)
            {
                string cleanFallbackUrl = fallbackUrl?.Trim() ?? "";
                if (!string.IsNullOrEmpty(cleanFallbackUrl))
                {
                    if (!cleanFallbackUrl.EndsWith("/socket.io/"))
                    {
                        cleanFallbackUrl = cleanFallbackUrl.TrimEnd('/') + "/socket.io/";
                    }
                    urlsToTest.Add(cleanFallbackUrl);
                }
            }
        }
        
        foreach (string testUrl in urlsToTest)
        {
            Debug.Log($"Testing server accessibility: {testUrl}");
            
            using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(testUrl))
            {
                request.timeout = 5; // 5 second timeout
                yield return request.SendWebRequest();
                
                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Debug.Log($"✅ Server accessible: {testUrl}");
                    Debug.Log($"Response: {request.downloadHandler.text}");
                }
                else
                {
                    Debug.LogError($"❌ Server not accessible: {testUrl}");
                    Debug.LogError($"Error: {request.error} (Code: {request.responseCode})");
                }
            }
        }
    }

    private void OnDestroy()
    {
        Debug.Log("SocketIOManager OnDestroy");
        
        // Stop all coroutines
        if (reconnectCoroutine != null)
        {
            StopCoroutine(reconnectCoroutine);
            reconnectCoroutine = null;
        }
        
        if (connectionTimeoutCoroutine != null)
        {
            StopCoroutine(connectionTimeoutCoroutine);
            connectionTimeoutCoroutine = null;
        }
        
        // Clean up Socket.IO
        CleanupSocket();

        // Unsubscribe from events
        if (APIConnector.Instance != null)
        {
            APIConnector.Instance.OnTestCanStart -= OnTestCanStart;
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        Debug.Log("SocketIOManager OnApplicationPause");
        // Handle app pause/resume
        if (pauseStatus && IsConnected)
        {
            if (showDebugInfo)
                Debug.Log("App paused - Socket.IO connection will be maintained");
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        Debug.Log("SocketIOManager OnApplicationFocus");
        // Handle app focus changes
        if (!hasFocus && IsConnected)
        {
            if (showDebugInfo)
                Debug.Log("App lost focus - Socket.IO connection will be maintained");
        }
    }

#if WEBGL_BUILD
    /// <summary>
    /// WebGL-specific method to get detailed connection status
    /// </summary>
    public string GetWebGLConnectionStatus()
    {
        if (webglSocket != null)
        {
            return webglSocket.GetConnectionStatus();
        }
        return "WebGL socket not initialized";
    }
    
    /// <summary>
    /// WebGL-specific method to send custom events
    /// </summary>
    public void SendWebGLEvent(string eventName, object data = null)
    {
        if (webglSocket != null && webglSocket.IsConnected)
        {
            webglSocket.SendEvent(eventName, data);
        }
        else
        {
            Debug.LogWarning("Cannot send WebGL event - not connected");
        }
    }
    
    /// <summary>
    /// WebGL-specific method to check if the JavaScript Socket.IO library is available
    /// </summary>
    public bool IsWebGLSocketIOAvailable()
    {
        return webglSocket != null;
    }
    
    /// <summary>
    /// Test WebGL JSON serialization for debugging
    /// </summary>
    [ContextMenu("Test WebGL JSON Serialization")]
    public void TestWebGLJsonSerialization()
    {
        WebGLJsonHelper.TestSerialization();
    }
    
    /// <summary>
    /// Run comprehensive WebGL Socket.IO tests
    /// </summary>
    [ContextMenu("Run WebGL Socket.IO Tests")]
    public void RunWebGLTests()
    {
        WebGLSocketIOTester tester = FindObjectOfType<WebGLSocketIOTester>();
        if (tester == null)
        {
            GameObject testerGO = new GameObject("WebGLSocketIOTester");
            tester = testerGO.AddComponent<WebGLSocketIOTester>();
        }
        tester.StartComprehensiveTest();
    }
    
    /// <summary>
    /// Quick connection status check
    /// </summary>
    [ContextMenu("Quick Connection Check")]
    public void QuickConnectionCheck()
    {
        Debug.Log("=== Quick Connection Check ===");
        Debug.Log($"Platform: {GetPlatformConnectionStatus()}");
        Debug.Log($"Is Connected: {IsConnected}");
        Debug.Log($"Is Connecting: {IsConnecting}");
        
#if WEBGL_BUILD
        if (IsWebGLSocketIOAvailable())
        {
            Debug.Log($"WebGL Status: {GetWebGLConnectionStatus()}");
        }
#endif
        
        if (!IsConnected && !IsConnecting)
        {
            Debug.Log("Attempting connection...");
            ConnectToSocketIO();
        }
    }
#endif

    // Backward compatibility methods for existing code
    public void ConnectToWebSocket() => ConnectToSocketIO();
    public void DisconnectFromWebSocket() => DisconnectFromSocketIO();
    public void SendWebSocketData(GameData gameData) => SendGameData(gameData);
    
    /// <summary>
    /// Get platform-specific connection status information
    /// </summary>
    public string GetPlatformConnectionStatus()
    {
#if WEBGL_BUILD
        return GetWebGLConnectionStatus();
#else
        if (socket != null)
        {
            return $"SocketIOUnity - Connected: {socket.Connected}, ID: {socket.Id ?? "null"}";
        }
        return "SocketIOUnity - Not initialized";
#endif
    }
}
