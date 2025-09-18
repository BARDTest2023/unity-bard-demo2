using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class URLParameterHandler : MonoBehaviour
{
    [Header("URL Parameters")]
    [SerializeField] private string playSessionUuid;
    [SerializeField] private string variant;
    [SerializeField] private string input;
    [SerializeField] private string lang;
    [SerializeField] private string testName;
    [SerializeField] private string testRank;
    [SerializeField] private string device;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    public static URLParameterHandler Instance { get; private set; }

    // Events
    public event Action<string> OnPlaySessionUuidReceived;
    public event Action<string> OnVariantReceived;
    public event Action<string> OnInputReceived;
    public event Action<string> OnLangReceived;
    public event Action<string> OnTestNameReceived;
    public event Action<string> OnTestRankReceived;
    public event Action<string> OnDeviceReceived;
    public event Action OnAllParametersParsed;

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
        Debug.Log("URLParameterHandler starting...");
        ParseURLParameters();
    }

    private void ParseURLParameters()
    {
        try
        {
            // Get the current URL
            string url = Application.absoluteURL;
            
            if (showDebugInfo)
                Debug.Log($"Parsing URL: {url}");

            // Check if URL has parameters
            if (string.IsNullOrEmpty(url) || !url.Contains("?"))
            {
                Debug.LogWarning("No URL parameters found. Using default values for testing.");
                SetDefaultValues();
                return;
            }

            // Extract query string
            string queryString = url.Substring(url.IndexOf('?') + 1);
            
            if (showDebugInfo)
                Debug.Log($"Query string: {queryString}");

            // Parse parameters
            Dictionary<string, string> parameters = ParseQueryString(queryString);

            // Extract and set parameters
            playSessionUuid = GetParameterValue(parameters, "play_session_uuid");
            variant = GetParameterValue(parameters, "variant");
            input = GetParameterValue(parameters, "input");
            lang = GetParameterValue(parameters, "lang");
            testName = GetParameterValue(parameters, "testName");
            testRank = GetParameterValue(parameters, "testRank");
            device = GetParameterValue(parameters, "device");

            // Trigger events
            Debug.Log("Triggering URL parameter events...");
            OnPlaySessionUuidReceived?.Invoke(playSessionUuid);
            OnVariantReceived?.Invoke(variant);
            OnInputReceived?.Invoke(input);
            OnLangReceived?.Invoke(lang);
            OnTestNameReceived?.Invoke(testName);
            OnTestRankReceived?.Invoke(testRank);
            OnDeviceReceived?.Invoke(device);
            Debug.Log("Invoking OnAllParametersParsed event");
            OnAllParametersParsed?.Invoke();

            if (showDebugInfo)
            {
                Debug.Log("URL Parameters parsed successfully:");
                Debug.Log($"Play Session UUID: {playSessionUuid}");
                Debug.Log($"Variant: {variant}");
                Debug.Log($"Input: {input}");
                Debug.Log($"Language: {lang}");
                Debug.Log($"Test Name: {testName}");
                Debug.Log($"Test Rank: {testRank}");
                Debug.Log($"Device: {device}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing URL parameters: {e.Message}");
            SetDefaultValues();
        }
    }

    private Dictionary<string, string> ParseQueryString(string queryString)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        
        string[] pairs = queryString.Split('&');
        
        foreach (string pair in pairs)
        {
            if (string.IsNullOrEmpty(pair)) continue;
            
            string[] keyValue = pair.Split('=');
            if (keyValue.Length == 2)
            {
                string key = keyValue[0];
                string value = System.Web.HttpUtility.UrlDecode(keyValue[1]);
                parameters[key] = value;
            }
        }
        
        return parameters;
    }

    private string GetParameterValue(Dictionary<string, string> parameters, string key)
    {
        return parameters.ContainsKey(key) ? parameters[key] : "";
    }

    private void SetDefaultValues()
    {
        Debug.Log("Setting default URL parameter values for testing...");
        playSessionUuid = "test-session-uuid";
        variant = "1";
        input = "standard";
        lang = "en";
        testName = "Unity Test";
        testRank = "1";
        device = "standard";

        // Trigger events with default values
        Debug.Log("Triggering events with default values...");
        OnPlaySessionUuidReceived?.Invoke(playSessionUuid);
        OnVariantReceived?.Invoke(variant);
        OnInputReceived?.Invoke(input);
        OnLangReceived?.Invoke(lang);
        OnTestNameReceived?.Invoke(testName);
        OnTestRankReceived?.Invoke(testRank);
        OnDeviceReceived?.Invoke(device);
        Debug.Log("Invoking OnAllParametersParsed event with default values");
        OnAllParametersParsed?.Invoke();
    }

    // Public getters for other scripts
    public string PlaySessionUuid => playSessionUuid;
    public string Variant => variant;
    public string Input => input;
    public string Lang => lang;
    public string TestName => testName;
    public string TestRank => testRank;
    public string Device => device;

    // Method to check if we have valid parameters
    public bool HasValidParameters()
    {
        return !string.IsNullOrEmpty(playSessionUuid) && 
               !string.IsNullOrEmpty(variant) && 
               !string.IsNullOrEmpty(input);
    }

    // Method to get all parameters as a dictionary for API calls
    public Dictionary<string, string> GetAllParameters()
    {
        return new Dictionary<string, string>
        {
            { "play_session_uuid", playSessionUuid },
            { "variant", variant },
            { "input", input },
            { "lang", lang },
            { "testName", testName },
            { "testRank", testRank },
            { "device", device }
        };
    }
} 