using System.Collections;
using UnityEngine;
using NativeWebSocket;

public class WebSocketQuickTest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool runTestOnStart = true;
    [SerializeField] private string testUrl = "wss://test.bardtest.gg/websocket";

    private WebSocket webSocket;

    private void Start()
    {
        if (runTestOnStart)
        {
            StartCoroutine(RunQuickTest());
        }
    }

    private IEnumerator RunQuickTest()
    {
        Debug.Log("=== WebSocket Quick Test Starting ===");
        
        // Test 1: Create WebSocket instance
        Debug.Log("Test 1: Creating WebSocket instance...");
        webSocket = new WebSocket(testUrl);
        
        if (webSocket != null)
        {
            Debug.Log("✅ WebSocket instance created successfully");
        }
        else
        {
            Debug.LogError("❌ Failed to create WebSocket instance");
            yield break;
        }

        // Test 2: Subscribe to events
        Debug.Log("Test 2: Subscribing to WebSocket events...");
        webSocket.OnOpen += OnWebSocketOpen;
        webSocket.OnMessage += OnWebSocketMessage;
        webSocket.OnError += OnWebSocketError;
        webSocket.OnClose += OnWebSocketClose;
        
        Debug.Log("✅ Event subscriptions completed");

        // Test 3: Connect to WebSocket
        Debug.Log("Test 3: Connecting to WebSocket...");
        webSocket.Connect();
        
        // Wait for connection
        yield return new WaitForSeconds(2f);

        // Test 4: Send test message
        if (webSocket.IsConnected)
        {
            Debug.Log("Test 4: Sending test message...");
            string testMessage = "{\"game\":\"test\",\"data\":[{\"score\":100}]}";
            webSocket.Send(testMessage);
            Debug.Log("✅ Test message sent");
        }
        else
        {
            Debug.LogWarning("⚠️ WebSocket not connected - skipping message test");
        }

        // Test 5: Dispatch message queue
        Debug.Log("Test 5: Dispatching message queue...");
        webSocket.DispatchMessageQueue();
        
        yield return new WaitForSeconds(1f);

        // Test 6: Close WebSocket
        Debug.Log("Test 6: Closing WebSocket...");
        webSocket.Close();
        
        yield return new WaitForSeconds(1f);

        Debug.Log("=== WebSocket Quick Test Completed ===");
    }

    private void OnWebSocketOpen()
    {
        Debug.Log("✅ WebSocket opened successfully");
    }

    private void OnWebSocketMessage(string message)
    {
        Debug.Log($"✅ WebSocket message received: {message}");
    }

    private void OnWebSocketError(string error)
    {
        Debug.LogError($"❌ WebSocket error: {error}");
    }

    private void OnWebSocketClose(WebSocketCloseCode code)
    {
        Debug.Log($"✅ WebSocket closed with code: {code}");
    }

    private void OnDestroy()
    {
        if (webSocket != null)
        {
            webSocket.Dispose();
        }
    }

    [ContextMenu("Run Quick Test")]
    public void RunTestFromContext()
    {
        StartCoroutine(RunQuickTest());
    }
} 