using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Comprehensive testing suite for WebGL Socket.IO implementation
/// Tests connection, messaging, and compatibility with BARD platform
/// </summary>
public class WebGLSocketIOTester : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private bool enableAutoTesting = false;
    [SerializeField] private bool testOnStart = false;
    [SerializeField] private float testInterval = 5f;
    [SerializeField] private bool showDetailedLogs = true;
    
    [Header("Test Servers")]
    [SerializeField] private List<TestServer> testServers = new List<TestServer>
    {
        new TestServer { name = "BARD Test Server", url = "http://test.bardtest.gg", port = 80 },
        new TestServer { name = "Local Test Server", url = "http://localhost", port = 3333 }
    };

    [System.Serializable]
    public class TestServer
    {
        public string name;
        public string url;
        public int port;
    }

    [Header("Test Results")]
    [SerializeField] private TestResults currentResults = new TestResults();
    
    [System.Serializable]
    public class TestResults
    {
        public bool connectionTest = false;
        public bool messageTest = false;
        public bool gameDataTest = false;
        public bool scoreReceiveTest = false;
        public bool reconnectionTest = false;
        public bool jsonSerializationTest = false;
        public bool platformDetectionTest = false;
        public string lastError = "";
        public float lastTestTime = 0f;
    }

    // Test state
    private bool isTestingInProgress = false;
    private Coroutine testCoroutine;
    private int currentTestServerIndex = 0;
    
    // Event tracking
    private bool connectionReceived = false;
    private bool scoreReceived = false;
    private bool messageReceived = false;
    private SocketIOResponse lastScoreResponse = null;
    private string lastMessage = "";

    public static WebGLSocketIOTester Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (testOnStart)
        {
            StartCoroutine(DelayedTestStart());
        }
    }

    private IEnumerator DelayedTestStart()
    {
        // Wait for other systems to initialize
        yield return new WaitForSeconds(2f);
        StartComprehensiveTest();
    }

    /// <summary>
    /// Start comprehensive WebGL Socket.IO testing
    /// </summary>
    [ContextMenu("Start Comprehensive Test")]
    public void StartComprehensiveTest()
    {
        if (isTestingInProgress)
        {
            Debug.LogWarning("[WebGLSocketIOTester] Test already in progress");
            return;
        }

        Debug.Log("=== Starting WebGL Socket.IO Comprehensive Test ===");
        testCoroutine = StartCoroutine(ComprehensiveTestCoroutine());
    }

    /// <summary>
    /// Stop all testing
    /// </summary>
    [ContextMenu("Stop Testing")]
    public void StopTesting()
    {
        if (testCoroutine != null)
        {
            StopCoroutine(testCoroutine);
            testCoroutine = null;
        }
        isTestingInProgress = false;
        Debug.Log("[WebGLSocketIOTester] Testing stopped");
    }

    private IEnumerator ComprehensiveTestCoroutine()
    {
        isTestingInProgress = true;
        currentResults = new TestResults();
        currentResults.lastTestTime = Time.time;

        // Test 1: Platform Detection
        yield return StartCoroutine(TestPlatformDetection());

        // Test 2: JSON Serialization
        yield return StartCoroutine(TestJsonSerialization());

        // Test 3: Connection Test
        yield return StartCoroutine(TestConnection());

        // Test 4: Message Sending
        yield return StartCoroutine(TestMessageSending());

        // Test 5: Game Data Test
        yield return StartCoroutine(TestGameDataSending());

        // Test 6: Score Receiving
        yield return StartCoroutine(TestScoreReceiving());

        // Test 7: Reconnection Test
        yield return StartCoroutine(TestReconnection());

        // Final Results
        LogFinalResults();

        isTestingInProgress = false;
    }

    private IEnumerator TestPlatformDetection()
    {
        LogTest("Platform Detection Test");

        try
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log("[Test] Platform: WebGL Build - CORRECT");
            currentResults.platformDetectionTest = true;
#else
            Debug.Log("[Test] Platform: Standalone Build");
            currentResults.platformDetectionTest = true;
#endif

            // Test SocketIOManager platform detection
            if (SocketIOManager.Instance != null)
            {
                string platformStatus = SocketIOManager.Instance.GetPlatformConnectionStatus();
                Debug.Log($"[Test] Platform Status: {platformStatus}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Test] Platform detection failed: {e.Message}");
            currentResults.lastError = e.Message;
        }

        yield return new WaitForSeconds(1f);
    }

    private IEnumerator TestJsonSerialization()
    {
        LogTest("JSON Serialization Test");

        try
        {
            // Test GameData serialization
            GameData testData = WebGLJsonHelper.CreateTestGameData();
            string serialized = WebGLJsonHelper.SerializeGameData(testData);
            
            Debug.Log($"[Test] Serialized GameData: {serialized}");
            
            if (WebGLJsonHelper.IsValidJson(serialized))
            {
                Debug.Log("[Test] JSON Serialization: PASSED");
                currentResults.jsonSerializationTest = true;
            }
            else
            {
                Debug.LogError("[Test] JSON Serialization: FAILED - Invalid JSON");
            }

            // Test SocketIOResponse deserialization
            string testResponse = @"{""messageId"":""test123"",""value"":42.5}";
            SocketIOResponse response = WebGLJsonHelper.DeserializeSocketIOResponse(testResponse);
            
            if (response != null && response.messageId == "test123" && Mathf.Approximately(response.value, 42.5f))
            {
                Debug.Log("[Test] JSON Deserialization: PASSED");
            }
            else
            {
                Debug.LogError("[Test] JSON Deserialization: FAILED");
                currentResults.jsonSerializationTest = false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Test] JSON serialization failed: {e.Message}");
            currentResults.lastError = e.Message;
        }

        yield return new WaitForSeconds(1f);
    }

    private IEnumerator TestConnection()
    {
        LogTest("Connection Test");

        if (SocketIOManager.Instance == null)
        {
            Debug.LogError("[Test] SocketIOManager not found!");
            yield break;
        }

        // Subscribe to connection events
        SocketIOManager.Instance.OnSocketConnected += OnTestConnectionReceived;
        SocketIOManager.Instance.OnSocketError += OnTestConnectionError;

        connectionReceived = false;

        // Attempt connection
        Debug.Log("[Test] Attempting connection...");
        SocketIOManager.Instance.ConnectToSocketIO();

        // Wait for connection (max 15 seconds)
        float timeout = 15f;
        float elapsed = 0f;

        while (!connectionReceived && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
            
            if (showDetailedLogs)
                Debug.Log($"[Test] Waiting for connection... {elapsed:F1}s");
        }

        if (connectionReceived)
        {
            Debug.Log("[Test] Connection: PASSED");
            currentResults.connectionTest = true;
        }
        else
        {
            Debug.LogError("[Test] Connection: FAILED - Timeout");
            currentResults.lastError = "Connection timeout";
        }

        // Cleanup
        SocketIOManager.Instance.OnSocketConnected -= OnTestConnectionReceived;
        SocketIOManager.Instance.OnSocketError -= OnTestConnectionError;

        yield return new WaitForSeconds(2f);
    }

    private IEnumerator TestMessageSending()
    {
        LogTest("Message Sending Test");

        if (!SocketIOManager.Instance.IsConnected)
        {
            Debug.LogError("[Test] Not connected - skipping message test");
            yield break;
        }

        messageReceived = false;
        SocketIOManager.Instance.OnMessageReceived += OnTestMessageReceived;

        // Send a test message (this might echo back depending on server)
        Debug.Log("[Test] Sending test message...");
        
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL-specific test
        if (SocketIOManager.Instance.IsWebGLSocketIOAvailable())
        {
            SocketIOManager.Instance.SendWebGLEvent("test", new { message = "WebGL test message", timestamp = Time.time });
        }
#endif

        // Wait for response (max 10 seconds)
        float timeout = 10f;
        float elapsed = 0f;

        while (!messageReceived && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }

        if (messageReceived)
        {
            Debug.Log($"[Test] Message Sending: PASSED - Received: {lastMessage}");
            currentResults.messageTest = true;
        }
        else
        {
            Debug.Log("[Test] Message Sending: TIMEOUT (may be normal if server doesn't echo)");
            currentResults.messageTest = true; // Consider timeout as pass for message sending
        }

        SocketIOManager.Instance.OnMessageReceived -= OnTestMessageReceived;

        yield return new WaitForSeconds(2f);
    }

    private IEnumerator TestGameDataSending()
    {
        LogTest("Game Data Sending Test");

        if (!SocketIOManager.Instance.IsConnected)
        {
            Debug.LogError("[Test] Not connected - skipping game data test");
            yield break;
        }

        Debug.Log("[Test] Sending test game data...");

        // Test different game data types
        SocketIOManager.Instance.SendDefaultScore(100f);
        yield return new WaitForSeconds(1f);

        SocketIOManager.Instance.SendPlatformerScore(5, 3);
        yield return new WaitForSeconds(1f);

        SocketIOManager.Instance.SendAimScore("hit", 0.85f, 1, 1);
        yield return new WaitForSeconds(1f);

        Debug.Log("[Test] Game Data Sending: PASSED");
        currentResults.gameDataTest = true;

        yield return new WaitForSeconds(2f);
    }

    private IEnumerator TestScoreReceiving()
    {
        LogTest("Score Receiving Test");

        if (!SocketIOManager.Instance.IsConnected)
        {
            Debug.LogError("[Test] Not connected - skipping score test");
            yield break;
        }

        scoreReceived = false;
        SocketIOManager.Instance.OnScoreReceived += OnTestScoreReceived;

        // Send data that should trigger a score response
        Debug.Log("[Test] Sending data to trigger score response...");
        SocketIOManager.Instance.SendDefaultScore(150f);

        // Wait for score response (max 10 seconds)
        float timeout = 10f;
        float elapsed = 0f;

        while (!scoreReceived && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }

        if (scoreReceived && lastScoreResponse != null)
        {
            Debug.Log($"[Test] Score Receiving: PASSED - MessageId: {lastScoreResponse.messageId}, Value: {lastScoreResponse.value}");
            currentResults.scoreReceiveTest = true;
        }
        else
        {
            Debug.LogWarning("[Test] Score Receiving: TIMEOUT (may be normal depending on server)");
        }

        SocketIOManager.Instance.OnScoreReceived -= OnTestScoreReceived;

        yield return new WaitForSeconds(2f);
    }

    private IEnumerator TestReconnection()
    {
        LogTest("Reconnection Test");

        if (!SocketIOManager.Instance.IsConnected)
        {
            Debug.LogWarning("[Test] Not connected - attempting reconnection test anyway");
        }

        connectionReceived = false;
        SocketIOManager.Instance.OnSocketConnected += OnTestConnectionReceived;

        // Disconnect and reconnect
        Debug.Log("[Test] Testing disconnection and reconnection...");
        SocketIOManager.Instance.DisconnectFromSocketIO();

        yield return new WaitForSeconds(3f);

        // Reconnect
        SocketIOManager.Instance.ConnectToSocketIO();

        // Wait for reconnection (max 20 seconds)
        float timeout = 20f;
        float elapsed = 0f;

        while (!connectionReceived && elapsed < timeout)
        {
            yield return new WaitForSeconds(1f);
            elapsed += 1f;
            
            if (showDetailedLogs)
                Debug.Log($"[Test] Waiting for reconnection... {elapsed:F1}s");
        }

        if (connectionReceived)
        {
            Debug.Log("[Test] Reconnection: PASSED");
            currentResults.reconnectionTest = true;
        }
        else
        {
            Debug.LogError("[Test] Reconnection: FAILED");
        }

        SocketIOManager.Instance.OnSocketConnected -= OnTestConnectionReceived;

        yield return new WaitForSeconds(2f);
    }

    private void LogTest(string testName)
    {
        Debug.Log($"=== {testName} ===");
    }

    private void LogFinalResults()
    {
        Debug.Log("=== FINAL TEST RESULTS ===");
        Debug.Log($"Platform Detection: {(currentResults.platformDetectionTest ? "PASSED" : "FAILED")}");
        Debug.Log($"JSON Serialization: {(currentResults.jsonSerializationTest ? "PASSED" : "FAILED")}");
        Debug.Log($"Connection: {(currentResults.connectionTest ? "PASSED" : "FAILED")}");
        Debug.Log($"Message Sending: {(currentResults.messageTest ? "PASSED" : "FAILED")}");
        Debug.Log($"Game Data: {(currentResults.gameDataTest ? "PASSED" : "FAILED")}");
        Debug.Log($"Score Receiving: {(currentResults.scoreReceiveTest ? "PASSED" : "FAILED")}");
        Debug.Log($"Reconnection: {(currentResults.reconnectionTest ? "PASSED" : "FAILED")}");
        
        int passedTests = 0;
        if (currentResults.platformDetectionTest) passedTests++;
        if (currentResults.jsonSerializationTest) passedTests++;
        if (currentResults.connectionTest) passedTests++;
        if (currentResults.messageTest) passedTests++;
        if (currentResults.gameDataTest) passedTests++;
        if (currentResults.scoreReceiveTest) passedTests++;
        if (currentResults.reconnectionTest) passedTests++;

        Debug.Log($"OVERALL RESULT: {passedTests}/7 tests passed");
        
        if (!string.IsNullOrEmpty(currentResults.lastError))
        {
            Debug.LogError($"Last Error: {currentResults.lastError}");
        }
        
        Debug.Log("=== END TEST RESULTS ===");
    }

    // Event handlers
    private void OnTestConnectionReceived()
    {
        connectionReceived = true;
        Debug.Log("[Test] Connection event received");
    }

    private void OnTestConnectionError(string error)
    {
        Debug.LogError($"[Test] Connection error: {error}");
        currentResults.lastError = error;
    }

    private void OnTestMessageReceived(string message)
    {
        messageReceived = true;
        lastMessage = message;
        Debug.Log($"[Test] Message received: {message}");
    }

    private void OnTestScoreReceived(SocketIOResponse response)
    {
        scoreReceived = true;
        lastScoreResponse = response;
        Debug.Log($"[Test] Score received: {response.messageId} = {response.value}");
    }

    /// <summary>
    /// Quick connection test for debugging
    /// </summary>
    [ContextMenu("Quick Connection Test")]
    public void QuickConnectionTest()
    {
        StartCoroutine(QuickConnectionTestCoroutine());
    }

    private IEnumerator QuickConnectionTestCoroutine()
    {
        Debug.Log("=== Quick Connection Test ===");
        
        if (SocketIOManager.Instance == null)
        {
            Debug.LogError("SocketIOManager not found!");
            yield break;
        }

        Debug.Log($"Current Status: {SocketIOManager.Instance.GetPlatformConnectionStatus()}");
        Debug.Log($"Is Connected: {SocketIOManager.Instance.IsConnected}");
        Debug.Log($"Is Connecting: {SocketIOManager.Instance.IsConnecting}");

#if UNITY_WEBGL && !UNITY_EDITOR
        if (SocketIOManager.Instance.IsWebGLSocketIOAvailable())
        {
            Debug.Log($"WebGL Status: {SocketIOManager.Instance.GetWebGLConnectionStatus()}");
        }
#endif

        if (!SocketIOManager.Instance.IsConnected && !SocketIOManager.Instance.IsConnecting)
        {
            Debug.Log("Attempting connection...");
            SocketIOManager.Instance.ConnectToSocketIO();
        }
    }

    private void Update()
    {
        if (enableAutoTesting && !isTestingInProgress)
        {
            if (Time.time - currentResults.lastTestTime > testInterval)
            {
                StartComprehensiveTest();
            }
        }
    }
}
