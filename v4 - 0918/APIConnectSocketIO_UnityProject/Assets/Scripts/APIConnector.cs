using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;
using System.Text;

[System.Serializable]
public class PlaySessionResponse
{
    public PlaySessionData data;
    public string meta;
    public string status;
}

[System.Serializable]
public class PlaySessionData
{
    public bool canStartGame;
    public string username;
    public bool saveResults;
    public string input;
}

[System.Serializable]
public class ResultSubmission
{
    public string game;
    public string variant;
    public string input;
    public ResultData results;
}

[System.Serializable]
public class ResultData
{
    public float score;
}

public class APIConnector : MonoBehaviour
{
    [Header("API Configuration")]
    [SerializeField] private string baseURL = "https://test.bardtest.gg";
    [SerializeField] private string apiEndpoint = "/api/play-sessions/";
    [SerializeField] private string resultsEndpoint = "/api/results/";
    [SerializeField] private string failedPlaySessionEndpoint = "/failed-play-session";

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    public static APIConnector Instance { get; private set; }

    // Events
    public event Action<PlaySessionData> OnTestCanStart;
    public event Action<string> OnTestCannotStart;
    public event Action<string> OnAPIError;
    public event Action<bool> OnResultsSaved;
    public event Action<bool> OnConnectionStatusChanged;

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
        // Subscribe to URL parameter events
        if (URLParameterHandler.Instance != null)
        {
            URLParameterHandler.Instance.OnAllParametersParsed += OnParametersReceived;
        }
    }

    private void OnParametersReceived()
    {
        Debug.Log("URL parameters received, triggering API validation...");
        // Automatically validate the test when parameters are received
        ValidateTestStart();
    }

    /// <summary>
    /// Validates if the test can start by calling the backend API
    /// </summary>
    public void ValidateTestStart()
    {
        if (URLParameterHandler.Instance == null)
        {
            Debug.LogError("URLParameterHandler not found!");
            return;
        }

        string playSessionUuid = URLParameterHandler.Instance.PlaySessionUuid;
        string input = URLParameterHandler.Instance.Input;
        string variant = URLParameterHandler.Instance.Variant;

        if (string.IsNullOrEmpty(playSessionUuid))
        {
            Debug.LogError("Play session UUID is missing!");
            OnAPIError?.Invoke("Play session UUID is missing");
            return;
        }

        StartCoroutine(ValidateTestStartCoroutine(playSessionUuid, input, variant));
    }

    private IEnumerator ValidateTestStartCoroutine(string playSessionUuid, string input, string variant)
    {
        Debug.Log("Starting API validation...");
        
        // Construct the API URL
        string apiUrl = $"{baseURL}{apiEndpoint}{playSessionUuid}?game=unity-demo&input={input}&variant={variant}";

        if (showDebugInfo)
            Debug.Log($"Calling API: {apiUrl}");

        OnConnectionStatusChanged?.Invoke(true);

        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            // Set headers
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            Debug.Log("Sending API request...");
            yield return request.SendWebRequest();
            Debug.Log($"API request completed. Result: {request.result}, Response Code: {request.responseCode}");

            OnConnectionStatusChanged?.Invoke(false);

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (showDebugInfo)
                    Debug.Log($"API Response: {request.downloadHandler.text}");

                try
                {
                    PlaySessionResponse response = JsonUtility.FromJson<PlaySessionResponse>(request.downloadHandler.text);
                    
                    if (response.status == "SUCCESS" && response.data.canStartGame)
                    {
                        if (showDebugInfo)
                            Debug.Log("Test can start successfully!");
                        
                        Debug.Log("Invoking OnTestCanStart event");
                        OnTestCanStart?.Invoke(response.data);
                    }
                    else
                    {
                        string errorMessage = "Test cannot start";
                        if (showDebugInfo)
                            Debug.LogWarning(errorMessage);
                        
                        OnTestCannotStart?.Invoke(errorMessage);
                        RedirectToFailedPage();
                    }
                }
                catch (Exception e)
                {
                    string errorMessage = $"Error parsing API response: {e.Message}";
                    Debug.LogError(errorMessage);
                    OnAPIError?.Invoke(errorMessage);
                }
            }
            else
            {
                string errorMessage = $"API request failed: {request.error}";
                Debug.LogError(errorMessage);
                OnAPIError?.Invoke(errorMessage);
                
                // If it's a 404 or similar, redirect to failed page
                if (request.responseCode >= 400)
                {
                    RedirectToFailedPage();
                }
            }
        }
    }

    /// <summary>
    /// Saves the final test results to the backend
    /// </summary>
    public void SaveResults(string gameId, float finalScore)
    {
        if (URLParameterHandler.Instance == null)
        {
            Debug.LogError("URLParameterHandler not found!");
            return;
        }

        StartCoroutine(SaveResultsCoroutine(gameId, finalScore));
    }

    private IEnumerator SaveResultsCoroutine(string gameId, float finalScore)
    {
        string playSessionUuid = URLParameterHandler.Instance.PlaySessionUuid;
        string input = URLParameterHandler.Instance.Input;
        string variant = URLParameterHandler.Instance.Variant;

        // Create the result data
        ResultSubmission resultData = new ResultSubmission
        {
            game = gameId,
            variant = variant,
            input = input,
            results = new ResultData
            {
                score = finalScore
            }
        };

        string jsonData = JsonUtility.ToJson(resultData);
        string apiUrl = $"{baseURL}{resultsEndpoint}{playSessionUuid}";

        if (showDebugInfo)
        {
            Debug.Log($"Saving results to: {apiUrl}");
            Debug.Log($"Result data: {jsonData}");
        }

        OnConnectionStatusChanged?.Invoke(true);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            OnConnectionStatusChanged?.Invoke(false);

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (showDebugInfo)
                    Debug.Log($"Results saved successfully: {request.downloadHandler.text}");
                
                OnResultsSaved?.Invoke(true);
            }
            else
            {
                string errorMessage = $"Failed to save results: {request.error}";
                Debug.LogError(errorMessage);
                OnResultsSaved?.Invoke(false);
            }
        }
    }

    /// <summary>
    /// Redirects to the failed play session page
    /// </summary>
    private void RedirectToFailedPage()
    {
        string failedUrl = $"{baseURL}{failedPlaySessionEndpoint}";
        
        if (showDebugInfo)
            Debug.Log($"Redirecting to failed page: {failedUrl}");

        // In WebGL, we can use Application.OpenURL to redirect
        Application.OpenURL(failedUrl);
    }

    /// <summary>
    /// Redirects to the progress page after test completion
    /// </summary>
    public void RedirectToProgressPage()
    {
        string progressUrl = $"{baseURL}/progressing-play-session";
        
        if (showDebugInfo)
            Debug.Log($"Redirecting to progress page: {progressUrl}");

        Application.OpenURL(progressUrl);
    }

    /// <summary>
    /// Manually trigger test validation (for testing purposes)
    /// </summary>
    [ContextMenu("Test API Connection")]
    public void TestAPIConnection()
    {
        ValidateTestStart();
    }

    /// <summary>
    /// Manually save test results (for testing purposes)
    /// </summary>
    [ContextMenu("Test Save Results")]
    public void TestSaveResults()
    {
        SaveResults("unity-demo", 100.0f);
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (URLParameterHandler.Instance != null)
        {
            URLParameterHandler.Instance.OnAllParametersParsed -= OnParametersReceived;
        }
    }
} 