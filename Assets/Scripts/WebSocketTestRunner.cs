using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class WebSocketTestRunner : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private bool runTestsOnStart = false;
    [SerializeField] private float testDelay = 1f;
    
    [Header("Test Results")]
    [SerializeField] private bool webSocketConnected = false;
    [SerializeField] private bool apiValidated = false;
    [SerializeField] private int messagesSent = 0;
    [SerializeField] private int responsesReceived = 0;
    [SerializeField] private bool resultsSubmitted = false;

    public static WebSocketTestRunner Instance { get; private set; }

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
        if (runTestsOnStart)
        {
            StartCoroutine(RunAllTests());
        }
    }

    public void RunTests()
    {
        StartCoroutine(RunAllTests());
    }

    private IEnumerator RunAllTests()
    {
        Debug.Log("=== Starting WebSocket Test Suite ===");
        
        // Reset test results
        webSocketConnected = false;
        apiValidated = false;
        messagesSent = 0;
        responsesReceived = 0;
        resultsSubmitted = false;

        // Test 1: Check if all managers are available
        yield return StartCoroutine(TestManagersAvailable());
        
        // Test 2: Test API validation
        yield return StartCoroutine(TestAPIValidation());
        
        // Test 3: Test WebSocket connection
        yield return StartCoroutine(TestWebSocketConnection());
        
        // Test 4: Test message sending
        yield return StartCoroutine(TestMessageSending());
        
        // Test 5: Test results submission
        yield return StartCoroutine(TestResultsSubmission());
        
        // Test 6: Test redirection
        yield return StartCoroutine(TestRedirection());
        
        Debug.Log("=== WebSocket Test Suite Completed ===");
        LogTestResults();
    }

    private IEnumerator TestManagersAvailable()
    {
        Debug.Log("Test 1: Checking if all managers are available...");
        
        bool allManagersAvailable = true;
        
        if (URLParameterHandler.Instance == null)
        {
            Debug.LogError("❌ URLParameterHandler not found!");
            allManagersAvailable = false;
        }
        else
        {
            Debug.Log("✅ URLParameterHandler found");
        }
        
        if (APIConnector.Instance == null)
        {
            Debug.LogError("❌ APIConnector not found!");
            allManagersAvailable = false;
        }
        else
        {
            Debug.Log("✅ APIConnector found");
        }
        
        if (WebSocketManager.Instance == null)
        {
            Debug.LogError("❌ WebSocketManager not found!");
            allManagersAvailable = false;
        }
        else
        {
            Debug.Log("✅ WebSocketManager found");
        }
        
        if (TestDataSender.Instance == null)
        {
            Debug.LogError("❌ TestDataSender not found!");
            allManagersAvailable = false;
        }
        else
        {
            Debug.Log("✅ TestDataSender found");
        }
        
        if (UIManager.Instance == null)
        {
            Debug.LogError("❌ UIManager not found!");
            allManagersAvailable = false;
        }
        else
        {
            Debug.Log("✅ UIManager found");
        }
        
        if (allManagersAvailable)
        {
            Debug.Log("✅ All managers are available");
        }
        
        yield return new WaitForSeconds(testDelay);
    }

    private IEnumerator TestAPIValidation()
    {
        Debug.Log("Test 2: Testing API validation...");
        
        if (APIConnector.Instance != null)
        {
            // Subscribe to API events
            APIConnector.Instance.OnTestCanStart += OnTestCanStart;
            APIConnector.Instance.OnTestCannotStart += OnTestCannotStart;
            APIConnector.Instance.OnAPIError += OnAPIError;
            
            // Trigger API validation
            APIConnector.Instance.ValidateTestStart();
            
            // Wait for response
            yield return new WaitForSeconds(3f);
            
            // Unsubscribe from events
            APIConnector.Instance.OnTestCanStart -= OnTestCanStart;
            APIConnector.Instance.OnTestCannotStart -= OnTestCannotStart;
            APIConnector.Instance.OnAPIError -= OnAPIError;
        }
        
        yield return new WaitForSeconds(testDelay);
    }

    private IEnumerator TestWebSocketConnection()
    {
        Debug.Log("Test 3: Testing WebSocket connection...");
        
        if (WebSocketManager.Instance != null)
        {
            // Subscribe to WebSocket events
            WebSocketManager.Instance.OnWebSocketConnected += OnWebSocketConnected;
            WebSocketManager.Instance.OnWebSocketError += OnWebSocketError;
            
            // Connect to WebSocket
            WebSocketManager.Instance.ConnectToWebSocket();
            
            // Wait for connection
            yield return new WaitForSeconds(3f);
            
            // Unsubscribe from events
            WebSocketManager.Instance.OnWebSocketConnected -= OnWebSocketConnected;
            WebSocketManager.Instance.OnWebSocketError -= OnWebSocketError;
        }
        
        yield return new WaitForSeconds(testDelay);
    }

    private IEnumerator TestMessageSending()
    {
        Debug.Log("Test 4: Testing message sending...");
        
        if (WebSocketManager.Instance != null && WebSocketManager.Instance.IsConnected)
        {
            // Subscribe to response events
            WebSocketManager.Instance.OnScoreReceived += OnScoreReceived;
            
            // Send test messages
            WebSocketManager.Instance.SendDefaultScore(100f);
            messagesSent++;
            yield return new WaitForSeconds(0.5f);
            
            WebSocketManager.Instance.SendPlatformerScore(5, 3);
            messagesSent++;
            yield return new WaitForSeconds(0.5f);
            
            WebSocketManager.Instance.SendAimScore("hit", 0.85f);
            messagesSent++;
            yield return new WaitForSeconds(0.5f);
            
            // Wait for responses
            yield return new WaitForSeconds(2f);
            
            // Unsubscribe from events
            WebSocketManager.Instance.OnScoreReceived -= OnScoreReceived;
        }
        else
        {
            Debug.LogWarning("⚠️ WebSocket not connected - skipping message sending test");
        }
        
        yield return new WaitForSeconds(testDelay);
    }

    private IEnumerator TestResultsSubmission()
    {
        Debug.Log("Test 5: Testing results submission...");
        
        if (APIConnector.Instance != null)
        {
            // Subscribe to results events
            APIConnector.Instance.OnResultsSaved += OnResultsSaved;
            
            // Submit test results
            APIConnector.Instance.SaveResults("unity-demo", 150f);
            
            // Wait for response
            yield return new WaitForSeconds(3f);
            
            // Unsubscribe from events
            APIConnector.Instance.OnResultsSaved -= OnResultsSaved;
        }
        
        yield return new WaitForSeconds(testDelay);
    }

    private IEnumerator TestRedirection()
    {
        Debug.Log("Test 6: Testing redirection...");
        
        if (APIConnector.Instance != null)
        {
            Debug.Log("Testing redirection to progress page...");
            // Note: This will actually redirect, so we'll just log it
            Debug.Log("✅ Redirection method available");
        }
        
        yield return new WaitForSeconds(testDelay);
    }

    // Event handlers for tests
    private void OnTestCanStart(PlaySessionData data)
    {
        Debug.Log("✅ API validation successful - test can start");
        apiValidated = true;
    }

    private void OnTestCannotStart(string error)
    {
        Debug.LogWarning($"⚠️ API validation failed: {error}");
    }

    private void OnAPIError(string error)
    {
        Debug.LogError($"❌ API error: {error}");
    }

    private void OnWebSocketConnected()
    {
        Debug.Log("✅ WebSocket connected successfully");
        webSocketConnected = true;
    }

    private void OnWebSocketError(string error)
    {
        Debug.LogError($"❌ WebSocket error: {error}");
    }

    private void OnScoreReceived(WebSocketResponse response)
    {
        Debug.Log($"✅ Score received: {response.value} (MessageId: {response.messageId})");
        responsesReceived++;
    }

    private void OnResultsSaved(bool success)
    {
        if (success)
        {
            Debug.Log("✅ Results saved successfully");
            resultsSubmitted = true;
        }
        else
        {
            Debug.LogError("❌ Failed to save results");
        }
    }

    private void LogTestResults()
    {
        Debug.Log("=== Test Results Summary ===");
        Debug.Log($"WebSocket Connected: {(webSocketConnected ? "✅" : "❌")}");
        Debug.Log($"API Validated: {(apiValidated ? "✅" : "❌")}");
        Debug.Log($"Messages Sent: {messagesSent}");
        Debug.Log($"Responses Received: {responsesReceived}");
        Debug.Log($"Results Submitted: {(resultsSubmitted ? "✅" : "❌")}");
        Debug.Log("=============================");
    }

    [ContextMenu("Run All Tests")]
    public void RunAllTestsFromContext()
    {
        RunTests();
    }
} 