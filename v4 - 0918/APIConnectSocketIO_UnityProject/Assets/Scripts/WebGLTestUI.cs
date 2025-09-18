using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple UI component to display WebGL Socket.IO test results
/// </summary>
public class WebGLTestUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI resultsText;
    [SerializeField] private Button testButton;
    [SerializeField] private Button quickTestButton;
    [SerializeField] private Button clearButton;

    [Header("Colors")]
    [SerializeField] private Color passedColor = Color.green;
    [SerializeField] private Color failedColor = Color.red;
    [SerializeField] private Color pendingColor = Color.yellow;

    private WebGLSocketIOTester tester;

    private void Start()
    {
        // Find or create the tester
        tester = FindObjectOfType<WebGLSocketIOTester>();
        if (tester == null)
        {
            GameObject testerGO = new GameObject("WebGLSocketIOTester");
            tester = testerGO.AddComponent<WebGLSocketIOTester>();
        }

        // Setup button listeners
        if (testButton != null)
            testButton.onClick.AddListener(StartTest);

        if (quickTestButton != null)
            quickTestButton.onClick.AddListener(QuickTest);

        if (clearButton != null)
            clearButton.onClick.AddListener(ClearResults);

        // Initialize UI
        UpdateUI();
        
        // Update UI periodically
        InvokeRepeating(nameof(UpdateUI), 1f, 1f);
    }

    public void StartTest()
    {
        if (tester != null)
        {
            tester.StartComprehensiveTest();
        }
    }

    public void QuickTest()
    {
        if (tester != null)
        {
            tester.QuickConnectionTest();
        }
    }

    public void ClearResults()
    {
        if (resultsText != null)
        {
            resultsText.text = "Results cleared. Run tests to see new results.";
        }
    }

    private void UpdateUI()
    {
        if (tester == null) return;

        // Update status
        if (statusText != null)
        {
            if (SocketIOManager.Instance != null)
            {
                string status = $"Platform: {GetPlatformName()}\n";
                status += $"Connected: {SocketIOManager.Instance.IsConnected}\n";
                status += $"Connecting: {SocketIOManager.Instance.IsConnecting}\n";
                status += $"Status: {SocketIOManager.Instance.GetPlatformConnectionStatus()}";

                statusText.text = status;
                statusText.color = SocketIOManager.Instance.IsConnected ? passedColor : failedColor;
            }
            else
            {
                statusText.text = "SocketIOManager not found!";
                statusText.color = failedColor;
            }
        }

        // Update test results (this would need to be expanded to show actual test results)
        if (resultsText != null)
        {
            string results = "WebGL Socket.IO Test Results:\n\n";
            
#if UNITY_WEBGL && !UNITY_EDITOR
            results += "✓ Platform: WebGL Build\n";
            
            if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsWebGLSocketIOAvailable())
            {
                results += "✓ WebGL Socket Available\n";
                results += $"Status: {SocketIOManager.Instance.GetWebGLConnectionStatus()}\n";
            }
            else
            {
                results += "✗ WebGL Socket Not Available\n";
            }
#else
            results += "✓ Platform: Standalone Build\n";
            results += "Note: WebGL-specific features not active in editor\n";
#endif

            results += "\nClick 'Start Test' for comprehensive testing";
            results += "\nClick 'Quick Test' for connection check";

            resultsText.text = results;
        }
    }

    private string GetPlatformName()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return "WebGL";
#elif UNITY_EDITOR
        return "Editor";
#elif UNITY_STANDALONE_WIN
        return "Windows";
#elif UNITY_STANDALONE_OSX
        return "macOS";
#elif UNITY_STANDALONE_LINUX
        return "Linux";
#else
        return "Unknown";
#endif
    }

    private void OnDestroy()
    {
        CancelInvoke();
    }
}
