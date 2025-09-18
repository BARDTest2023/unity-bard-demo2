using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using NativeWebSocket;

// NOTE: GameData, GameMetric, and SocketIOResponse classes are now defined in SocketIOManager.cs
// This file is kept for reference only and should not be used

[System.Serializable]
public class WebSocketResponse_DEPRECATED
{
    public string messageId;
    public float value;
}

// DEPRECATED: This class has been replaced by SocketIOManager
// Keeping for reference only - DO NOT USE
public class WebSocketManager_DEPRECATED : MonoBehaviour
{
    [Header("WebSocket Configuration")]
    [SerializeField] private string wsUrl = "wss://test.bardtest.gg/websocket";
    [SerializeField] private string gameId = "unity-demo";
    [SerializeField] private float reconnectDelay = 5f;
    [SerializeField] private int maxReconnectAttempts = 3;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool autoConnect = true;

    public static WebSocketManager_DEPRECATED Instance { get; private set; }

    // WebSocket instance
    private WebSocket webSocket;
    private Coroutine reconnectCoroutine;
    private int reconnectAttempts = 0;

    // Message tracking
    private int messageCounter = 0;

    // Events
    public event Action OnWebSocketConnected;
    public event Action OnWebSocketDisconnected;
    public event Action<string> OnWebSocketError;
    public event Action<WebSocketResponse_DEPRECATED> OnScoreReceived;
    public event Action<string> OnMessageReceived;
    public event Action<bool> OnConnectionStatusChanged;

    // Connection status
    public bool IsConnected => webSocket != null && webSocket.IsConnected;
    public bool IsConnecting => webSocket != null && webSocket.State == WebSocketState.Connecting;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (autoConnect)
        {
            // Wait for API validation before connecting
            if (APIConnector.Instance != null)
            {
                APIConnector.Instance.OnTestCanStart += OnTestCanStart;
            }
        }
    }

    private void Update()
    {
        // Dispatch WebSocket message queue
        if (webSocket != null)
        {
            webSocket.DispatchMessageQueue();
        }
    }

    private void OnTestCanStart(PlaySessionData data)
    {
        if (showDebugInfo)
            Debug.Log("Test can start - connecting to WebSocket...");
        
        ConnectToWebSocket();
    }

    /// <summary>
    /// Connects to the WebSocket server
    /// </summary>
    public async void ConnectToWebSocket()
    {
        if (IsConnected || IsConnecting)
        {
            if (showDebugInfo)
                Debug.Log("WebSocket already connected or connecting");
            return;
        }

        if (showDebugInfo)
            Debug.Log($"Connecting to WebSocket: {wsUrl}");

        OnConnectionStatusChanged?.Invoke(true);

        try
        {
            // Create new WebSocket instance
            webSocket = new WebSocket(wsUrl);

            // Subscribe to events
            webSocket.OnOpen += OnWebSocketOpen;
            webSocket.OnMessage += OnWebSocketMessage;
            webSocket.OnError += OnWebSocketErrorReceived;
            webSocket.OnClose += OnWebSocketClose;

            // Connect
            await webSocket.Connect();
        }
        catch (Exception e)
        {
            string errorMessage = $"Failed to create WebSocket connection: {e.Message}";
            Debug.LogError(errorMessage);
            OnWebSocketError?.Invoke(errorMessage);
            OnConnectionStatusChanged?.Invoke(false);
        }
    }

    private void OnWebSocketOpen()
    {
        reconnectAttempts = 0;

        if (showDebugInfo)
            Debug.Log("WebSocket connected successfully!");

        OnWebSocketConnected?.Invoke();
        OnConnectionStatusChanged?.Invoke(false);
    }

    private void OnWebSocketMessage(string message)
    {
        if (showDebugInfo)
            Debug.Log($"WebSocket message received: {message}");

        try
        {
            // Parse the response
            WebSocketResponse_DEPRECATED response = JsonUtility.FromJson<WebSocketResponse_DEPRECATED>(message);
            
            if (response != null)
            {
                if (showDebugInfo)
                    Debug.Log($"Score received - MessageId: {response.messageId}, Value: {response.value}");

                OnScoreReceived?.Invoke(response);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing WebSocket message: {e.Message}");
        }

        OnMessageReceived?.Invoke(message);
    }

    private void OnWebSocketErrorReceived(string error)
    {
        string errorMessage = $"WebSocket error: {error}";
        Debug.LogError(errorMessage);
        OnWebSocketError?.Invoke(errorMessage);
        OnConnectionStatusChanged?.Invoke(false);
    }

    private void OnWebSocketClose(WebSocketCloseCode code)
    {
        if (showDebugInfo)
            Debug.Log($"WebSocket closed with code: {code}");

        OnWebSocketDisconnected?.Invoke();
        OnConnectionStatusChanged?.Invoke(false);

        // Attempt to reconnect if not manually closed
        if (code != WebSocketCloseCode.Normal && reconnectAttempts < maxReconnectAttempts)
        {
            StartReconnect();
        }
    }

    private void StartReconnect()
    {
        if (reconnectCoroutine != null)
        {
            StopCoroutine(reconnectCoroutine);
        }

        reconnectCoroutine = StartCoroutine(ReconnectCoroutine());
    }

    private IEnumerator ReconnectCoroutine()
    {
        reconnectAttempts++;
        
        if (showDebugInfo)
            Debug.Log($"Attempting to reconnect... (Attempt {reconnectAttempts}/{maxReconnectAttempts})");

        yield return new WaitForSeconds(reconnectDelay);

        ConnectToWebSocket();
    }

    /// <summary>
    /// Disconnects from the WebSocket server
    /// </summary>
    public async void DisconnectFromWebSocket()
    {
        if (webSocket != null && webSocket.IsConnected)
        {
            if (showDebugInfo)
                Debug.Log("Disconnecting from WebSocket");

            await webSocket.Close();
        }
    }

    /// <summary>
    /// DEPRECATED - Use SocketIOManager.SendDefaultScore instead
    /// </summary>
    public void SendDefaultScore(float score)
    {
        Debug.LogError("WebSocketManager is DEPRECATED. Use SocketIOManager.SendDefaultScore instead.");
    }

    /// <summary>
    /// DEPRECATED - Use SocketIOManager.SendPlatformerScore instead
    /// </summary>
    public void SendPlatformerScore(int victim = 0, int streak = 0)
    {
        Debug.LogError("WebSocketManager is DEPRECATED. Use SocketIOManager.SendPlatformerScore instead.");
    }

    /// <summary>
    /// DEPRECATED - Use SocketIOManager.SendAimScore instead
    /// </summary>
    public void SendAimScore(string type, float precision = 0f, int age = 0, int nth = 0)
    {
        Debug.LogError("WebSocketManager is DEPRECATED. Use SocketIOManager.SendAimScore instead.");
    }

    /// <summary>
    /// DEPRECATED - Use SocketIOManager.SendMultitaskingScore instead
    /// </summary>
    public void SendMultitaskingScore(float score, bool obstacleBlock = false, int barsActive = 0, List<string> targetClicks = null)
    {
        Debug.LogError("WebSocketManager is DEPRECATED. Use SocketIOManager.SendMultitaskingScore instead.");
    }

    /// <summary>
    /// DEPRECATED - Use SocketIOManager.SendObserveScore instead
    /// </summary>
    public void SendObserveScore(float score, string question, string answer)
    {
        Debug.LogError("WebSocketManager is DEPRECATED. Use SocketIOManager.SendObserveScore instead.");
    }

    /// <summary>
    /// DEPRECATED - Use SocketIOManager.SendHoldTheWallScore instead
    /// </summary>
    public void SendHoldTheWallScore(float timeElapsed, float score)
    {
        Debug.LogError("WebSocketManager is DEPRECATED. Use SocketIOManager.SendHoldTheWallScore instead.");
    }

    /// <summary>
    /// DEPRECATED - Use SocketIOManager.SendButtonSamshScore instead
    /// </summary>
    public void SendButtonSamshScore(float score)
    {
        Debug.LogError("WebSocketManager is DEPRECATED. Use SocketIOManager.SendButtonSamshScore instead.");
    }

    /// <summary>
    /// DEPRECATED - Use SocketIOManager.SendStayOnTargetScore instead
    /// </summary>
    public void SendStayOnTargetScore(float timeElapsed, float score)
    {
        Debug.LogError("WebSocketManager is DEPRECATED. Use SocketIOManager.SendStayOnTargetScore instead.");
    }

    /// <summary>
    /// DEPRECATED - Use SocketIOManager.SendGameData instead
    /// </summary>
    public void SendGameData(GameData gameData)
    {
        Debug.LogError("WebSocketManager is DEPRECATED. Use SocketIOManager.SendGameData instead.");
    }

    /// <summary>
    /// DEPRECATED - Use SocketIOManager.SendTestMessage instead
    /// </summary>
    public void SendTestMessage()
    {
        Debug.LogError("WebSocketManager is DEPRECATED. Use SocketIOManager.SendTestMessage instead.");
    }

    /// <summary>
    /// Manually trigger WebSocket connection (for testing)
    /// </summary>
    [ContextMenu("Test WebSocket Connection")]
    public void TestWebSocketConnection()
    {
        ConnectToWebSocket();
    }

    /// <summary>
    /// Manually send test data (for testing)
    /// </summary>
    [ContextMenu("Test Send Data")]
    public void TestSendData()
    {
        if (IsConnected)
        {
            SendTestMessage();
        }
        else
        {
            Debug.LogWarning("WebSocket not connected - cannot send test data");
        }
    }

    private void OnDestroy()
    {
        // Clean up WebSocket
        if (webSocket != null)
        {
            webSocket.Dispose();
            webSocket = null;
        }

        // Unsubscribe from events
        if (APIConnector.Instance != null)
        {
            APIConnector.Instance.OnTestCanStart -= OnTestCanStart;
        }

        // Stop coroutines
        if (reconnectCoroutine != null)
        {
            StopCoroutine(reconnectCoroutine);
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // Handle app pause/resume
        if (pauseStatus && IsConnected)
        {
            if (showDebugInfo)
                Debug.Log("App paused - WebSocket connection will be maintained");
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // Handle app focus changes
        if (!hasFocus && IsConnected)
        {
            if (showDebugInfo)
                Debug.Log("App lost focus - WebSocket connection will be maintained");
        }
    }
} 