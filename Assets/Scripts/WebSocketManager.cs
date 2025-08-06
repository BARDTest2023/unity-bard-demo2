using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using NativeWebSocket;

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
}

[System.Serializable]
public class WebSocketResponse
{
    public string messageId;
    public float value;
}

public class WebSocketManager : MonoBehaviour
{
    [Header("WebSocket Configuration")]
    [SerializeField] private string wsUrl = "wss://test.bardtest.gg/websocket";
    [SerializeField] private string gameId = "unity-demo";
    [SerializeField] private float reconnectDelay = 5f;
    [SerializeField] private int maxReconnectAttempts = 3;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool autoConnect = true;

    public static WebSocketManager Instance { get; private set; }

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
    public event Action<WebSocketResponse> OnScoreReceived;
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
            WebSocketResponse response = JsonUtility.FromJson<WebSocketResponse>(message);
            
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
    /// Sends default scoring data to the WebSocket
    /// </summary>
    public void SendDefaultScore(float score)
    {
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
    /// Sends custom game data
    /// </summary>
    public void SendGameData(GameData gameData)
    {
        if (!IsConnected)
        {
            Debug.LogWarning("Cannot send data - WebSocket not connected");
            return;
        }

        try
        {
            string jsonData = JsonUtility.ToJson(gameData);
            
            if (showDebugInfo)
                Debug.Log($"Sending WebSocket data: {jsonData}");

            webSocket.Send(jsonData);
        }
        catch (Exception e)
        {
            string errorMessage = $"Failed to send WebSocket data: {e.Message}";
            Debug.LogError(errorMessage);
            OnWebSocketError?.Invoke(errorMessage);
        }
    }

    /// <summary>
    /// Sends a simple test message
    /// </summary>
    public void SendTestMessage()
    {
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