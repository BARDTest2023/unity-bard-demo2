using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// WebGL-specific Socket.IO implementation using JavaScript interop
/// This class provides the bridge between Unity C# and the JavaScript Socket.IO plugin
/// </summary>
public class SocketIOWebGL : MonoBehaviour
{
    #if UNITY_WEBGL && !UNITY_EDITOR
    
    // JavaScript function imports - these call the functions in our socketio.jslib plugin
    [DllImport("__Internal")]
    private static extern int SocketIO_Initialize(string serverUrl, int serverPort, string gameObjectName);
    
    [DllImport("__Internal")]
    private static extern int SocketIO_Connect();
    
    [DllImport("__Internal")]
    private static extern int SocketIO_Disconnect();
    
    [DllImport("__Internal")]
    private static extern int SocketIO_IsConnected();
    
    [DllImport("__Internal")]
    private static extern int SocketIO_EmitGameData(string gameDataJson);
    
    [DllImport("__Internal")]
    private static extern int SocketIO_Emit(string eventName, string dataJson);
    
    [DllImport("__Internal")]
    private static extern string SocketIO_GetStatus();
    
    [DllImport("__Internal")]
    private static extern void SocketIO_Cleanup();
    
    #endif
    
    [Header("WebGL Socket.IO Configuration")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Events that mirror the main SocketIOManager events
    public event Action OnSocketConnected;
    public event Action OnSocketDisconnected;
    public event Action<string> OnSocketError;
    public event Action<SocketIOResponse> OnScoreReceived;
    public event Action<string> OnMessageReceived;
    
    // Connection state
    private bool isInitialized = false;
    private bool isConnecting = false;
    
    public bool IsConnected 
    { 
        get 
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            return isInitialized && SocketIO_IsConnected() == 1;
            #else
            return false;
            #endif
        } 
    }
    
    public bool IsConnecting => isConnecting;
    
    /// <summary>
    /// Initialize the WebGL Socket.IO connection
    /// </summary>
    /// <param name="serverUrl">Server URL (e.g., "http://test.bardtest.gg")</param>
    /// <param name="serverPort">Server port (e.g., 80)</param>
    /// <returns>True if initialization was successful</returns>
    public bool Initialize(string serverUrl, int serverPort)
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        
        if (isInitialized)
        {
            if (showDebugInfo)
                Debug.Log("[SocketIOWebGL] Already initialized, skipping...");
            return true;
        }
        
        if (showDebugInfo)
            Debug.Log($"[SocketIOWebGL] Initializing connection to {serverUrl}:{serverPort}");
        
        try
        {
            // Use this GameObject's name for JavaScript callbacks
            string callbackObjectName = gameObject.name;
            int result = SocketIO_Initialize(serverUrl, serverPort, callbackObjectName);
            
            if (result == 1)
            {
                isInitialized = true;
                if (showDebugInfo)
                    Debug.Log("[SocketIOWebGL] Initialization successful");
                return true;
            }
            else
            {
                Debug.LogError("[SocketIOWebGL] Initialization failed");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SocketIOWebGL] Initialization exception: {e.Message}");
            return false;
        }
        
        #else
        
        if (showDebugInfo)
            Debug.LogWarning("[SocketIOWebGL] Initialize called on non-WebGL platform");
        return false;
        
        #endif
    }
    
    /// <summary>
    /// Connect to the Socket.IO server
    /// </summary>
    public void Connect()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        
        if (!isInitialized)
        {
            Debug.LogError("[SocketIOWebGL] Cannot connect - not initialized. Call Initialize() first.");
            return;
        }
        
        if (IsConnected)
        {
            if (showDebugInfo)
                Debug.Log("[SocketIOWebGL] Already connected, skipping...");
            return;
        }
        
        if (showDebugInfo)
            Debug.Log("[SocketIOWebGL] Attempting to connect...");
        
        isConnecting = true;
        
        try
        {
            int result = SocketIO_Connect();
            if (result != 1)
            {
                isConnecting = false;
                Debug.LogError("[SocketIOWebGL] Connect call failed");
            }
            // Note: Actual connection success/failure will be reported via JavaScript callbacks
        }
        catch (Exception e)
        {
            isConnecting = false;
            Debug.LogError($"[SocketIOWebGL] Connect exception: {e.Message}");
            OnSocketError?.Invoke($"Connect exception: {e.Message}");
        }
        
        #else
        
        if (showDebugInfo)
            Debug.LogWarning("[SocketIOWebGL] Connect called on non-WebGL platform");
        
        #endif
    }
    
    /// <summary>
    /// Disconnect from the Socket.IO server
    /// </summary>
    public void Disconnect()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        
        if (!isInitialized)
        {
            if (showDebugInfo)
                Debug.Log("[SocketIOWebGL] Not initialized, nothing to disconnect");
            return;
        }
        
        if (showDebugInfo)
            Debug.Log("[SocketIOWebGL] Attempting to disconnect...");
        
        try
        {
            SocketIO_Disconnect();
            isConnecting = false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SocketIOWebGL] Disconnect exception: {e.Message}");
        }
        
        #else
        
        if (showDebugInfo)
            Debug.LogWarning("[SocketIOWebGL] Disconnect called on non-WebGL platform");
        
        #endif
    }
    
    /// <summary>
    /// Send game data to the server
    /// </summary>
    /// <param name="gameData">Game data object to send</param>
    public void SendGameData(GameData gameData)
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        
        if (!IsConnected)
        {
            Debug.LogError("[SocketIOWebGL] Cannot send game data - not connected");
            OnSocketError?.Invoke("Cannot send game data - not connected");
            return;
        }
        
        try
        {
            string gameDataJson = WebGLJsonHelper.SerializeGameData(gameData);
            
            if (showDebugInfo)
                Debug.Log($"[SocketIOWebGL] Sending game data: {gameDataJson}");
            
            int result = SocketIO_EmitGameData(gameDataJson);
            
            if (result != 1)
            {
                Debug.LogError("[SocketIOWebGL] Failed to send game data");
                OnSocketError?.Invoke("Failed to send game data");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SocketIOWebGL] SendGameData exception: {e.Message}");
            OnSocketError?.Invoke($"SendGameData exception: {e.Message}");
        }
        
        #else
        
        if (showDebugInfo)
            Debug.LogWarning("[SocketIOWebGL] SendGameData called on non-WebGL platform");
        
        #endif
    }
    
    /// <summary>
    /// Send a custom event with optional data
    /// </summary>
    /// <param name="eventName">Name of the event</param>
    /// <param name="data">Optional data object</param>
    public void SendEvent(string eventName, object data = null)
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        
        if (!IsConnected)
        {
            Debug.LogError($"[SocketIOWebGL] Cannot send event '{eventName}' - not connected");
            return;
        }
        
        try
        {
            string dataJson = data != null ? WebGLJsonHelper.SerializeObject(data) : "";
            
            if (showDebugInfo)
                Debug.Log($"[SocketIOWebGL] Sending event '{eventName}' with data: {dataJson}");
            
            int result = SocketIO_Emit(eventName, dataJson);
            
            if (result != 1)
            {
                Debug.LogError($"[SocketIOWebGL] Failed to send event '{eventName}'");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SocketIOWebGL] SendEvent exception: {e.Message}");
            OnSocketError?.Invoke($"SendEvent exception: {e.Message}");
        }
        
        #else
        
        if (showDebugInfo)
            Debug.LogWarning($"[SocketIOWebGL] SendEvent '{eventName}' called on non-WebGL platform");
        
        #endif
    }
    
    /// <summary>
    /// Get the current connection status as a string
    /// </summary>
    public string GetConnectionStatus()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        
        if (!isInitialized)
            return "Not initialized";
        
        try
        {
            return SocketIO_GetStatus();
        }
        catch (Exception e)
        {
            return $"Error getting status: {e.Message}";
        }
        
        #else
        
        return "WebGL only";
        
        #endif
    }
    
    // ===========================================
    // JavaScript Callback Methods
    // These methods are called by our JavaScript plugin via SendMessage()
    // ===========================================
    
    /// <summary>
    /// Called by JavaScript when Socket.IO connection is established
    /// </summary>
    /// <param name="socketId">Socket ID from the server</param>
    public void OnSocketIOConnected(string socketId)
    {
        isConnecting = false;
        
        if (showDebugInfo)
            Debug.Log($"[SocketIOWebGL] Connected with socket ID: {socketId}");
        
        OnSocketConnected?.Invoke();
    }
    
    /// <summary>
    /// Called by JavaScript when Socket.IO connection is lost
    /// </summary>
    /// <param name="reason">Disconnection reason</param>
    public void OnSocketIODisconnected(string reason)
    {
        isConnecting = false;
        
        if (showDebugInfo)
            Debug.Log($"[SocketIOWebGL] Disconnected, reason: {reason}");
        
        OnSocketDisconnected?.Invoke();
    }
    
    /// <summary>
    /// Called by JavaScript when a Socket.IO error occurs
    /// </summary>
    /// <param name="error">Error message</param>
    public void OnSocketIOError(string error)
    {
        isConnecting = false;
        
        if (showDebugInfo)
            Debug.LogError($"[SocketIOWebGL] Socket.IO error: {error}");
        
        OnSocketError?.Invoke(error);
    }
    
    /// <summary>
    /// Called by JavaScript when a score event is received
    /// </summary>
    /// <param name="scoreJson">JSON string containing score data</param>
    public void OnSocketIOScoreReceived(string scoreJson)
    {
        if (showDebugInfo)
            Debug.Log($"[SocketIOWebGL] Score received: {scoreJson}");
        
        try
        {
            SocketIOResponse response = WebGLJsonHelper.DeserializeSocketIOResponse(scoreJson);
            if (response != null)
            {
                OnScoreReceived?.Invoke(response);
            }
            else
            {
                Debug.LogError($"[SocketIOWebGL] Failed to parse score JSON: {scoreJson}");
                OnSocketError?.Invoke("Failed to parse score response");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SocketIOWebGL] Error parsing score JSON: {e.Message}");
            OnSocketError?.Invoke($"Score parsing error: {e.Message}");
        }
    }
    
    /// <summary>
    /// Called by JavaScript when a message event is received
    /// </summary>
    /// <param name="message">Message string</param>
    public void OnSocketIOMessageReceived(string message)
    {
        if (showDebugInfo)
            Debug.Log($"[SocketIOWebGL] Message received: {message}");
        
        OnMessageReceived?.Invoke(message);
    }
    
    // ===========================================
    // Unity Lifecycle Methods
    // ===========================================
    
    private void OnDestroy()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        
        if (isInitialized)
        {
            if (showDebugInfo)
                Debug.Log("[SocketIOWebGL] Cleaning up on destroy...");
            
            try
            {
                SocketIO_Cleanup();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SocketIOWebGL] Cleanup exception: {e.Message}");
            }
        }
        
        #endif
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && IsConnected)
        {
            if (showDebugInfo)
                Debug.Log("[SocketIOWebGL] Application paused, maintaining connection...");
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && IsConnected)
        {
            if (showDebugInfo)
                Debug.Log("[SocketIOWebGL] Application lost focus, maintaining connection...");
        }
    }
}

