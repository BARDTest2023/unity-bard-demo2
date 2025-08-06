using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TestDataSender : MonoBehaviour
{
    [Header("Test Data Configuration")]
    [SerializeField] private float testScore = 100f;
    [SerializeField] private int testVictim = 5;
    [SerializeField] private int testStreak = 3;
    [SerializeField] private float testPrecision = 0.85f;
    [SerializeField] private bool testObstacleBlock = false;
    [SerializeField] private int testBarsActive = 2;
    [SerializeField] private string testQuestion = "What color is the sky?";
    [SerializeField] private string testAnswer = "blue";

    [Header("Auto Test Settings")]
    [SerializeField] private bool enableAutoTest = false;
    [SerializeField] private float autoTestInterval = 2f;
    [SerializeField] private int autoTestCycles = 5;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    public static TestDataSender Instance { get; private set; }

    // Events
    public event Action<string> OnTestDataSent;
    public event Action<string> OnTestCompleted;
    public event Action<int> OnAutoTestProgress;

    private Coroutine autoTestCoroutine;
    private int currentTestCycle = 0;

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
        // Subscribe to WebSocket events
        if (WebSocketManager.Instance != null)
        {
            WebSocketManager.Instance.OnWebSocketConnected += OnWebSocketConnected;
            WebSocketManager.Instance.OnScoreReceived += OnScoreReceived;
        }
    }

    private void OnWebSocketConnected()
    {
        if (showDebugInfo)
            Debug.Log("WebSocket connected - TestDataSender ready to send data");

        if (enableAutoTest)
        {
            StartAutoTest();
        }
    }

    private void OnScoreReceived(WebSocketResponse response)
    {
        if (showDebugInfo)
            Debug.Log($"TestDataSender received score: {response.value} (MessageId: {response.messageId})");
    }

    /// <summary>
    /// Sends default scoring test data
    /// </summary>
    public void SendDefaultTest()
    {
        if (!IsWebSocketReady())
            return;

        if (showDebugInfo)
            Debug.Log($"Sending default test score: {testScore}");

        WebSocketManager.Instance.SendDefaultScore(testScore);
        OnTestDataSent?.Invoke($"Default Score: {testScore}");
    }

    /// <summary>
    /// Sends platformer scoring test data
    /// </summary>
    public void SendPlatformerTest()
    {
        if (!IsWebSocketReady())
            return;

        if (showDebugInfo)
            Debug.Log($"Sending platformer test - Victim: {testVictim}, Streak: {testStreak}");

        WebSocketManager.Instance.SendPlatformerScore(testVictim, testStreak);
        OnTestDataSent?.Invoke($"Platformer - Victim: {testVictim}, Streak: {testStreak}");
    }

    /// <summary>
    /// Sends aim scoring test data
    /// </summary>
    public void SendAimTest()
    {
        if (!IsWebSocketReady())
            return;

        string aimType = "hit";
        if (showDebugInfo)
            Debug.Log($"Sending aim test - Type: {aimType}, Precision: {testPrecision}");

        WebSocketManager.Instance.SendAimScore(aimType, testPrecision);
        OnTestDataSent?.Invoke($"Aim - Type: {aimType}, Precision: {testPrecision}");
    }

    /// <summary>
    /// Sends aim miss test data
    /// </summary>
    public void SendAimMissTest()
    {
        if (!IsWebSocketReady())
            return;

        string aimType = "miss";
        if (showDebugInfo)
            Debug.Log($"Sending aim miss test - Type: {aimType}");

        WebSocketManager.Instance.SendAimScore(aimType, 0f, 0, 1);
        OnTestDataSent?.Invoke($"Aim - Type: {aimType}");
    }

    /// <summary>
    /// Sends multitasking scoring test data
    /// </summary>
    public void SendMultitaskingTest()
    {
        if (!IsWebSocketReady())
            return;

        List<string> targetClicks = new List<string> { "mid", "inner", "outer" };

        if (showDebugInfo)
            Debug.Log($"Sending multitasking test - Score: {testScore}, Bars: {testBarsActive}");

        WebSocketManager.Instance.SendMultitaskingScore(testScore, testObstacleBlock, testBarsActive, targetClicks);
        OnTestDataSent?.Invoke($"Multitasking - Score: {testScore}, Bars: {testBarsActive}");
    }

    /// <summary>
    /// Sends observe scoring test data
    /// </summary>
    public void SendObserveTest()
    {
        if (!IsWebSocketReady())
            return;

        if (showDebugInfo)
            Debug.Log($"Sending observe test - Question: {testQuestion}, Answer: {testAnswer}");

        WebSocketManager.Instance.SendObserveScore(testScore, testQuestion, testAnswer);
        OnTestDataSent?.Invoke($"Observe - Question: {testQuestion}, Answer: {testAnswer}");
    }

    /// <summary>
    /// Sends HoldTheWall scoring test data
    /// </summary>
    public void SendHoldTheWallTest()
    {
        if (!IsWebSocketReady())
            return;

        float timeElapsed = 30f; // Simulated time elapsed
        if (showDebugInfo)
            Debug.Log($"Sending HoldTheWall test - Time: {timeElapsed}, Score: {testScore}");

        WebSocketManager.Instance.SendHoldTheWallScore(timeElapsed, testScore);
        OnTestDataSent?.Invoke($"HoldTheWall - Time: {timeElapsed}, Score: {testScore}");
    }

    /// <summary>
    /// Sends ButtonSamsh scoring test data
    /// </summary>
    public void SendButtonSamshTest()
    {
        if (!IsWebSocketReady())
            return;

        if (showDebugInfo)
            Debug.Log($"Sending ButtonSamsh test - Score: {testScore}");

        WebSocketManager.Instance.SendButtonSamshScore(testScore);
        OnTestDataSent?.Invoke($"ButtonSamsh - Score: {testScore}");
    }

    /// <summary>
    /// Sends StayOnTarget scoring test data
    /// </summary>
    public void SendStayOnTargetTest()
    {
        if (!IsWebSocketReady())
            return;

        float timeElapsed = 45f; // Simulated time elapsed
        if (showDebugInfo)
            Debug.Log($"Sending StayOnTarget test - Time: {timeElapsed}, Score: {testScore}");

        WebSocketManager.Instance.SendStayOnTargetScore(timeElapsed, testScore);
        OnTestDataSent?.Invoke($"StayOnTarget - Time: {timeElapsed}, Score: {testScore}");
    }

    /// <summary>
    /// Sends all types of test data in sequence
    /// </summary>
    public void SendAllTests()
    {
        if (!IsWebSocketReady())
            return;

        if (showDebugInfo)
            Debug.Log("Sending all test data types...");

        StartCoroutine(SendAllTestsCoroutine());
    }

    private IEnumerator SendAllTestsCoroutine()
    {
        // Send default test
        SendDefaultTest();
        yield return new WaitForSeconds(0.5f);

        // Send platformer test
        SendPlatformerTest();
        yield return new WaitForSeconds(0.5f);

        // Send aim test
        SendAimTest();
        yield return new WaitForSeconds(0.5f);

        // Send aim miss test
        SendAimMissTest();
        yield return new WaitForSeconds(0.5f);

        // Send multitasking test
        SendMultitaskingTest();
        yield return new WaitForSeconds(0.5f);

        // Send observe test
        SendObserveTest();
        yield return new WaitForSeconds(0.5f);

        // Send HoldTheWall test
        SendHoldTheWallTest();
        yield return new WaitForSeconds(0.5f);

        // Send ButtonSamsh test
        SendButtonSamshTest();
        yield return new WaitForSeconds(0.5f);

        // Send StayOnTarget test
        SendStayOnTargetTest();

        if (showDebugInfo)
            Debug.Log("All test data sent successfully!");

        OnTestCompleted?.Invoke("All tests completed");
    }

    /// <summary>
    /// Starts automatic testing cycle
    /// </summary>
    public void StartAutoTest()
    {
        if (autoTestCoroutine != null)
        {
            StopCoroutine(autoTestCoroutine);
        }

        currentTestCycle = 0;
        autoTestCoroutine = StartCoroutine(AutoTestCoroutine());
    }

    /// <summary>
    /// Stops automatic testing cycle
    /// </summary>
    public void StopAutoTest()
    {
        if (autoTestCoroutine != null)
        {
            StopCoroutine(autoTestCoroutine);
            autoTestCoroutine = null;
        }

        if (showDebugInfo)
            Debug.Log("Auto test stopped");
    }

    private IEnumerator AutoTestCoroutine()
    {
        if (showDebugInfo)
            Debug.Log($"Starting auto test - {autoTestCycles} cycles, {autoTestInterval}s interval");

        for (int i = 0; i < autoTestCycles; i++)
        {
            currentTestCycle = i + 1;
            
            if (showDebugInfo)
                Debug.Log($"Auto test cycle {currentTestCycle}/{autoTestCycles}");

            OnAutoTestProgress?.Invoke(currentTestCycle);

            // Send a random test type
            SendRandomTest();

            yield return new WaitForSeconds(autoTestInterval);
        }

        if (showDebugInfo)
            Debug.Log("Auto test completed!");

        OnTestCompleted?.Invoke($"Auto test completed - {autoTestCycles} cycles");
    }

    /// <summary>
    /// Sends a random test type
    /// </summary>
    public void SendRandomTest()
    {
        if (!IsWebSocketReady())
            return;

        int testType = UnityEngine.Random.Range(0, 9);

        switch (testType)
        {
            case 0:
                SendDefaultTest();
                break;
            case 1:
                SendPlatformerTest();
                break;
            case 2:
                SendAimTest();
                break;
            case 3:
                SendAimMissTest();
                break;
            case 4:
                SendMultitaskingTest();
                break;
            case 5:
                SendObserveTest();
                break;
            case 6:
                SendHoldTheWallTest();
                break;
            case 7:
                SendButtonSamshTest();
                break;
            case 8:
                SendStayOnTargetTest();
                break;
        }
    }

    /// <summary>
    /// Checks if WebSocket is ready to send data
    /// </summary>
    private bool IsWebSocketReady()
    {
        if (WebSocketManager.Instance == null)
        {
            Debug.LogError("WebSocketManager not found!");
            return false;
        }

        if (!WebSocketManager.Instance.IsConnected)
        {
            Debug.LogWarning("WebSocket not connected - cannot send test data");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Manually trigger default test (for testing purposes)
    /// </summary>
    [ContextMenu("Test Default Score")]
    public void TestDefaultScore()
    {
        SendDefaultTest();
    }

    /// <summary>
    /// Manually trigger platformer test (for testing purposes)
    /// </summary>
    [ContextMenu("Test Platformer Score")]
    public void TestPlatformerScore()
    {
        SendPlatformerTest();
    }

    /// <summary>
    /// Manually trigger aim test (for testing purposes)
    /// </summary>
    [ContextMenu("Test Aim Score")]
    public void TestAimScore()
    {
        SendAimTest();
    }

    /// <summary>
    /// Manually trigger all tests (for testing purposes)
    /// </summary>
    [ContextMenu("Test All Data Types")]
    public void TestAllDataTypes()
    {
        SendAllTests();
    }

    /// <summary>
    /// Manually trigger auto test (for testing purposes)
    /// </summary>
    [ContextMenu("Test Auto Test")]
    public void TestAutoTest()
    {
        StartAutoTest();
    }

    private void OnDestroy()
    {
        // Stop auto test coroutine
        if (autoTestCoroutine != null)
        {
            StopCoroutine(autoTestCoroutine);
        }

        // Unsubscribe from events
        if (WebSocketManager.Instance != null)
        {
            WebSocketManager.Instance.OnWebSocketConnected -= OnWebSocketConnected;
            WebSocketManager.Instance.OnScoreReceived -= OnScoreReceived;
        }
    }

    // Public getters for UI
    public float TestScore => testScore;
    public int TestVictim => testVictim;
    public int TestStreak => testStreak;
    public float TestPrecision => testPrecision;
    public bool TestObstacleBlock => testObstacleBlock;
    public int TestBarsActive => testBarsActive;
    public string TestQuestion => testQuestion;
    public string TestAnswer => testAnswer;
    public bool EnableAutoTest => enableAutoTest;
    public float AutoTestInterval => autoTestInterval;
    public int AutoTestCycles => autoTestCycles;
    public int CurrentTestCycle => currentTestCycle;
    public bool IsAutoTestRunning => autoTestCoroutine != null;
} 