using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Validates server availability for Socket.IO testing
/// </summary>
public class ServerValidator : MonoBehaviour
{
    [System.Serializable]
    public class ServerInfo
    {
        public string name;
        public string url;
        public int port;
        public bool isAvailable;
        public string lastError;
        public float lastCheckTime;
    }

    [Header("Servers to Validate")]
    [SerializeField] private ServerInfo[] servers = new ServerInfo[]
    {
        new ServerInfo { name = "BARD Test Server", url = "http://test.bardtest.gg", port = 80 },
        new ServerInfo { name = "Local Test Server", url = "http://localhost", port = 3333 }
    };

    [Header("Validation Settings")]
    [SerializeField] private float checkInterval = 30f;
    [SerializeField] private float requestTimeout = 10f;
    [SerializeField] private bool autoValidate = true;

    public static ServerValidator Instance { get; private set; }

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
        if (autoValidate)
        {
            InvokeRepeating(nameof(ValidateAllServers), 2f, checkInterval);
        }
    }

    /// <summary>
    /// Validate all configured servers
    /// </summary>
    [ContextMenu("Validate All Servers")]
    public void ValidateAllServers()
    {
        Debug.Log("=== Server Validation Started ===");
        
        foreach (var server in servers)
        {
            StartCoroutine(ValidateServer(server));
        }
    }

    /// <summary>
    /// Validate a specific server
    /// </summary>
    /// <param name="server">Server to validate</param>
    /// <returns>Coroutine for validation</returns>
    public IEnumerator ValidateServer(ServerInfo server)
    {
        server.lastCheckTime = Time.time;
        string fullUrl = $"{server.url}:{server.port}";
        
        Debug.Log($"[ServerValidator] Checking {server.name} at {fullUrl}");

        using (UnityWebRequest request = UnityWebRequest.Get(fullUrl))
        {
            request.timeout = (int)requestTimeout;
            
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                server.isAvailable = true;
                server.lastError = "";
                Debug.Log($"[ServerValidator] ✅ {server.name}: Available (Status: {request.responseCode})");
                
                // Try to parse response for additional info
                try
                {
                    string response = request.downloadHandler.text;
                    if (!string.IsNullOrEmpty(response))
                    {
                        Debug.Log($"[ServerValidator] Response from {server.name}: {response.Substring(0, Mathf.Min(200, response.Length))}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[ServerValidator] Could not parse response from {server.name}: {e.Message}");
                }
            }
            else
            {
                server.isAvailable = false;
                server.lastError = $"{request.result}: {request.error}";
                Debug.LogWarning($"[ServerValidator] ❌ {server.name}: Not available ({server.lastError})");
                
                // Provide helpful suggestions
                if (server.url.Contains("localhost"))
                {
                    Debug.LogWarning($"[ServerValidator] 💡 To test locally, run: node test-socketio-server.js");
                }
            }
        }
    }

    /// <summary>
    /// Get server availability status
    /// </summary>
    /// <param name="serverName">Name of the server</param>
    /// <returns>True if server is available</returns>
    public bool IsServerAvailable(string serverName)
    {
        foreach (var server in servers)
        {
            if (server.name.Equals(serverName, System.StringComparison.OrdinalIgnoreCase))
            {
                return server.isAvailable;
            }
        }
        return false;
    }

    /// <summary>
    /// Get the first available server
    /// </summary>
    /// <returns>First available server info, or null if none available</returns>
    public ServerInfo GetFirstAvailableServer()
    {
        foreach (var server in servers)
        {
            if (server.isAvailable)
            {
                return server;
            }
        }
        return null;
    }

    /// <summary>
    /// Log current server status
    /// </summary>
    [ContextMenu("Log Server Status")]
    public void LogServerStatus()
    {
        Debug.Log("=== Server Status Report ===");
        
        foreach (var server in servers)
        {
            string status = server.isAvailable ? "✅ Available" : "❌ Not Available";
            string lastCheck = server.lastCheckTime > 0 ? $"(Last check: {Time.time - server.lastCheckTime:F1}s ago)" : "(Not checked)";
            
            Debug.Log($"{server.name} ({server.url}:{server.port}): {status} {lastCheck}");
            
            if (!server.isAvailable && !string.IsNullOrEmpty(server.lastError))
            {
                Debug.Log($"  Error: {server.lastError}");
            }
        }
        
        ServerInfo available = GetFirstAvailableServer();
        if (available != null)
        {
            Debug.Log($"💡 Recommended server: {available.name} ({available.url}:{available.port})");
        }
        else
        {
            Debug.LogWarning("⚠️ No servers are currently available!");
        }
        
        Debug.Log("=== End Server Status ===");
    }

    /// <summary>
    /// Test connection to the BARD test server specifically
    /// </summary>
    [ContextMenu("Test BARD Server")]
    public void TestBardServer()
    {
        foreach (var server in servers)
        {
            if (server.name.Contains("BARD"))
            {
                StartCoroutine(ValidateServer(server));
                break;
            }
        }
    }

    /// <summary>
    /// Test connection to the local test server specifically
    /// </summary>
    [ContextMenu("Test Local Server")]
    public void TestLocalServer()
    {
        foreach (var server in servers)
        {
            if (server.name.Contains("Local"))
            {
                StartCoroutine(ValidateServer(server));
                break;
            }
        }
    }

    /// <summary>
    /// Get server status for UI display
    /// </summary>
    /// <returns>Formatted string with server status</returns>
    public string GetServerStatusString()
    {
        string status = "Server Status:\n";
        
        foreach (var server in servers)
        {
            string availability = server.isAvailable ? "✅" : "❌";
            status += $"{availability} {server.name}\n";
        }
        
        return status;
    }

    private void OnDestroy()
    {
        CancelInvoke();
    }
}
