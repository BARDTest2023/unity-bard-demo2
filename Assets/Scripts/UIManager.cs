using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{

    [Header("Connection Status")]
    [SerializeField] private TextMeshProUGUI connectionStatusText;
    [SerializeField] private TextMeshProUGUI urlParametersText;
    [SerializeField] private TextMeshProUGUI apiStatusText;

    [Header("Test Controls")]
    [SerializeField] private Button connectButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private Button sendDefaultButton;
    [SerializeField] private Button sendPlatformerButton;
    [SerializeField] private Button sendAimButton;
    [SerializeField] private Button sendAimMissButton;
    [SerializeField] private Button sendMultitaskingButton;
    [SerializeField] private Button sendObserveButton;
    [SerializeField] private Button sendAllTestsButton;
    [SerializeField] private Button startAutoTestButton;
    [SerializeField] private Button stopAutoTestButton;
    [SerializeField] private Button submitResultsButton;

    [Header("Test Configuration")]
    [SerializeField] private Slider scoreSlider;
    [SerializeField] private TextMeshProUGUI scoreValueText;
    [SerializeField] private Slider victimSlider;
    [SerializeField] private TextMeshProUGUI victimValueText;
    [SerializeField] private Slider streakSlider;
    [SerializeField] private TextMeshProUGUI streakValueText;
    [SerializeField] private Slider precisionSlider;
    [SerializeField] private TextMeshProUGUI precisionValueText;
    [SerializeField] private Toggle obstacleBlockToggle;
    [SerializeField] private Slider barsActiveSlider;
    [SerializeField] private TextMeshProUGUI barsActiveValueText;
    [SerializeField] private TMP_InputField questionInputField;
    [SerializeField] private TMP_InputField answerInputField;

    [Header("Auto Test Settings")]
    [SerializeField] private Toggle enableAutoTestToggle;
    [SerializeField] private Slider autoTestIntervalSlider;
    [SerializeField] private TextMeshProUGUI autoTestIntervalText;
    [SerializeField] private Slider autoTestCyclesSlider;
    [SerializeField] private TextMeshProUGUI autoTestCyclesText;
    [SerializeField] private TextMeshProUGUI autoTestProgressText;

    [Header("Response Monitoring")]
    [SerializeField] private ScrollRect responseScrollRect;
    [SerializeField] private TextMeshProUGUI responseText;
    [SerializeField] private Button clearResponsesButton;
    [SerializeField] private TextMeshProUGUI lastScoreText;
    [SerializeField] private TextMeshProUGUI totalResponsesText;
    [SerializeField] private ScrollViewContentSizer responseContentSizer;

    [Header("UI Colors")]
    [SerializeField] private Color connectedColor = Color.green;
    [SerializeField] private Color disconnectedColor = Color.red;
    [SerializeField] private Color connectingColor = Color.yellow;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    public static UIManager Instance { get; private set; }

    // UI State
    private bool isConnected = false;
    private bool isConnecting = false;
    private int totalResponses = 0;
    private float lastScore = 0f;
    private List<string> responseHistory = new List<string>();

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
        Debug.Log("=== UIManager Start() called ===");
        Debug.Log("UIManager Instance: " + (Instance != null ? "Found" : "NULL"));
        
        InitializeUI();
        SubscribeToEvents();
        UpdateConnectionStatus(false, "Not connected");
        
        Debug.Log("=== UIManager Start() completed ===");
    }

    private void InitializeUI()
    {
        // Initialize sliders
        if (scoreSlider != null)
        {
            scoreSlider.minValue = 0f;
            scoreSlider.maxValue = 1000f;
            scoreSlider.value = 100f;
            scoreSlider.onValueChanged.AddListener(OnScoreSliderChanged);
        }

        if (victimSlider != null)
        {
            victimSlider.minValue = 0;
            victimSlider.maxValue = 20;
            victimSlider.value = 5;
            victimSlider.onValueChanged.AddListener(OnVictimSliderChanged);
        }

        if (streakSlider != null)
        {
            streakSlider.minValue = 0;
            streakSlider.maxValue = 10;
            streakSlider.value = 3;
            streakSlider.onValueChanged.AddListener(OnStreakSliderChanged);
        }

        if (precisionSlider != null)
        {
            precisionSlider.minValue = 0f;
            precisionSlider.maxValue = 1f;
            precisionSlider.value = 0.85f;
            precisionSlider.onValueChanged.AddListener(OnPrecisionSliderChanged);
        }

        if (barsActiveSlider != null)
        {
            barsActiveSlider.minValue = 0;
            barsActiveSlider.maxValue = 5;
            barsActiveSlider.value = 2;
            barsActiveSlider.onValueChanged.AddListener(OnBarsActiveSliderChanged);
        }

        if (autoTestIntervalSlider != null)
        {
            autoTestIntervalSlider.minValue = 0.5f;
            autoTestIntervalSlider.maxValue = 10f;
            autoTestIntervalSlider.value = 2f;
            autoTestIntervalSlider.onValueChanged.AddListener(OnAutoTestIntervalChanged);
        }

        if (autoTestCyclesSlider != null)
        {
            autoTestCyclesSlider.minValue = 1;
            autoTestCyclesSlider.maxValue = 20;
            autoTestCyclesSlider.value = 5;
            autoTestCyclesSlider.onValueChanged.AddListener(OnAutoTestCyclesChanged);
        }

        // Initialize input fields
        if (questionInputField != null)
        {
            questionInputField.text = "What color is the sky?";
        }

        if (answerInputField != null)
        {
            answerInputField.text = "blue";
        }

        // Initialize toggles
        if (enableAutoTestToggle != null)
        {
            enableAutoTestToggle.isOn = false;
        }

        if (obstacleBlockToggle != null)
        {
            obstacleBlockToggle.isOn = false;
        }

        // Initialize button click events
        Debug.Log("Setting up button click events...");
        if (connectButton != null)
        {
            Debug.Log("Connect button found - adding click listener");
            connectButton.onClick.AddListener(OnConnectButtonClicked);
        }
        else
        {
            Debug.LogError("Connect button is NULL! Make sure it's assigned in the inspector.");
        }

        if (disconnectButton != null)
        {
            disconnectButton.onClick.AddListener(OnDisconnectButtonClicked);
        }

        if (sendDefaultButton != null)
        {
            sendDefaultButton.onClick.AddListener(OnSendDefaultButtonClicked);
        }

        if (sendPlatformerButton != null)
        {
            sendPlatformerButton.onClick.AddListener(OnSendPlatformerButtonClicked);
        }

        if (sendAimButton != null)
        {
            sendAimButton.onClick.AddListener(OnSendAimButtonClicked);
        }

        if (sendAimMissButton != null)
        {
            sendAimMissButton.onClick.AddListener(OnSendAimMissButtonClicked);
        }

        if (sendMultitaskingButton != null)
        {
            sendMultitaskingButton.onClick.AddListener(OnSendMultitaskingButtonClicked);
        }

        if (sendObserveButton != null)
        {
            sendObserveButton.onClick.AddListener(OnSendObserveButtonClicked);
        }

        if (sendAllTestsButton != null)
        {
            sendAllTestsButton.onClick.AddListener(OnSendAllTestsButtonClicked);
        }

        if (startAutoTestButton != null)
        {
            startAutoTestButton.onClick.AddListener(OnStartAutoTestButtonClicked);
        }

        if (stopAutoTestButton != null)
        {
            stopAutoTestButton.onClick.AddListener(OnStopAutoTestButtonClicked);
        }

        if (submitResultsButton != null)
        {
            submitResultsButton.onClick.AddListener(OnSubmitResultsButtonClicked);
        }

        if (clearResponsesButton != null)
        {
            clearResponsesButton.onClick.AddListener(OnClearResponsesButtonClicked);
        }

        UpdateSliderTexts();
    }

    private void SubscribeToEvents()
    {
        // WebSocket events
        if (WebSocketManager.Instance != null)
        {
            WebSocketManager.Instance.OnWebSocketConnected += OnWebSocketConnected;
            WebSocketManager.Instance.OnWebSocketDisconnected += OnWebSocketDisconnected;
            WebSocketManager.Instance.OnWebSocketError += OnWebSocketError;
            WebSocketManager.Instance.OnScoreReceived += OnScoreReceived;
            WebSocketManager.Instance.OnMessageReceived += OnMessageReceived;
            WebSocketManager.Instance.OnConnectionStatusChanged += OnConnectionStatusChanged;
        }

        // API events
        if (APIConnector.Instance != null)
        {
            APIConnector.Instance.OnTestCanStart += OnTestCanStart;
            APIConnector.Instance.OnTestCannotStart += OnTestCannotStart;
            APIConnector.Instance.OnAPIError += OnAPIError;
            APIConnector.Instance.OnResultsSaved += OnResultsSaved;
            APIConnector.Instance.OnConnectionStatusChanged += OnAPIConnectionStatusChanged;
        }

        // TestDataSender events
        if (TestDataSender.Instance != null)
        {
            TestDataSender.Instance.OnTestDataSent += OnTestDataSent;
            TestDataSender.Instance.OnTestCompleted += OnTestCompleted;
            TestDataSender.Instance.OnAutoTestProgress += OnAutoTestProgress;
        }
    }

    #region Event Handlers

    private void OnWebSocketConnected()
    {
        isConnected = true;
        isConnecting = false;
        UpdateConnectionStatus(true, "WebSocket Connected");
        UpdateButtonStates();
    }

    private void OnWebSocketDisconnected()
    {
        isConnected = false;
        isConnecting = false;
        UpdateConnectionStatus(false, "WebSocket Disconnected");
        UpdateButtonStates();
    }

    private void OnWebSocketError(string error)
    {
        AddResponse($"WebSocket Error: {error}");
        UpdateConnectionStatus(false, $"Error: {error}");
    }

    private void OnScoreReceived(WebSocketResponse response)
    {
        lastScore = response.value;
        totalResponses++;
        
        string responseMessage = $"Score Received - MessageId: {response.messageId}, Value: {response.value}";
        AddResponse(responseMessage);
        
        UpdateResponseUI();
    }

    private void OnMessageReceived(string message)
    {
        AddResponse($"Message: {message}");
    }

    private void OnConnectionStatusChanged(bool isConnecting)
    {
        this.isConnecting = isConnecting;
        if (isConnecting)
        {
            UpdateConnectionStatus(false, "Connecting...");
        }
    }

    private void OnTestCanStart(PlaySessionData data)
    {
        string message = $"Test can start - Username: {data.username}, Save Results: {data.saveResults}";
        AddResponse(message);
        UpdateAPIStatus("API: Test validated successfully");
    }

    private void OnTestCannotStart(string error)
    {
        AddResponse($"Test cannot start: {error}");
        UpdateAPIStatus($"API: {error}");
    }

    private void OnAPIError(string error)
    {
        AddResponse($"API Error: {error}");
        UpdateAPIStatus($"API: {error}");
    }

    private void OnResultsSaved(bool success)
    {
        string message = success ? "Results saved successfully" : "Failed to save results";
        AddResponse(message);
        
        if (success)
        {
            // Redirect to the progress page after successful results submission
            if (showDebugInfo)
                Debug.Log("Results saved successfully - redirecting to progress page");
            
            AddResponse("Redirecting to progress page...");
            
            // Use a coroutine to delay the redirect slightly so the user can see the success message
            StartCoroutine(RedirectAfterDelay(2f));
        }
    }

    private IEnumerator RedirectAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (APIConnector.Instance != null)
        {
            APIConnector.Instance.RedirectToProgressPage();
        }
        else
        {
            Debug.LogError("APIConnector not found for redirection!");
        }
    }

    private void OnAPIConnectionStatusChanged(bool isConnecting)
    {
        if (isConnecting)
        {
            UpdateAPIStatus("API: Connecting...");
        }
    }

    private void OnTestDataSent(string message)
    {
        AddResponse($"Test Sent: {message}");
    }

    private void OnTestCompleted(string message)
    {
        AddResponse($"Test Completed: {message}");
    }

    private void OnAutoTestProgress(int cycle)
    {
        if (autoTestProgressText != null)
        {
            autoTestProgressText.text = $"Auto Test: Cycle {cycle}";
        }
    }

    #endregion

    #region UI Updates

    private void UpdateConnectionStatus(bool connected, string status)
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.text = status;
        }

        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        bool canSendData = isConnected && !isConnecting;

        // Connection buttons
        if (connectButton != null)
            connectButton.interactable = !isConnected && !isConnecting;

        if (disconnectButton != null)
            disconnectButton.interactable = isConnected;

        // Test buttons
        if (sendDefaultButton != null)
            sendDefaultButton.interactable = canSendData;

        if (sendPlatformerButton != null)
            sendPlatformerButton.interactable = canSendData;

        if (sendAimButton != null)
            sendAimButton.interactable = canSendData;

        if (sendAimMissButton != null)
            sendAimMissButton.interactable = canSendData;

        if (sendMultitaskingButton != null)
            sendMultitaskingButton.interactable = canSendData;

        if (sendObserveButton != null)
            sendObserveButton.interactable = canSendData;

        if (sendAllTestsButton != null)
            sendAllTestsButton.interactable = canSendData;

        if (startAutoTestButton != null)
            startAutoTestButton.interactable = canSendData && !TestDataSender.Instance.IsAutoTestRunning;

        if (stopAutoTestButton != null)
            stopAutoTestButton.interactable = TestDataSender.Instance.IsAutoTestRunning;

        if (submitResultsButton != null)
            submitResultsButton.interactable = true; // Always enabled since it doesn't require WebSocket connection
    }

    private void UpdateAPIStatus(string status)
    {
        if (apiStatusText != null)
        {
            apiStatusText.text = status;
        }
    }

    private void UpdateURLParameters()
    {
        if (URLParameterHandler.Instance != null && urlParametersText != null)
        {
            string parameters = $"Session: {URLParameterHandler.Instance.PlaySessionUuid}\n" +
                             $"Variant: {URLParameterHandler.Instance.Variant}\n" +
                             $"Input: {URLParameterHandler.Instance.Input}\n" +
                             $"Language: {URLParameterHandler.Instance.Lang}\n" +
                             $"Test: {URLParameterHandler.Instance.TestName}\n" +
                             $"Rank: {URLParameterHandler.Instance.TestRank}\n" +
                             $"Device: {URLParameterHandler.Instance.Device}";

            urlParametersText.text = parameters;
        }
    }

    private void UpdateResponseUI()
    {
        if (lastScoreText != null)
        {
            lastScoreText.text = $"Last Score: {lastScore:F1}";
        }

        if (totalResponsesText != null)
        {
            totalResponsesText.text = $"Total Responses: {totalResponses}";
        }
    }

    private void AddResponse(string message)
    {
        if (responseText != null)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            string formattedMessage = $"[{timestamp}] {message}\n";
            
            responseHistory.Add(formattedMessage);
            
            // Keep only last 50 responses
            if (responseHistory.Count > 50)
            {
                responseHistory.RemoveAt(0);
            }

            // Update response text using the content sizer if available
            if (responseContentSizer != null)
            {
                responseContentSizer.SetText(string.Join("", responseHistory));
            }
            else
            {
                // Fallback to original method
                responseText.text = string.Join("", responseHistory);
                
                // Scroll to bottom
                if (responseScrollRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                    responseScrollRect.verticalNormalizedPosition = 0f;
                }
            }
        }
    }

    #endregion

    #region Slider Event Handlers

    private void OnScoreSliderChanged(float value)
    {
        UpdateSliderTexts();
    }

    private void OnVictimSliderChanged(float value)
    {
        UpdateSliderTexts();
    }

    private void OnStreakSliderChanged(float value)
    {
        UpdateSliderTexts();
    }

    private void OnPrecisionSliderChanged(float value)
    {
        UpdateSliderTexts();
    }

    private void OnBarsActiveSliderChanged(float value)
    {
        UpdateSliderTexts();
    }

    private void OnAutoTestIntervalChanged(float value)
    {
        UpdateSliderTexts();
    }

    private void OnAutoTestCyclesChanged(float value)
    {
        UpdateSliderTexts();
    }

    private void UpdateSliderTexts()
    {
        if (scoreValueText != null && scoreSlider != null)
            scoreValueText.text = $"Score: {scoreSlider.value:F0}";

        if (victimValueText != null && victimSlider != null)
            victimValueText.text = $"Victim: {victimSlider.value:F0}";

        if (streakValueText != null && streakSlider != null)
            streakValueText.text = $"Streak: {streakSlider.value:F0}";

        if (precisionValueText != null && precisionSlider != null)
            precisionValueText.text = $"Precision: {precisionSlider.value:F2}";

        if (barsActiveValueText != null && barsActiveSlider != null)
            barsActiveValueText.text = $"Bars Active: {barsActiveSlider.value:F0}";

        if (autoTestIntervalText != null && autoTestIntervalSlider != null)
            autoTestIntervalText.text = $"Interval: {autoTestIntervalSlider.value:F1}s";

        if (autoTestCyclesText != null && autoTestCyclesSlider != null)
            autoTestCyclesText.text = $"Cycles: {autoTestCyclesSlider.value:F0}";
    }

    #endregion

    #region Button Event Handlers

    public void OnConnectButtonClicked()
    {
        Debug.Log("=== CONNECT BUTTON CLICKED ===");
        Debug.Log("UIManager Instance: " + (Instance != null ? "Found" : "NULL"));
        Debug.Log("Connect Button: " + (connectButton != null ? "Found" : "NULL"));
        
        // Try API validation first
        if (APIConnector.Instance != null)
        {
            Debug.Log("APIConnector found - calling ValidateTestStart()");
            APIConnector.Instance.ValidateTestStart();
        }
        else
        {
            Debug.LogError("APIConnector not found! Make sure APIConnector script is attached to a GameObject.");
        }
        
        // Also try direct WebSocket connection for testing
        if (WebSocketManager.Instance != null)
        {
            Debug.Log("WebSocketManager found - calling ConnectToWebSocket()");
            WebSocketManager.Instance.ConnectToWebSocket();
        }
        else
        {
            Debug.LogError("WebSocketManager not found! Make sure WebSocketManager script is attached to a GameObject.");
        }
        
        Debug.Log("=== END CONNECT BUTTON CLICK ===");
    }

    public void OnDisconnectButtonClicked()
    {
        if (WebSocketManager.Instance != null)
        {
            WebSocketManager.Instance.DisconnectFromWebSocket();
        }
    }

    public void OnSendDefaultButtonClicked()
    {
        if (TestDataSender.Instance != null)
        {
            TestDataSender.Instance.SendDefaultTest();
        }
    }

    public void OnSendPlatformerButtonClicked()
    {
        if (TestDataSender.Instance != null)
        {
            TestDataSender.Instance.SendPlatformerTest();
        }
    }

    public void OnSendAimButtonClicked()
    {
        if (TestDataSender.Instance != null)
        {
            TestDataSender.Instance.SendAimTest();
        }
    }

    public void OnSendAimMissButtonClicked()
    {
        if (TestDataSender.Instance != null)
        {
            TestDataSender.Instance.SendAimMissTest();
        }
    }

    public void OnSendMultitaskingButtonClicked()
    {
        if (TestDataSender.Instance != null)
        {
            TestDataSender.Instance.SendMultitaskingTest();
        }
    }

    public void OnSendObserveButtonClicked()
    {
        if (TestDataSender.Instance != null)
        {
            TestDataSender.Instance.SendObserveTest();
        }
    }

    public void OnSendAllTestsButtonClicked()
    {
        if (TestDataSender.Instance != null)
        {
            TestDataSender.Instance.SendAllTests();
        }
    }

    public void OnStartAutoTestButtonClicked()
    {
        if (TestDataSender.Instance != null)
        {
            TestDataSender.Instance.StartAutoTest();
        }
    }

    public void OnStopAutoTestButtonClicked()
    {
        if (TestDataSender.Instance != null)
        {
            TestDataSender.Instance.StopAutoTest();
        }
    }

    public void OnClearResponsesButtonClicked()
    {
        responseHistory.Clear();
        
        // Clear text using the content sizer if available
        if (responseContentSizer != null)
        {
            responseContentSizer.ClearText();
        }
        else if (responseText != null)
        {
            responseText.text = "";
        }
        
        totalResponses = 0;
        UpdateResponseUI();
    }

    public void OnSubmitResultsButtonClicked()
    {
        if (APIConnector.Instance != null)
        {
            // Get the current score from the slider
            float finalScore = scoreSlider != null ? scoreSlider.value : 100f;
            
            if (showDebugInfo)
                Debug.Log($"Submitting results with score: {finalScore}");

            // Save results using the existing APIConnector method
            APIConnector.Instance.SaveResults("unity-demo", finalScore);
            
            AddResponse($"Submitting results with score: {finalScore}");
        }
        else
        {
            Debug.LogError("APIConnector not found!");
            AddResponse("Error: APIConnector not found!");
        }
    }

    #endregion

    private void Update()
    {
        // Update URL parameters display
        UpdateURLParameters();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (WebSocketManager.Instance != null)
        {
            WebSocketManager.Instance.OnWebSocketConnected -= OnWebSocketConnected;
            WebSocketManager.Instance.OnWebSocketDisconnected -= OnWebSocketDisconnected;
            WebSocketManager.Instance.OnWebSocketError -= OnWebSocketError;
            WebSocketManager.Instance.OnScoreReceived -= OnScoreReceived;
            WebSocketManager.Instance.OnMessageReceived -= OnMessageReceived;
            WebSocketManager.Instance.OnConnectionStatusChanged -= OnConnectionStatusChanged;
        }

        if (APIConnector.Instance != null)
        {
            APIConnector.Instance.OnTestCanStart -= OnTestCanStart;
            APIConnector.Instance.OnTestCannotStart -= OnTestCannotStart;
            APIConnector.Instance.OnAPIError -= OnAPIError;
            APIConnector.Instance.OnResultsSaved -= OnResultsSaved;
            APIConnector.Instance.OnConnectionStatusChanged -= OnAPIConnectionStatusChanged;
        }

        if (TestDataSender.Instance != null)
        {
            TestDataSender.Instance.OnTestDataSent -= OnTestDataSent;
            TestDataSender.Instance.OnTestCompleted -= OnTestCompleted;
            TestDataSender.Instance.OnAutoTestProgress -= OnAutoTestProgress;
        }
    }
} 