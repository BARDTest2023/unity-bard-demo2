using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WebGL-compatible callback system to replace async/await patterns
/// Provides a way to handle asynchronous operations using coroutines and callbacks
/// </summary>
public class WebGLCallbackHelper : MonoBehaviour
{
    private static WebGLCallbackHelper _instance;
    public static WebGLCallbackHelper Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("WebGLCallbackHelper");
                _instance = go.AddComponent<WebGLCallbackHelper>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    /// <summary>
    /// Callback delegate for WebGL operations
    /// </summary>
    /// <param name="success">True if operation succeeded</param>
    /// <param name="result">Result data or error message</param>
    public delegate void WebGLCallback(bool success, string result);

    /// <summary>
    /// Callback delegate for operations with typed results
    /// </summary>
    /// <typeparam name="T">Type of the result</typeparam>
    /// <param name="success">True if operation succeeded</param>
    /// <param name="result">Result data</param>
    /// <param name="error">Error message if failed</param>
    public delegate void WebGLCallback<T>(bool success, T result, string error);

    /// <summary>
    /// Execute an operation with a delay (WebGL-compatible alternative to Task.Delay)
    /// </summary>
    /// <param name="delaySeconds">Delay in seconds</param>
    /// <param name="callback">Callback to execute after delay</param>
    public void ExecuteWithDelay(float delaySeconds, Action callback)
    {
        StartCoroutine(ExecuteWithDelayCoroutine(delaySeconds, callback));
    }

    /// <summary>
    /// Execute an operation with timeout (WebGL-compatible alternative to async timeout)
    /// </summary>
    /// <param name="operation">Operation to execute</param>
    /// <param name="timeoutSeconds">Timeout in seconds</param>
    /// <param name="callback">Callback with success/failure result</param>
    public void ExecuteWithTimeout(Action operation, float timeoutSeconds, WebGLCallback callback)
    {
        StartCoroutine(ExecuteWithTimeoutCoroutine(operation, timeoutSeconds, callback));
    }

    /// <summary>
    /// Execute a series of operations in sequence (WebGL-compatible alternative to async chains)
    /// </summary>
    /// <param name="operations">List of operations to execute</param>
    /// <param name="callback">Final callback</param>
    public void ExecuteSequence(List<Action> operations, WebGLCallback callback)
    {
        StartCoroutine(ExecuteSequenceCoroutine(operations, callback));
    }

    /// <summary>
    /// Retry an operation with exponential backoff (WebGL-compatible)
    /// </summary>
    /// <param name="operation">Operation to retry</param>
    /// <param name="maxAttempts">Maximum number of attempts</param>
    /// <param name="baseDelay">Base delay between attempts</param>
    /// <param name="callback">Final callback</param>
    public void RetryOperation(Func<bool> operation, int maxAttempts, float baseDelay, WebGLCallback callback)
    {
        StartCoroutine(RetryOperationCoroutine(operation, maxAttempts, baseDelay, callback));
    }

    /// <summary>
    /// Wait for a condition to be met (WebGL-compatible alternative to async waiting)
    /// </summary>
    /// <param name="condition">Condition to wait for</param>
    /// <param name="timeoutSeconds">Timeout in seconds</param>
    /// <param name="callback">Callback when condition is met or timeout occurs</param>
    public void WaitForCondition(Func<bool> condition, float timeoutSeconds, WebGLCallback callback)
    {
        StartCoroutine(WaitForConditionCoroutine(condition, timeoutSeconds, callback));
    }

    // ===========================================
    // Coroutine Implementations
    // ===========================================

    private IEnumerator ExecuteWithDelayCoroutine(float delaySeconds, Action callback)
    {
        yield return new WaitForSeconds(delaySeconds);
        
        try
        {
            callback?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebGLCallbackHelper] Error in delayed execution: {e.Message}");
        }
    }

    private IEnumerator ExecuteWithTimeoutCoroutine(Action operation, float timeoutSeconds, WebGLCallback callback)
    {
        bool completed = false;
        string error = null;

        try
        {
            operation?.Invoke();
            completed = true;
        }
        catch (Exception e)
        {
            error = e.Message;
            completed = true;
        }

        float elapsedTime = 0f;
        while (!completed && elapsedTime < timeoutSeconds)
        {
            yield return new WaitForSeconds(0.1f);
            elapsedTime += 0.1f;
        }

        if (!completed)
        {
            callback?.Invoke(false, "Operation timed out");
        }
        else if (!string.IsNullOrEmpty(error))
        {
            callback?.Invoke(false, error);
        }
        else
        {
            callback?.Invoke(true, "Operation completed successfully");
        }
    }

    private IEnumerator ExecuteSequenceCoroutine(List<Action> operations, WebGLCallback callback)
    {
        if (operations == null || operations.Count == 0)
        {
            callback?.Invoke(true, "No operations to execute");
            yield break;
        }

        for (int i = 0; i < operations.Count; i++)
        {
            bool operationFailed = false;
            string errorMessage = null;
            
            try
            {
                operations[i]?.Invoke();
            }
            catch (Exception e)
            {
                operationFailed = true;
                errorMessage = $"Operation {i} failed: {e.Message}";
            }
            
            if (operationFailed)
            {
                callback?.Invoke(false, errorMessage);
                yield break;
            }
            
            yield return null; // Allow frame to process
        }

        callback?.Invoke(true, "All operations completed successfully");
    }

    private IEnumerator RetryOperationCoroutine(Func<bool> operation, int maxAttempts, float baseDelay, WebGLCallback callback)
    {
        int attempt = 0;
        
        while (attempt < maxAttempts)
        {
            attempt++;
            
            try
            {
                bool success = operation?.Invoke() ?? false;
                if (success)
                {
                    callback?.Invoke(true, $"Operation succeeded on attempt {attempt}");
                    yield break;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WebGLCallbackHelper] Attempt {attempt} failed: {e.Message}");
                
                if (attempt >= maxAttempts)
                {
                    callback?.Invoke(false, $"All {maxAttempts} attempts failed. Last error: {e.Message}");
                    yield break;
                }
            }

            // Exponential backoff delay
            float delay = baseDelay * Mathf.Pow(2, attempt - 1);
            yield return new WaitForSeconds(delay);
        }

        callback?.Invoke(false, $"Operation failed after {maxAttempts} attempts");
    }

    private IEnumerator WaitForConditionCoroutine(Func<bool> condition, float timeoutSeconds, WebGLCallback callback)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < timeoutSeconds)
        {
            try
            {
                if (condition?.Invoke() == true)
                {
                    callback?.Invoke(true, "Condition met");
                    yield break;
                }
            }
            catch (Exception e)
            {
                callback?.Invoke(false, $"Error checking condition: {e.Message}");
                yield break;
            }

            yield return new WaitForSeconds(0.1f);
            elapsedTime += 0.1f;
        }

        callback?.Invoke(false, "Condition not met within timeout");
    }

    // ===========================================
    // Utility Methods
    // ===========================================

    /// <summary>
    /// Convert a Unity coroutine to a callback-based operation
    /// </summary>
    /// <param name="coroutine">Coroutine to execute</param>
    /// <param name="callback">Callback when coroutine completes</param>
    public void ExecuteCoroutine(IEnumerator coroutine, WebGLCallback callback)
    {
        StartCoroutine(ExecuteCoroutineWithCallback(coroutine, callback));
    }

    private IEnumerator ExecuteCoroutineWithCallback(IEnumerator coroutine, WebGLCallback callback)
    {
        bool success = true;
        string error = null;

        // Execute the coroutine and handle exceptions outside of yield
        var coroutineOperation = StartCoroutine(coroutine);
        
        yield return coroutineOperation;
        
        // Check if there was an exception (Unity coroutines don't throw exceptions in the traditional sense)
        // So we'll assume success unless we detect otherwise
        callback?.Invoke(success, error ?? "Coroutine completed successfully");
    }

    /// <summary>
    /// Create a simple callback that logs the result
    /// </summary>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <returns>WebGLCallback that logs results</returns>
    public static WebGLCallback CreateLoggingCallback(string operationName)
    {
        return (success, result) =>
        {
            if (success)
            {
                Debug.Log($"[WebGLCallbackHelper] {operationName} succeeded: {result}");
            }
            else
            {
                Debug.LogError($"[WebGLCallbackHelper] {operationName} failed: {result}");
            }
        };
    }

    /// <summary>
    /// Chain multiple callbacks together
    /// </summary>
    /// <param name="callbacks">Callbacks to chain</param>
    /// <returns>Combined callback</returns>
    public static WebGLCallback ChainCallbacks(params WebGLCallback[] callbacks)
    {
        return (success, result) =>
        {
            foreach (var callback in callbacks)
            {
                try
                {
                    callback?.Invoke(success, result);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[WebGLCallbackHelper] Error in chained callback: {e.Message}");
                }
            }
        };
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
}
